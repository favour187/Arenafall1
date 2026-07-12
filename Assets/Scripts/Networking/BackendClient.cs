using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using ArenaFall.Core;
using ArenaFall.Data;
using ArenaFall.Events;

namespace ArenaFall.Networking
{
    /// <summary>
    /// REST & WebSocket/HTTP Client Bridge that connects the Unity client directly to our Node.js backend.
    /// Handles Authentication, Profile Sync, Matchmaking Queueing, Leaderboards, and Live-Ops.
    /// </summary>
    public class BackendClient : MonoBehaviour
    {
        public static BackendClient Instance { get; private set; }

        [Header("Backend Configuration")]
        [SerializeField] private string _baseUrl = "https://arenafall-api.onrender.com/api/v1";
        [SerializeField] private float _requestTimeout = 10f;

        private string _authToken;
        private string _refreshToken;
        private PlayerSaveData _cachedProfile;
        private bool _isAuthenticated;

        public string AuthToken => _authToken;
        public bool IsAuthenticated => _isAuthenticated;
        public PlayerSaveData CachedProfile => _cachedProfile;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadStoredTokens();
                Debug.Log("[BackendClient] Initialized API Bridge to " + _baseUrl);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void LoadStoredTokens()
        {
            _authToken = PlayerPrefs.GetString("arenafall_auth_token", string.Empty);
            _refreshToken = PlayerPrefs.GetString("arenafall_refresh_token", string.Empty);
            if (!string.IsNullOrEmpty(_authToken))
            {
                _isAuthenticated = true;
            }
        }

        private void SaveTokens(string token, string refresh)
        {
            _authToken = token;
            _refreshToken = refresh;
            _isAuthenticated = !string.IsNullOrEmpty(token);
            PlayerPrefs.SetString("arenafall_auth_token", token);
            PlayerPrefs.SetString("arenafall_refresh_token", refresh);
            PlayerPrefs.Save();
        }

        public void Logout()
        {
            SaveTokens(string.Empty, string.Empty);
            _cachedProfile = null;
            Debug.Log("[BackendClient] Logged out successfully");
        }

        // ─── AUTHENTICATION ENDPOINTS ──────────────────────────────

        public void Login(string email, string password, Action<bool, string> onComplete)
        {
            StartCoroutine(LoginCoroutine(email, password, onComplete));
        }

        private IEnumerator LoginCoroutine(string email, string password, Action<bool, string> onComplete)
        {
            string url = $"{_baseUrl}/auth/login";
            string payload = JsonUtility.ToJson(new LoginRequest { email = email, password = password });

            using (UnityWebRequest req = CreatePostRequest(url, payload))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
                    if (response != null && response.success)
                    {
                        SaveTokens(response.token, response.refreshToken);
                        if (response.player != null)
                        {
                            _cachedProfile = response.player;
                        }
                        Debug.Log("[BackendClient] Login successful! Welcome " + (_cachedProfile?.playerName ?? email));
                        onComplete?.Invoke(true, "Login successful!");
                    }
                    else
                    {
                        onComplete?.Invoke(false, response?.message ?? "Invalid login credentials.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[BackendClient] Login failed: {req.error}");
                    onComplete?.Invoke(false, req.error);
                }
            }
        }

        public void Register(string email, string username, string password, Action<bool, string> onComplete)
        {
            StartCoroutine(RegisterCoroutine(email, username, password, onComplete));
        }

