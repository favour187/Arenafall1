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
    /// Arena Fall Tactical Battle Royale HUD Controller.
    /// Styled according to 04_UI_Style_Guide.md (Deep Navy #0A1628, Holographic Cyan #00D4FF, Neon Orange #FF6B35).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Arena Fall Health & Shield")]
        [SerializeField] private Slider _healthBar;
        [SerializeField] private Slider _shieldBar;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _shieldText;
        [SerializeField] private Image _healthFillImage;
        [SerializeField] private Image _shieldFillImage;
        [SerializeField] private Color _healthyColor = new Color(0.267f, 1f, 0.333f, 1f); // #44FF55 Success Green
        [SerializeField] private Color _damagedColor = new Color(1f, 0.133f, 0.267f, 1f); // #FF2244 Danger Red

        [Header("Weapon & Ammo")]
        [SerializeField] private TextMeshProUGUI _weaponNameText;
        [SerializeField] private TextMeshProUGUI _ammoText;
        [SerializeField] private TextMeshProUGUI _reserveAmmoText;
        [SerializeField] private Image _weaponIcon;
        [SerializeField] private GameObject _reloadIndicator;

        [Header("Match Telemetry")]
        [SerializeField] private TextMeshProUGUI _playerCountText;
        [SerializeField] private TextMeshProUGUI _killCountText;
        [SerializeField] private TextMeshProUGUI _placementText;
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("Minimap & Compass")]
        [SerializeField] private RawImage _minimapImage;
        [SerializeField] private RectTransform _playerIcon;
        [SerializeField] private RectTransform _zoneIndicator;
        [SerializeField] private float _minimapScale = 10f;
        [SerializeField] private RectTransform _compassBar;

        [Header("Notifications & Kill Feed")]
        [SerializeField] private GameObject _killFeedPrefab;
        [SerializeField] private Transform _killFeedContainer;
        [SerializeField] private GameObject _notificationPrefab;
        [SerializeField] private Transform _notificationContainer;
        [SerializeField] private int _maxKillFeedItems = 5;
        [SerializeField] private float _notificationDuration = 3f;

        [Header("Interaction Prompts")]
        [SerializeField] private TextMeshProUGUI _interactionText;
        [SerializeField] private Image _interactionProgressBar;
        [SerializeField] private GameObject _interactionPanel;

        // References
        private CharacterHealth _playerHealth;
        private PlayerCharacterController _playerController;
        private CameraManager _cameraManager;

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

            if (_killFeedContainer != null)
            {
                foreach (Transform child in _killFeedContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        public void SetPlayer(GameObject player)
        {
            if (player != null)
            {
                _playerHealth = player.GetComponent<CharacterHealth>();
                _playerController = player.GetComponent<PlayerCharacterController>();

                if (_playerHealth != null)
                {
                    UpdateHealthBar(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
                    UpdateShieldBar(_playerHealth.CurrentShield, _playerHealth.MaxShield);
                }
            }
        }

        public void UpdateWeaponDisplay(string weaponName, int currentAmmo, int reserveAmmo, Sprite icon)
        {
            if (_weaponNameText != null)
                _weaponNameText.text = weaponName.ToUpper();

            if (_ammoText != null)
                _ammoText.text = $"{currentAmmo} / {reserveAmmo}";

            if (_weaponIcon != null && icon != null)
                _weaponIcon.sprite = icon;

            if (currentAmmo <= 5 && _ammoText != null)
            {
                _ammoText.color = _damagedColor;
            }
            else if (_ammoText != null)
            {
                _ammoText.color = Color.white;
            }
        }

        public void SetReloading(bool reloading)
        {
            if (_reloadIndicator != null)
                _reloadIndicator.SetActive(reloading);
        }

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

        public void HideInteraction()
        {
            if (_interactionPanel != null)
                _interactionPanel.SetActive(false);
        }

        public void AddKillFeedEntry(string killer, string victim, string weapon)
        {
            if (_killFeedPrefab == null || _killFeedContainer == null) return;

            var entry = Instantiate(_killFeedPrefab, _killFeedContainer);
            var text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"💀 {killer} ➔ {victim} [{weapon}]";
                text.color = new Color(0f, 0.831f, 1f, 1f); // Holographic Cyan
            }

            if (_killFeedContainer.childCount > _maxKillFeedItems)
            {
                Destroy(_killFeedContainer.GetChild(0).gameObject);
            }

            Destroy(entry.gameObject, 5f);
        }

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
                _healthText.text = $"HP  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

            if (_healthFillImage != null)
            {
                float percent = current / max;
                _healthFillImage.color = percent > 0.4f ? _healthyColor : 
                    percent > 0.20f ? Color.yellow : _damagedColor;
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
                _shieldText.text = max > 0 ? $"EP  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}" : "";
        }

        private void UpdateMatchInfo()
        {
        }

        private void UpdateMinimap()
        {
            if (_playerIcon != null && _playerController != null)
            {
                Vector3 pos = _playerController.Position;
                _playerIcon.anchoredPosition = new Vector2(pos.x / _minimapScale, pos.z / _minimapScale);
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
        }

        private void OnPlayerEliminated(PlayerEliminatedEvent evt)
        {
            AddKillFeedEntry(evt.KillerId, evt.PlayerId, evt.WeaponId);

            if (evt.KillerId == GetLocalPlayerId())
            {
                if (_killCountText != null)
                {
                    int current = 0;
                    int.TryParse(_killCountText.text.Replace("KILLS: ", ""), out current);
                    _killCountText.text = $"KILLS: {current + 1}";
                }
            }
        }

        private void OnMatchStarted(MatchStartedEvent evt)
        {
            if (_playerCountText != null)
                _playerCountText.text = $"ALIVE: {evt.PlayerCount}";

            if (_killCountText != null)
                _killCountText.text = "KILLS: 0";
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
