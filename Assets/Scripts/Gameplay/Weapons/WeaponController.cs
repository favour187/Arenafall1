using UnityEngine;
using System.Collections;
using ArenaFall.Core;
using ArenaFall.Data;
using ArenaFall.Interfaces;
using ArenaFall.Events;
using ArenaFall.Managers;
using ArenaFall.Gameplay.Characters;

namespace ArenaFall.Gameplay.Weapons
{
    /// <summary>
    /// Controls weapon behavior including firing, reloading, aiming, and attachments.
    /// Uses WeaponData ScriptableObject for configuration.
    /// </summary>
    public class WeaponController : MonoBehaviour, IWeapon
    {
        [Header("Data")]
        [SerializeField] private WeaponData _weaponData;

        [Header("References")]
        [SerializeField] private Transform _muzzlePoint;
        [SerializeField] private Transform _weaponVisual;
        [SerializeField] private GameObject _muzzleFlashEffect;
        [SerializeField] private TrailRenderer _bulletTrailPrefab;

        [Header("Weapon Audio")]
        [SerializeField] private AudioSource _audioSource;

        // State
        private int _currentAmmo;
        private int _reserveAmmo;
        private int _currentFireMode;
        private bool _isFiring;
        private bool _isReloading;
        private bool _isAiming;
        private bool _isEquipped;
        private float _lastFireTime;
        private float _currentAccuracy;
        private float _recoilRecoveryTimer;

        // Attachments
        private AttachmentData _sightAttachment;
        private AttachmentData _muzzleAttachment;
        private AttachmentData _gripAttachment;
        private AttachmentData _magazineAttachment;

        // Cached components
        private PlayerCharacterController _owner;
        private InputManager _input;
        private PoolManager _pool;

        // Properties
        public string WeaponName => _weaponData != null ? _weaponData.weaponName : "Unknown";
        public WeaponData Data => _weaponData;
        public int CurrentAmmo => _currentAmmo;
        public int ReserveAmmo => _reserveAmmo;
        public int CurrentFireMode => _currentFireMode;
        public bool IsFiring => _isFiring;
        public bool IsReloading => _isReloading;
        public bool IsReady => _isEquipped && !_isReloading;
        public bool IsAiming => _isAiming;

        private void Awake()
        {
            _currentAccuracy = _weaponData != null ? _weaponData.baseAccuracy : 0.95f;
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
            _pool = ServiceLocator.Get<PoolManager>();
            _input = ServiceLocator.Get<InputManager>();
        }

        private void Start()
        {
            if (_weaponData != null)
            {
                _currentAmmo = _weaponData.magazineSize;
                _reserveAmmo = _weaponData.maxReserveAmmo;
            }
        }

        private void Update()
        {
            if (_input == null) _input = InputManager.Instance ?? ServiceLocator.Get<InputManager>();
            if (_input != null && _isEquipped)
            {
                if (_input.IsFiring && !_isFiring) StartFire();
                else if (!_input.IsFiring && _isFiring) StopFire();

                if (_input.IsAiming && !_isAiming) { StartAim(); if (CameraManager.Instance != null) CameraManager.Instance.SetAiming(true); }
                else if (!_input.IsAiming && _isAiming) { StopAim(); if (CameraManager.Instance != null) CameraManager.Instance.SetAiming(false); }

                if (_input.IsReloading && !_isReloading) Reload();
            }

            if (!_isEquipped) return;

            // Accuracy recovery
            if (_currentAccuracy < _weaponData.baseAccuracy)
            {
                _currentAccuracy = Mathf.Min(_currentAccuracy + _weaponData.accuracyRecoveryRate * Time.deltaTime, _weaponData.baseAccuracy);
            }

            // Recoil recovery
            if (_recoilRecoveryTimer > 0)
            {
                _recoilRecoveryTimer -= Time.deltaTime;
            }

            // Continuous fire
            if (_isFiring && !_isReloading && Time.time - _lastFireTime >= _weaponData.GetFireInterval())
            {
                Fire();
            }
        }

        /// <summary>
        /// Start the firing sequence.
        /// </summary>
        public void StartFire()
        {
            if (_isReloading || !_isEquipped || _currentAmmo <= 0) return;

            _isFiring = true;

            // Single fire mode - fire once, then stop
            if (_weaponData.availableFireModes[_currentFireMode] == FireMode.Single)
            {
                Fire();
                _isFiring = false;
            }
        }

        /// <summary>
        /// Stop the firing sequence.
        /// </summary>
        public void StopFire()
        {
            _isFiring = false;
        }

