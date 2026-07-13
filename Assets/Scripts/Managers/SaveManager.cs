using System;
using System.IO;
using UnityEngine;
using ArenaFall.Core;
using ArenaFall.Data;
using ArenaFall.Events;

namespace ArenaFall.Managers
{
    /// <summary>
    /// Manages saving and loading player data to local storage and cloud.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string _saveFileName = "player_save.json";
        [SerializeField] private bool _autoSave = true;
        [SerializeField] private float _autoSaveInterval = 120f;
        [SerializeField] private bool _enableCloudSave = false;

        private PlayerSaveData _currentSave;
        private string _saveFilePath;
        private float _autoSaveTimer;
        private bool _isDirty;

        public static SaveManager Instance { get; private set; }
        public PlayerSaveData CurrentSave => _currentSave;
        public bool IsLoaded { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register<SaveManager>(this);

            _saveFilePath = Path.Combine(Application.persistentDataPath, _saveFileName);
        }

        private void Start()
        {
            LoadGame();
        }

        private void Update()
        {
            if (!_autoSave || !IsLoaded || !_isDirty) return;

            _autoSaveTimer += Time.unscaledDeltaTime;
            if (_autoSaveTimer >= _autoSaveInterval)
            {
                _autoSaveTimer = 0f;
                SaveGame();
            }
        }

        /// <summary>
        /// Save game data to local storage.
        /// </summary>
        public void SaveGame()
        {
            try
            {
                string json = JsonUtility.ToJson(_currentSave, true);
                File.WriteAllText(_saveFilePath, json);
                _isDirty = false;
                Debug.Log("[SaveManager] Game saved successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save: {ex.Message}");
            }
        }

        /// <summary>
        /// Load game data from local storage.
        /// </summary>
        public void LoadGame()
        {
            try
            {
                if (File.Exists(_saveFilePath))
                {
                    string json = File.ReadAllText(_saveFilePath);
                    _currentSave = JsonUtility.FromJson<PlayerSaveData>(json);
                    Debug.Log("[SaveManager] Game loaded successfully");
                }
                else
                {
                    Debug.Log("[SaveManager] No save found, creating new save");
                    _currentSave = CreateNewSave();
                    SaveGame();
                }

                IsLoaded = true;
                _isDirty = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to load: {ex.Message}");
                _currentSave = CreateNewSave();
                IsLoaded = true;
            }
        }

        /// <summary>
        /// Create a new save data with default values.
        /// </summary>
        private PlayerSaveData CreateNewSave()
        {
            var save = new PlayerSaveData
            {
                playerId = Guid.NewGuid().ToString(),
                playerName = $"Player{UnityEngine.Random.Range(1000, 9999)}",
                createdDate = DateTime.UtcNow,
                lastLoginDate = DateTime.UtcNow
            };

            // Add default loadout
            save.loadouts.Add(new PlayerLoadout
            {
                loadoutName = "Default",
                characterId = "default_vanguard"
            });

            return save;
        }

        /// <summary>
        /// Mark the save as dirty, triggering auto-save.
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Force an immediate save.
        /// </summary>
        public void ForceSave()
        {
            _currentSave.lastLoginDate = DateTime.UtcNow;
            SaveGame();
        }

        /// <summary>
        /// Delete all save data.
        /// </summary>
        public void DeleteSave()
        {
            if (File.Exists(_saveFilePath))
            {
                File.Delete(_saveFilePath);
            }
            _currentSave = CreateNewSave();
            IsLoaded = false;
            Debug.Log("[SaveManager] Save deleted");
        }

        /// <summary>
        /// Get the player's display name.
        /// </summary>
        public string GetPlayerName()
        {
            return _currentSave?.playerName ?? "Unknown";
        }

        /// <summary>
        /// Set the player's display name.
        /// </summary>
        public void SetPlayerName(string name)
        {
            if (_currentSave != null)
            {
                _currentSave.playerName = name;
                MarkDirty();
            }
        }

        /// <summary>
        /// Add XP and handle level ups.
        /// </summary>
        public void AddXP(int amount)
        {
            if (_currentSave == null) return;

            _currentSave.xp += amount;
            _currentSave.totalXP += amount;

            // Check level up (simplified formula)
            int xpForNextLevel = GetXPForLevel(_currentSave.level);
            while (_currentSave.xp >= xpForNextLevel)
            {
                _currentSave.xp -= xpForNextLevel;
                _currentSave.level++;
                xpForNextLevel = GetXPForLevel(_currentSave.level);

                EventBus.Raise(new LevelUpEvent
                {
                    NewLevel = _currentSave.level,
                    PreviousLevel = _currentSave.level - 1
                });
            }

            MarkDirty();
        }

        /// <summary>
        /// Get XP required for a given level.
        /// </summary>
        public static int GetXPForLevel(int level)
        {
            // Formula: 100 * level * 1.5^level
            return Mathf.RoundToInt(100 * level * Mathf.Pow(1.5f, level / 10f));
        }

        private void OnDestroy()
        {
            if (Instance == this && _currentSave != null)
            {
                _currentSave.lastLoginDate = DateTime.UtcNow;
                SaveGame();
            }
        }

        private void OnApplicationQuit()
        {
            if (_currentSave != null)
            {
                _currentSave.lastLoginDate = DateTime.UtcNow;
                SaveGame();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _currentSave != null)
            {
                SaveGame();
            }
        }
    }
}
