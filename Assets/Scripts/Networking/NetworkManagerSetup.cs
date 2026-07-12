using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using ArenaFall.Core;
using ArenaFall.Managers;

namespace ArenaFall.Networking
{
    /// <summary>
    /// Automatically initializes Unity Netcode (NetworkManager + UnityTransport) at startup.
    /// Manages Client, Host, and Headless Dedicated Server (-batchmode) configurations seamlessly.
    /// </summary>
    public class NetworkManagerSetup : MonoBehaviour
    {
        public static NetworkManagerSetup Instance { get; private set; }

        [Header("Transport Settings")]
        [SerializeField] private string _serverAddress = "127.0.0.1";
        [SerializeField] private ushort _serverPort = 7777;

        private NetworkManager _networkManager;
        private UnityTransport _transport;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeNetcode();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void InitializeNetcode()
        {
            if (NetworkManager.Singleton != null)
            {
                _networkManager = NetworkManager.Singleton;
                Debug.Log("[NetworkManagerSetup] Using existing NetworkManager instance.");
                return;
            }

            // Create NetworkManager GameObject
            var netObj = new GameObject("[AUTO] NetworkManager");
            DontDestroyOnLoad(netObj);

            _networkManager = netObj.AddComponent<NetworkManager>();
            _transport = netObj.AddComponent<UnityTransport>();

            // Configure UnityTransport with High-Capacity Buffers for 60-100 Player Lobbies
            _transport.SetConnectionData(_serverAddress, _serverPort);
            _transport.MaxPacketQueueSize = 1024;
            _transport.MaxPayloadSize = 6144;
            _networkManager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = _transport,
                PlayerPrefab = null, // Will be dynamically registered when needed
                TickRate = 30,
                ClientConnectionBufferTimeout = 20,
                EnableSceneManagement = true
            };

            Debug.Log("[NetworkManagerSetup] ✓ NetworkManager & UnityTransport configured.");

            // Check if running as Dedicated Server in headless batchmode
            if (Application.isBatchMode || System.Environment.CommandLine.Contains("-batchmode"))
            {
                Debug.Log("[NetworkManagerSetup] Headless server detected (-batchmode). Starting Dedicated Server...");
                StartServer();
            }
        }

        public void StartHost()
        {
            if (_networkManager != null && !_networkManager.IsListening)
            {
                _networkManager.StartHost();
                Debug.Log("[NetworkManagerSetup] Started Match Host on port " + _serverPort);
            }
        }

        public void StartClient(string targetIp = "127.0.0.1", ushort targetPort = 7777)
        {
            if (_networkManager != null && !_networkManager.IsListening)
            {
                _transport.SetConnectionData(targetIp, targetPort);
                _networkManager.StartClient();
                Debug.Log($"[NetworkManagerSetup] Connecting as Client to {targetIp}:{targetPort}...");
            }
        }

        public void StartServer()
        {
            if (_networkManager != null && !_networkManager.IsListening)
            {
                _networkManager.StartServer();
                Debug.Log("[NetworkManagerSetup] Dedicated Server active on port " + _serverPort);
            }
        }

        public void Disconnect()
        {
            if (_networkManager != null && _networkManager.IsListening)
            {
                _networkManager.Shutdown();
                Debug.Log("[NetworkManagerSetup] Network session disconnected.");
            }
        }
    }
}