        private IEnumerator RegisterCoroutine(string email, string username, string password, Action<bool, string> onComplete)
        {
            string url = $"{_baseUrl}/auth/register";
            string payload = JsonUtility.ToJson(new RegisterRequest { email = email, username = username, password = password });

            using (UnityWebRequest req = CreatePostRequest(url, payload))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
                    if (response != null && response.success)
                    {
                        SaveTokens(response.token, response.refreshToken);
                        if (response.player != null) _cachedProfile = response.player;
                        Debug.Log("[BackendClient] Registration successful for " + username);
                        onComplete?.Invoke(true, "Registration successful!");
                    }
                    else
                    {
                        onComplete?.Invoke(false, response?.message ?? "Registration failed.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[BackendClient] Registration failed: {req.error}");
                    onComplete?.Invoke(false, req.error);
                }
            }
        }

        // ─── INSTANT MOBILE & IOS GUEST LOGIN ──────────────────────
        public void LoginAsGuest(Action<bool, string> onComplete)
        {
            StartCoroutine(LoginAsGuestCoroutine(onComplete));
        }

        private IEnumerator LoginAsGuestCoroutine(Action<bool, string> onComplete)
        {
            string url = $"{_baseUrl}/auth/guest";
            using (UnityWebRequest req = CreatePostRequest(url, "{}"))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<AuthResponse>(req.downloadHandler.text);
                    if (response != null && response.success)
                    {
                        SaveTokens(response.token, response.refreshToken);
                        if (response.player != null) _cachedProfile = response.player;
                        Debug.Log("[BackendClient] Guest login successful! Welcome " + (_cachedProfile?.playerName ?? "Guest"));
                        onComplete?.Invoke(true, "Guest login successful!");
                    }
                    else
                    {
                        onComplete?.Invoke(false, response?.message ?? "Guest login failed.");
                    }
                }
                else
                {
                    // Offline fallback if network fails
                    _cachedProfile = new PlayerSaveData { playerId = "offline_guest", playerName = "Offline_Guest_77", level = 1, credits = 1500 };
                    _isAuthenticated = true;
                    Debug.Log("[BackendClient] Offline Guest fallback active.");
                    onComplete?.Invoke(true, "Offline Guest login active!");
                }
            }
        }

        // ─── PLAYER PROFILE & PROGRESSION ──────────────────────────

        public void FetchPlayerProfile(Action<PlayerSaveData> onComplete)
        {
            StartCoroutine(FetchProfileCoroutine(onComplete));
        }

        private IEnumerator FetchProfileCoroutine(Action<PlayerSaveData> onComplete)
        {
            string url = $"{_baseUrl}/player/profile";
            using (UnityWebRequest req = CreateGetRequest(url))
            {
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    var response = JsonUtility.FromJson<ProfileResponse>(req.downloadHandler.text);
                    if (response != null && response.success && response.data != null)
                    {
                        _cachedProfile = response.data;
                        onComplete?.Invoke(_cachedProfile);
                        yield break;
                    }
                }
                Debug.LogWarning("[BackendClient] Failed to fetch profile from server. Using local default.");
                onComplete?.Invoke(_cachedProfile ?? new PlayerSaveData { playerName = "Vanguard_Soldier" });
            }
        }

        public void SyncStatsAfterMatch(int kills, int damage, int placement, float survivalTime)
        {
            StartCoroutine(SyncStatsCoroutine(kills, damage, placement, survivalTime));
        }

        private IEnumerator SyncStatsCoroutine(int kills, int damage, int placement, float survivalTime)
        {
            string url = $"{_baseUrl}/player/stats";
            string payload = JsonUtility.ToJson(new MatchResultPayload
            {
                kills = kills,
                damageDealt = damage,
                placement = placement,
                survivalTime = survivalTime
            });

            using (UnityWebRequest req = CreatePostRequest(url, payload))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[BackendClient] Match stats synced successfully to server!");
                }
            }
        }

        // ─── MATCHMAKING QUEUE ─────────────────────────────────────

        public void JoinMatchmakingQueue(string gameMode, Action<bool, MatchmakingStatus> onStatusUpdate)
        {
            StartCoroutine(MatchmakingCoroutine(gameMode, onStatusUpdate));
        }

        private IEnumerator MatchmakingCoroutine(string gameMode, Action<bool, MatchmakingStatus> onStatusUpdate)
        {
            string url = $"{_baseUrl}/match/queue";
            string payload = JsonUtility.ToJson(new QueueRequest { gameMode = gameMode });

            using (UnityWebRequest req = CreatePostRequest(url, payload))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var status = JsonUtility.FromJson<MatchmakingStatus>(req.downloadHandler.text);
                    onStatusUpdate?.Invoke(true, status);
                }
                else
                {
                    onStatusUpdate?.Invoke(false, new MatchmakingStatus { status = "error", message = req.error });
                }
            }
        }

        // ─── HELPERS ───────────────────────────────────────────────

        private UnityWebRequest CreatePostRequest(string url, string jsonPayload)
        {
            UnityWebRequest req = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (_isAuthenticated && !string.IsNullOrEmpty(_authToken))
            {
                req.SetRequestHeader("Authorization", $"Bearer {_authToken}");
            }
            req.timeout = (int)_requestTimeout;
            return req;
        }

        private UnityWebRequest CreateGetRequest(string url)
        {
            UnityWebRequest req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Content-Type", "application/json");
            if (_isAuthenticated && !string.IsNullOrEmpty(_authToken))
            {
                req.SetRequestHeader("Authorization", $"Bearer {_authToken}");
            }
            req.timeout = (int)_requestTimeout;
            return req;
        }

        // ─── DATA STRUCTURES ───────────────────────────────────────

        [Serializable]
        private class LoginRequest { public string email; public string password; }

        [Serializable]
        private class RegisterRequest { public string email; public string username; public string password; }

        [Serializable]
        private class QueueRequest { public string gameMode; }

        [Serializable]
        private class MatchResultPayload { public int kills; public int damageDealt; public int placement; public float survivalTime; }

        [Serializable]
        public class AuthResponse
        {
            public bool success;
            public string message;
            public string token;
            public string refreshToken;
            public PlayerSaveData player;
        }

        [Serializable]
        public class ProfileResponse
        {
            public bool success;
            public PlayerSaveData data;
        }

        [Serializable]
        public class MatchmakingStatus
        {
            public string status;
            public string ticketId;
            public int estimatedWaitTime;
            public string matchId;
            public string serverIp;
            public int serverPort;
            public string message;
        }
    }
}
