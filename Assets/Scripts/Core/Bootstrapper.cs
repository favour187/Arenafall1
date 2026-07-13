using UnityEngine;
using ArenaFall.Managers;

namespace ArenaFall.Core
{
    /// <summary>
    /// Initial bootstrapper that runs before anything else.
    /// Sets up core systems and dependencies for the game.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        [Header("Manager Prefabs")]
        [SerializeField] private GameObject[] _coreManagerPrefabs;
        [SerializeField] private GameObject[] _gameplayManagerPrefabs;
        [SerializeField] private GameObject[] _uiManagerPrefabs;

        [Header("Configuration")]
        [SerializeField] public bool _initializeOnAwake = true;
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private bool _vsyncEnabled = false;

        private bool _initialized;

        private void Awake()
        {
            if (_initializeOnAwake && !_initialized)
            {
                Bootstrap();
            }
        }

        /// <summary>
        /// Run the bootstrap process to initialize all core systems.
        /// </summary>
        public void Bootstrap()
        {
            if (_initialized)
            {
                Debug.LogWarning("[Bootstrapper] Already initialized");
                return;
            }

            Debug.Log("=== Arena Fall Bootstrapper ===");
            Debug.Log("[Bootstrapper] Starting initialization...");

            // Application settings
            Application.targetFrameRate = _targetFrameRate;
            QualitySettings.vSyncCount = _vsyncEnabled ? 1 : 0;
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Initialize Service Locator
            ServiceLocator.Initialize();

            // Spawn core managers
            SpawnManagers(_coreManagerPrefabs, "Core");

            // Initialize save system
            var saveManager = FindObjectOfType<SaveManager>();
            if (saveManager == null)
            {
                GameObject saveObj = new GameObject("SaveManager");
                saveObj.AddComponent<SaveManager>();
                DontDestroyOnLoad(saveObj);
            }

            // Spawn gameplay managers
            SpawnManagers(_gameplayManagerPrefabs, "Gameplay");

            // Spawn UI managers
            SpawnManagers(_uiManagerPrefabs, "UI");

            // Register GameManager
            var gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                ServiceLocator.Register<GameManager>(gameManager);
            }

            // Attach SceneAutoBuilder for automatic scene content generation
            if (FindObjectOfType<SceneAutoBuilder>() == null)
            {
                var builder = gameObject.AddComponent<SceneAutoBuilder>();
                Debug.Log("[Bootstrapper] SceneAutoBuilder attached");
            }

            // Initialize the GameManager
            var gm = ServiceLocator.Get<GameManager>();
            if (gm != null)
            {
                gm.InitializeGame();
            }

            _initialized = true;
            Debug.Log("[Bootstrapper] Arena Fall initialization complete!");
        }

        private void SpawnManagers(GameObject[] prefabs, string category)
        {
            if (prefabs == null) return;

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var instance = Instantiate(prefab);
                    instance.name = prefab.name;
                    DontDestroyOnLoad(instance);
                    Debug.Log($"[Bootstrapper] Spawned {category} manager: {prefab.name}");
                }
            }
        }

        /// <summary>
        /// Check if bootstrap has completed.
        /// </summary>
        public bool IsInitialized => _initialized;
    }
}