        /// <summary>
        /// Execute a single shot.
        /// </summary>
        private void Fire()
        {
            if (_currentAmmo <= 0 || Time.time - _lastFireTime < _weaponData.GetFireInterval())
            {
                if (_currentAmmo <= 0) TryAutoReload();
                return;
            }

            _lastFireTime = Time.time;
            _currentAmmo--;

            // Calculate accuracy
            float spread = _weaponData.hipFireSpread * (1f + (1f - _currentAccuracy));
            if (_isAiming) spread *= _weaponData.adsSpreadMultiplier;

            // Calculate recoil
            Vector2 recoil = _weaponData.recoilKick;
            if (_muzzleAttachment != null) recoil *= _muzzleAttachment.recoilModifier;

            // Apply recoil to controller
            ApplyRecoil(recoil);

            // Raycast for hit
            Vector3 aimPoint = GetAimPoint(spread);
            PerformShot(aimPoint);

            // Visual effects
            PlayFireEffects();

            // Audio
            PlayFireSound();

            // Reduce accuracy
            _currentAccuracy = Mathf.Max(0.1f, _currentAccuracy - _weaponData.accuracySpreadPerShot);

            // Event
            EventBus.Raise(new WeaponFiredEvent
            {
                PlayerId = _owner != null ? _owner.GetInstanceID().ToString() : "",
                WeaponId = _weaponData.weaponId,
                FirePoint = _muzzlePoint.position,
                AimPoint = aimPoint,
                IsAds = _isAiming
            });

            // Auto reload
            if (_currentAmmo <= 0) TryAutoReload();
        }

        private Vector3 GetAimPoint(float spread)
        {
            Vector3 direction = _isAiming ? 
                Camera.main.transform.forward : 
                Camera.main.transform.forward + Random.insideUnitSphere * spread;

            Ray ray = new Ray(Camera.main.transform.position, direction);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, _weaponData.maxRange))
            {
                return hit.point;
            }

