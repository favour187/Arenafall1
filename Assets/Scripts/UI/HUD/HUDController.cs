using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ArenaFall.Core;
using ArenaFall.Events;
using ArenaFall.Gameplay.Characters;
using ArenaFall.Managers;

namespace ArenaFall.UI.HUD
{
    /// <summary>
    /// Controls the in-game HUD display including health, shields, ammo, minimap, and notifications.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Health & Shield")]
        [SerializeField] private Slider _healthBar;
        [SerializeField] private Slider _shieldBar;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _shieldText;
        [SerializeField] private Image _healthFillImage;
        [SerializeField] private Image _shieldFillImage;
        [SerializeField] private Color _healthyColor = Color.green;
        [SerializeField] private Color _damagedColor = Color.red;

        [Header("Weapon")]
        [SerializeField] private TextMeshProUGUI _weaponNameText;
        [SerializeField] private TextMeshProUGUI _ammoText;
        [SerializeField] private TextMeshProUGUI _reserveAmmoText;
        [SerializeField] private Image _weaponIcon;
        [SerializeField] private GameObject _reloadIndicator;

        [Header("Match Info")]
        [SerializeField] private TextMeshProUGUI _playerCountText;
        [SerializeField] private TextMeshProUGUI _killCountText;
        [SerializeField] private TextMeshProUGUI _placementText;
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("Minimap")]
        [SerializeField] private RawImage _minimapImage;
        [SerializeField] private RectTransform _playerIcon;
        [SerializeField] private RectTransform _zoneIndicator;
        [SerializeField] private float _minimapScale = 10f;

        [Header("Notifications")]
        [SerializeField] private GameObject _killFeedPrefab;
        [SerializeField] private Transform _killFeedContainer;
        [SerializeField] private GameObject _notificationPrefab;
        [SerializeField] private Transform _notificationContainer;
        [SerializeField] private int _maxKillFeedItems = 5;
        [SerializeField] private float _notificationDuration = 3f;

        [Header("Interaction")]
        [SerializeField] private TextMeshProUGUI _interactionText;
        [SerializeField] private Image _interactionProgressBar;
        [SerializeField] private GameObject _interactionPanel;

        [Header("Compass")]
        [SerializeField] private RectTransform _compassBar;
        [SerializeField] private GameObject _compassTickPrefab;

        // References
        private CharacterHealth _playerHealth;
        private PlayerCharacterController _playerController;
        private CameraManager _cameraManager;
        private EventBus _eventBus;

        private void Awake()
        {
            _cameraManager = ServiceLocator.Get<CameraManager>();
        }

        private void Start()
        {
            EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<PlayerShieldChangedEvent>(OnShieldChanged);
            EventBus.Subscribe<WeaponFiredEvent>(OnWeaponFired);
            EventBus.Subscribe<PlayerEliminatedEvent>(OnPlayerEliminated);
            EventBus.Subscribe<MatchStartedEvent>(OnMatchStarted);

            InitializeHUD();
        }

        private void Update()
        {
            UpdateMatchInfo();
            UpdateMinimap();
        }

        private void InitializeHUD()
        {
            if (GetComponent<MobileTouchControls>() == null)
            {
                gameObject.AddComponent<MobileTouchControls>();
            }

            if (_interactionPanel != null)
                _interactionPanel.SetActive(false);

            if (_reloadIndicator != null)
                _reloadIndicator.SetActive(false);

            // Init kill feed
            if (_killFeedContainer != null)
            {
                foreach (Transform child in _killFeedContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Set the player reference for HUD data.
        /// </summary>
        public void SetPlayer(GameObject player)
        {
            if (player != null)
            {
                _playerHealth = player.GetComponent<CharacterHealth>();
                _playerController = player.GetComponent<PlayerCharacterController>();

                // Update initial values
                if (_playerHealth != null)
                {
                    UpdateHealthBar(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
                    UpdateShieldBar(_playerHealth.CurrentShield, _playerHealth.MaxShield);
                }
            }
        }

        /// <summary>
        /// Update the weapon display.
        /// </summary>
        public void UpdateWeaponDisplay(string weaponName, int currentAmmo, int reserveAmmo, Sprite icon)
        {
            if (_weaponNameText != null)
                _weaponNameText.text = weaponName;

            if (_ammoText != null)
                _ammoText.text = currentAmmo.ToString();

            if (_reserveAmmoText != null)
                _reserveAmmoText.text = reserveAmmo.ToString();

            if (_weaponIcon != null && icon != null)
                _weaponIcon.sprite = icon;

            // Flash ammo if low
            if (currentAmmo <= 5 && _ammoText != null)
            {
                _ammoText.color = Color.red;
            }
            else if (_ammoText != null)
            {
                _ammoText.color = Color.white;
            }
        }

        /// <summary>
        /// Show/hide reload indicator.
        /// </summary>
        public void SetReloading(bool reloading)
        {
            if (_reloadIndicator != null)
                _reloadIndicator.SetActive(reloading);
        }

        /// <summary>
        /// Show an interaction prompt.
        /// </summary>
        public void ShowInteraction(string text, float progress = -1f)
        {
            if (_interactionPanel != null)
                _interactionPanel.SetActive(true);

            if (_interactionText != null)
                _interactionText.text = text;

            if (_interactionProgressBar != null)
            {
                if (progress >= 0)
                {
                    _interactionProgressBar.fillAmount = progress;
                    _interactionProgressBar.gameObject.SetActive(true);
                }
                else
                {
                    _interactionProgressBar.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Hide interaction prompt.
        /// </summary>
        public void HideInteraction()
        {
            if (_interactionPanel != null)
                _interactionPanel.SetActive(false);
        }

        /// <summary>
        /// Add a kill feed entry.
        /// </summary>
        public void AddKillFeedEntry(string killer, string victim, string weapon)
        {
            if (_killFeedPrefab == null || _killFeedContainer == null) return;

            var entry = Instantiate(_killFeedPrefab, _killFeedContainer);
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"{killer} → {victim} [{weapon}]";
            }

            // Limit entries
            if (_killFeedContainer.childCount > _maxKillFeedItems)
            {
                Destroy(_killFeedContainer.GetChild(0).gameObject);
            }

            // Auto-remove after delay
            Destroy(entry.gameObject, 5f);
        }

        /// <summary>
        /// Show a notification.
        /// </summary>
        public void ShowNotification(string message, Color color)
        {
            if (_notificationPrefab == null || _notificationContainer == null) return;

            var notification = Instantiate(_notificationPrefab, _notificationContainer);
            var text = notification.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;
                text.color = color;
            }

            Destroy(notification.gameObject, _notificationDuration);
        }

        private void UpdateHealthBar(float current, float max)
        {
            if (_healthBar != null)
            {
                _healthBar.maxValue = max;
                _healthBar.value = current;
            }

            if (_healthText != null)
                _healthText.text = $"{Mathf.CeilToInt(current)}";

            if (_healthFillImage != null)
            {
                float percent = current / max;
                _healthFillImage.color = percent > 0.5f ? _healthyColor : 
                    percent > 0.25f ? Color.yellow : _damagedColor;
            }
        }

        private void UpdateShieldBar(float current, float max)
        {
            if (_shieldBar != null)
            {
                _shieldBar.maxValue = max;
                _shieldBar.value = current;
            }

            if (_shieldText != null)
                _shieldText.text = max > 0 ? $"{Mathf.CeilToInt(current)}" : "";
        }

        private void UpdateMatchInfo()
        {
            // Update kill count, player count, placement, timer
            // These would come from MatchManager
        }

        private void UpdateMinimap()
        {
            if (_playerIcon != null && _playerController != null)
            {
                // Update player icon position on minimap
                Vector3 pos = _playerController.Position;
                _playerIcon.anchoredPosition = new Vector2(pos.x / _minimapScale, pos.z / _minimapScale);

                // Update rotation
                _playerIcon.rotation = Quaternion.Euler(0, 0, -_playerController.transform.eulerAngles.y);
            }
        }

        private void OnHealthChanged(PlayerHealthChangedEvent evt)
        {
            if (_playerHealth != null && evt.PlayerId == _playerHealth.PlayerId)
            {
                UpdateHealthBar(evt.NewHealth, evt.MaxHealth);
            }
        }

        private void OnShieldChanged(PlayerShieldChangedEvent evt)
        {
            if (_playerHealth != null && evt.PlayerId == _playerHealth.PlayerId)
            {
                UpdateShieldBar(evt.NewShield, evt.MaxShield);
            }
        }

        private void OnWeaponFired(WeaponFiredEvent evt)
        {
            // Update ammo display
        }

        private void OnPlayerEliminated(PlayerEliminatedEvent evt)
        {
            AddKillFeedEntry(evt.KillerId, evt.PlayerId, evt.WeaponId);

            if (evt.KillerId == GetLocalPlayerId())
            {
                // Update kill count
                if (_killCountText != null)
                {
                    int current = int.Parse(_killCountText.text);
                    _killCountText.text = (current + 1).ToString();
                }
            }
        }

        private void OnMatchStarted(MatchStartedEvent evt)
        {
            if (_playerCountText != null)
                _playerCountText.text = evt.PlayerCount.ToString();

            if (_killCountText != null)
                _killCountText.text = "0";
        }

        private string GetLocalPlayerId()
        {
            return _playerHealth != null ? _playerHealth.PlayerId : "";
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<PlayerShieldChangedEvent>(OnShieldChanged);
            EventBus.Unsubscribe<WeaponFiredEvent>(OnWeaponFired);
            EventBus.Unsubscribe<PlayerEliminatedEvent>(OnPlayerEliminated);
            EventBus.Unsubscribe<MatchStartedEvent>(OnMatchStarted);
        }
    }
}
