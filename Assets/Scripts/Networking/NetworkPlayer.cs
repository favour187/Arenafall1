using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using ArenaFall.Core;
using ArenaFall.Events;
using ArenaFall.Interfaces;
using ArenaFall.Gameplay.Characters;
using ArenaFall.Managers;

namespace ArenaFall.Networking
{
    /// <summary>
    /// Networked player representation that syncs state across the network.
    /// Uses Unity Netcode for GameObjects.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(CharacterHealth))]
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private float _syncPositionRate = 0.05f;
        [SerializeField] private float _syncRotationRate = 0.1f;

        // Network variables
        private NetworkVariable<Vector3> _netPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<Vector2> _netRotation = new NetworkVariable<Vector2>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> _netHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<float> _netShield = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> _netIsAlive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> _netTeamId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<FixedString32Bytes> _netPlayerName = new NetworkVariable<FixedString32Bytes>("Player", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // Components
        private CharacterHealth _health;
        private PlayerCharacterController _controller;

        // Player data
        private string _playerId;
        private ulong _clientId;

        // Properties
        public string PlayerId => _playerId;
        public ulong ClientId => _clientId;
        public string PlayerName => _netPlayerName.Value.ToString();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _health = GetComponent<CharacterHealth>();
            _controller = GetComponent<PlayerCharacterController>();
            _clientId = OwnerClientId;

            if (IsServer)
            {
                _playerId = $"player_{NetworkManager.Singleton.ConnectedClients.Count}";
                _netPlayerName.Value = $"Player {OwnerClientId}";
            }

            if (IsOwner)
            {
                // This is our local player
                SetupLocalPlayer();
            }
            else
            {
                // Remote player
                SetupRemotePlayer();
            }

            // Subscribe to network variable changes
            _netHealth.OnValueChanged += OnHealthChanged;
            _netIsAlive.OnValueChanged += OnAliveStateChanged;
        }

        private void SetupLocalPlayer()
        {
            // Enable local input and camera
            if (_controller != null)
            {
                _controller.enabled = true;
            }

            var cameraManager = ServiceLocator.Get<CameraManager>();
            if (cameraManager != null)
            {
                cameraManager.SetTarget(transform);
            }

            var hud = FindObjectOfType<UI.HUD.HUDController>();
            if (hud != null)
            {
                hud.SetPlayer(gameObject);
            }

            // Register in match
            var matchManager = ServiceLocator.Get<MatchManager>();
            if (matchManager != null)
            {
                matchManager.RegisterPlayer(_playerId, _netPlayerName.Value.ToString());
            }

            Debug.Log($"[NetworkPlayer] Local player spawned: {_playerId}");
        }

        private void SetupRemotePlayer()
        {
            // Disable local input for remote players
            if (_controller != null)
            {
                _controller.enabled = false;
            }

            // Remove audio listener
            var audioListener = GetComponent<AudioListener>();
            if (audioListener != null)
            {
                Destroy(audioListener);
            }

            Debug.Log($"[NetworkPlayer] Remote player spawned: {_playerId}");
        }

        private void Update()
        {
            if (!IsSpawned) return;

            if (IsOwner)
            {
                // Send local data to server
                SendLocalState();
            }

            if (IsServer)
            {
                // Handle server-side logic
                UpdateServerState();
            }

            // Apply network state for non-owned objects
            if (!IsOwner)
            {
                ApplyRemoteState();
            }
        }

        private void SendLocalState()
        {
            if (_controller == null) return;

            // Send position, rotation, health to server
            if (Time.frameCount % Mathf.RoundToInt(_syncPositionRate / Time.deltaTime) == 0)
            {
                SubmitPositionServerRpc(transform.position, transform.eulerAngles.y);
            }
        }

        [ServerRpc]
        private void SubmitPositionServerRpc(Vector3 position, float rotation)
        {
            _netPosition.Value = position;
            _netRotation.Value = new Vector2(rotation, 0);
        }

        private void UpdateServerState()
        {
            // Update health/shield from health component
            if (_health != null)
            {
                _netHealth.Value = _health.CurrentHealth;
                _netShield.Value = _health.CurrentShield;
                _netIsAlive.Value = _health.IsAlive;
            }
        }

        private void ApplyRemoteState()
        {
            // Interpolate to network position
            transform.position = Vector3.Lerp(transform.position, _netPosition.Value, 10f * Time.deltaTime);
            
            // Apply rotation
            Vector3 euler = transform.eulerAngles;
            euler.y = Mathf.LerpAngle(euler.y, _netRotation.Value.x, 10f * Time.deltaTime);
            transform.eulerAngles = euler;
        }

        /// <summary>
        /// Damage another network player.
        /// </summary>
        [ServerRpc]
        public void DealDamageServerRpc(ulong targetId, float damage, DamageType damageType)
        {
            var target = NetworkManager.Singleton.ConnectedClients[targetId].PlayerObject;
            if (target != null)
            {
                var health = target.GetComponent<CharacterHealth>();
                if (health != null)
                {
                    health.TakeDamage(damage, damageType, gameObject);
                }
            }
        }

        /// <summary>
        /// Notify all clients of a player elimination.
        /// </summary>
        [ClientRpc]
        public void NotifyEliminationClientRpc(ulong victimId, ulong killerId, string weaponId)
        {
            EventBus.Raise(new PlayerEliminatedEvent
            {
                PlayerId = victimId.ToString(),
                KillerId = killerId.ToString(),
                WeaponId = weaponId
            });
        }

        /// <summary>
        /// Initialize player data.
        /// </summary>
        public void Initialize(string playerName, int teamId, string characterId)
        {
            _netPlayerName.Value = playerName;
            _netTeamId.Value = teamId;
            _playerId = $"player_{NetworkManager.Singleton.ConnectedClients.Count}";

            if (_health != null)
            {
                _health.Initialize(_playerId, teamId);
            }
        }

        private void OnHealthChanged(float oldValue, float newValue)
        {
            EventBus.Raise(new PlayerHealthChangedEvent
            {
                PlayerId = _playerId,
                OldHealth = oldValue,
                NewHealth = newValue,
                MaxHealth = _health != null ? _health.MaxHealth : 100f
            });
        }

        private void OnAliveStateChanged(bool oldValue, bool newValue)
        {
            if (!newValue)
            {
                EventBus.Raise(new PlayerDiedEvent
                {
                    PlayerId = _playerId,
                    KillerId = "unknown",
                    DamageType = DamageType.Bullet,
                    DeathPosition = transform.position
                });
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _netHealth.OnValueChanged -= OnHealthChanged;
            _netIsAlive.OnValueChanged -= OnAliveStateChanged;
        }
    }
}