            return ray.GetPoint(_weaponData.effectiveRange);
        }

        private void PerformShot(Vector3 aimPoint)
        {
            Vector3 direction = (aimPoint - _muzzlePoint.position).normalized;
            RaycastHit hit;

            // Check for damage falloff
            float distance = Vector3.Distance(_muzzlePoint.position, aimPoint);
            float damageMultiplier = 1f;

            if (distance > _weaponData.damageFalloffStart)
            {
                float t = Mathf.InverseLerp(_weaponData.damageFalloffStart, _weaponData.damageFalloffEnd, distance);
                damageMultiplier = Mathf.Lerp(1f, _weaponData.damageFalloffMin, t);
            }

            if (Physics.Raycast(_muzzlePoint.position, direction, out hit, _weaponData.maxRange))
            {
                IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    float baseDamage = _weaponData.baseDamage * damageMultiplier;

                    // Check for headshot
                    bool isHeadshot = hit.collider.CompareTag("Head");
                    if (isHeadshot)
                    {
                        baseDamage *= _weaponData.headshotMultiplier;
                    }

                    // Apply attachment modifiers
                    if (_muzzleAttachment != null) baseDamage *= _muzzleAttachment.damageModifier;

                    damageable.TakeDamage(baseDamage, DamageType.Bullet, gameObject);
                }

                // Spawn impact effect
                if (_weaponData.bulletImpactPrefab != null)
                {
                    Instantiate(_weaponData.bulletImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                }
            }

            // Bullet trail
            if (_bulletTrailPrefab != null)
            {
                TrailRenderer trail = Instantiate(_bulletTrailPrefab, _muzzlePoint.position, Quaternion.identity);
                StartCoroutine(UpdateTrail(trail, hit.point));
            }
        }

        private IEnumerator UpdateTrail(TrailRenderer trail, Vector3 hitPoint)
        {
            float time = 0;
            Vector3 startPos = _muzzlePoint.position;
            
            while (time < 0.1f)
            {
                trail.transform.position = Vector3.Lerp(startPos, hitPoint, time / 0.1f);
                time += Time.deltaTime;
                yield return null;
            }
            
            trail.transform.position = hitPoint;
            Destroy(trail.gameObject, trail.time);
        }

        private void ApplyRecoil(Vector2 recoil)
        {
            // Apply to camera/weapon visuals
            if (_owner != null && Camera.main != null)
            {
                // Camera recoil would be handled by a camera controller
            }
        }

        private void PlayFireEffects()
        {
            if (_muzzleFlashEffect != null)
            {
                var flash = Instantiate(_muzzleFlashEffect, _muzzlePoint.position, _muzzlePoint.rotation, _muzzlePoint);
                Destroy(flash, 0.1f);
            }
        }

        private void PlayFireSound()
        {
            if (_audioSource == null) return;

            AudioClip fireClip = _muzzleAttachment != null && _muzzleAttachment.isSuppressor ? 
                _weaponData.fireSuppressedSound : _weaponData.fireSound;

            if (fireClip != null)
            {
                _audioSource.pitch = Random.Range(0.95f, 1.05f);
                _audioSource.PlayOneShot(fireClip);
            }
        }

        private void TryAutoReload()
        {
            if (_reserveAmmo > 0 && !_isReloading)
            {
                Reload();
            }
        }

        /// <summary>
        /// Reload the weapon.
        /// </summary>
        public void Reload()
        {
            if (_isReloading || _currentAmmo >= _weaponData.magazineSize || _reserveAmmo <= 0) return;

            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            _isReloading = true;

            // Determine reload time
            float reloadTime = _currentAmmo > 0 ? _weaponData.reloadTime : _weaponData.reloadEmptyTime;
            if (_magazineAttachment != null) reloadTime *= _magazineAttachment.reloadSpeedModifier;

            // Play reload animation/sound
            if (_audioSource != null && _weaponData.reloadSound != null)
            {
                _audioSource.PlayOneShot(_currentAmmo > 0 ? _weaponData.reloadSound : _weaponData.reloadEmptySound);
            }

            yield return new WaitForSeconds(reloadTime);

            // Calculate ammo to add
            int ammoToAdd = Mathf.Min(_weaponData.magazineSize - _currentAmmo, _reserveAmmo);
            _currentAmmo += ammoToAdd;
            _reserveAmmo -= ammoToAdd;

            _isReloading = false;

            EventBus.Raise(new WeaponReloadedEvent
            {
                PlayerId = _owner != null ? _owner.GetInstanceID().ToString() : "",
                WeaponId = _weaponData.weaponId,
                WasEmpty = _currentAmmo == 0
            });
        }

        /// <summary>
        /// Switch to next fire mode.
        /// </summary>
        public void SwitchFireMode()
        {
            if (_weaponData.availableFireModes.Length <= 1) return;
            _currentFireMode = (_currentFireMode + 1) % _weaponData.availableFireModes.Length;
            Debug.Log($"[WeaponController] Switched to {_weaponData.availableFireModes[_currentFireMode]}");
        }

        /// <summary>
        /// Start aiming down sights.
        /// </summary>
        public void StartAim()
        {
            _isAiming = true;
        }

        /// <summary>
        /// Stop aiming down sights.
        /// </summary>
        public void StopAim()
        {
            _isAiming = false;
        }

        /// <summary>
        /// Equip the weapon.
        /// </summary>
        public void Equip()
        {
            _isEquipped = true;
            gameObject.SetActive(true);

            if (_audioSource != null && _weaponData.equipSound != null)
            {
                _audioSource.PlayOneShot(_weaponData.equipSound);
            }
        }

        /// <summary>
        /// Unequip the weapon.
        /// </summary>
        public void Unequip()
        {
            _isEquipped = false;
            _isAiming = false;
            _isFiring = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Add ammo to the reserve.
        /// </summary>
        public void AddAmmo(int amount)
        {
            _reserveAmmo = Mathf.Min(_reserveAmmo + amount, _weaponData.maxReserveAmmo);
        }

        /// <summary>
        /// Get current accuracy modifier.
        /// </summary>
        public float GetAccuracy()
        {
            float accuracy = _currentAccuracy;
            if (_gripAttachment != null) accuracy *= _gripAttachment.accuracyModifier;
            return accuracy;
        }

        /// <summary>
        /// Get current damage modifier.
        /// </summary>
        public float GetDamageModifier()
        {
            float modifier = 1f;
            if (_muzzleAttachment != null) modifier *= _muzzleAttachment.damageModifier;
            return modifier;
        }

        /// <summary>
        /// Set the owner of this weapon.
        /// </summary>
        public void SetOwner(PlayerCharacterController owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Attach an attachment to the weapon.
        /// </summary>
        public bool AttachAttachment(AttachmentData attachment)
        {
            if (attachment == null) return false;

            switch (attachment.slotType)
            {
                case AttachmentSlotType.Sight:
                    _sightAttachment = attachment;
                    break;
                case AttachmentSlotType.Muzzle:
                    _muzzleAttachment = attachment;
                    break;
                case AttachmentSlotType.Grip:
                    _gripAttachment = attachment;
                    break;
                case AttachmentSlotType.Magazine:
                    _magazineAttachment = attachment;
                    break;
                default:
                    return false;
            }

            // Apply visual changes
            UpdateAttachmentVisuals(attachment);
            return true;
        }

        private void UpdateAttachmentVisuals(AttachmentData attachment)
        {
            // Instance the attachment model on the weapon
            if (attachment.attachmentPrefab != null && _weaponVisual != null)
            {
                var attachPoint = FindAttachmentPoint(attachment.slotType);
                if (attachPoint != null)
                {
                    Instantiate(attachment.attachmentPrefab, attachPoint.position, attachPoint.rotation, attachPoint);
                }
            }
        }

        private Transform FindAttachmentPoint(AttachmentSlotType slotType)
        {
            string pointName = slotType switch
            {
                AttachmentSlotType.Sight => "attach_sight",
                AttachmentSlotType.Muzzle => "attach_muzzle",
                AttachmentSlotType.Grip => "attach_grip",
                AttachmentSlotType.Magazine => "attach_magazine",
                _ => null
            };

            if (pointName != null && _weaponVisual != null)
            {
                return _weaponVisual.Find(pointName);
            }
            return null;
        }

        /// <summary>
        /// Initialize the weapon with data.
        /// </summary>
        public void Initialize(WeaponData data)
        {
            _weaponData = data;
            _currentAmmo = data.magazineSize;
            _reserveAmmo = data.maxReserveAmmo;
            _currentAccuracy = data.baseAccuracy;
            _currentFireMode = 0;
        }

        /// <summary>
        /// Get total ammo (magazine + reserve).
        /// </summary>
        public int GetTotalAmmo() => _currentAmmo + _reserveAmmo;
    }
}
