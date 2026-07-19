using System.Collections.Generic;
using UnityEngine;
using ArenaFall.Core;
using ArenaFall.Events;

namespace ArenaFall.Managers
{
    /// <summary>
    /// Manages the lifecycle of a battle royale match.
    /// Controls start, progression, scoring, and end conditions.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        [Header("Match Settings")]
        [SerializeField] private int _maxPlayers = 60;
        [SerializeField] private int _minPlayersToStart = 1;
        [SerializeField] private float _lobbyWaitTime = 60f;
        [SerializeField] private float _dropPhaseTime = 30f;
        [SerializeField] private float _postMatchTime = 15f;

        [Header("Scoring")]
        [SerializeField] private int _killPoints = 100;
        [SerializeField] private int _winPoints = 500;
        [SerializeField] private int _top10Points = 200;
        [SerializeField] private int _top5Points = 300;
        [SerializeField] private int _survivalPointsPerMinute = 10;

        [Header("Prefabs")]
        [SerializeField] private GameObject _dropPodPrefab;
        [SerializeField] private GameObject _playerSpawnPrefab;

        // State
        private MatchPhase _currentPhase = MatchPhase.None;
        private float _phaseTimer;
        private int _alivePlayerCount;
        private int _totalPlayerCount;
        private Dictionary<string, PlayerMatchData> _players = new();
        private List<string> _eliminationOrder = new();
        private float _matchStartTime;

        // Properties
        public MatchPhase CurrentPhase => _currentPhase;
        public int AlivePlayerCount => _alivePlayerCount;
        public int TotalPlayerCount => _totalPlayerCount;
        public float MatchElapsedTime => Time.time - _matchStartTime;

        public static MatchManager Instance { get; private set; }

        public enum MatchPhase
        {
            None,
            Lobby,
            PreDrop,
            Dropping,
            Playing,
            Ending,
            PostMatch
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ServiceLocator.Register<MatchManager>(this);
        }

        private void Start()
        {
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Subscribe<PlayerEliminatedEvent>(OnPlayerEliminated);
        }

        private void Update()
        {
            UpdateMatchPhase();
        }

        /// <summary>
        /// Start a new match.
        /// </summary>
        public void StartMatch(string mapName, int playerCount)
        {
            _totalPlayerCount = Mathf.Min(playerCount, _maxPlayers);
            _alivePlayerCount = _totalPlayerCount;
            _players.Clear();
            _eliminationOrder.Clear();
            _matchStartTime = Time.time;

            TransitionToPhase(MatchPhase.Lobby);

            EventBus.Raise(new MatchStartedEvent
            {
                PlayerCount = _totalPlayerCount,
                MapName = mapName
            });

            Debug.Log($"[MatchManager] Match started with {_totalPlayerCount} players on {mapName}");
        }

        /// <summary>
        /// Register a player in the match.
        /// </summary>
        public void RegisterPlayer(string playerId, string playerName)
        {
            if (!_players.ContainsKey(playerId))
            {
                _players[playerId] = new PlayerMatchData
                {
                    playerId = playerId,
                    playerName = playerName
                };
            }
        }

        /// <summary>
        /// Record a kill for scoring.
        /// </summary>
        public void RecordKill(string killerId, string victimId, string weaponId, bool headshot)
        {
            if (_players.ContainsKey(killerId))
            {
                _players[killerId].kills++;
                _players[killerId].score += _killPoints;
                if (headshot) _players[killerId].headshots++;
            }

            if (_players.ContainsKey(victimId))
            {
                _players[victimId].deaths++;
                _players[victimId].alive = false;
                _alivePlayerCount--;
                _eliminationOrder.Add(victimId);
            }

            EventBus.Raise(new PlayerEliminatedEvent
            {
                PlayerId = victimId,
                KillerId = killerId,
                WeaponId = weaponId,
                WasHeadshot = headshot
            });

            // Check win condition
            if (_alivePlayerCount <= 1)
            {
                EndMatch();
            }
        }

        /// <summary>
        /// End the current match.
        /// </summary>
        public void EndMatch()
        {
            if (_currentPhase == MatchPhase.Ending || _currentPhase == MatchPhase.PostMatch) return;

            TransitionToPhase(MatchPhase.Ending);

            // Calculate final scores
            string winnerId = "";
            foreach (var player in _players.Values)
            {
                if (player.alive)
                {
                    winnerId = player.playerId;
                    player.score += _winPoints;
                    player.placement = 1;
                }

                // Survival score
                float survivalMinutes = MatchElapsedTime / 60f;
                player.score += Mathf.FloorToInt(survivalMinutes * _survivalPointsPerMinute);

                // Placement score
                if (player.placement <= 5) player.score += _top5Points;
                else if (player.placement <= 10) player.score += _top10Points;
            }

            string winnerName = winnerId;
            if (_players.ContainsKey(winnerId))
                winnerName = _players[winnerId].playerName;

            EventBus.Raise(new MatchEndedEvent
            {
                WinnerId = winnerId,
                WinnerName = winnerName,
                Placement = 1,
                Kills = _players.ContainsKey(winnerId) ? _players[winnerId].kills : 0,
                DamageDealt = _players.ContainsKey(winnerId) ? _players[winnerId].damageDealt : 0,
                SurvivalTime = MatchElapsedTime
            });

            TransitionToPhase(MatchPhase.PostMatch);
            Debug.Log($"[MatchManager] Match ended! Winner: {winnerName}");
        }

        private void UpdateMatchPhase()
        {
            _phaseTimer -= Time.deltaTime;

            switch (_currentPhase)
            {
                case MatchPhase.Lobby:
                    if (_phaseTimer <= 0 && AlivePlayerCount >= _minPlayersToStart)
                    {
                        TransitionToPhase(MatchPhase.PreDrop);
                    }
                    break;

                case MatchPhase.PreDrop:
                    if (_phaseTimer <= 0)
                    {
                        TransitionToPhase(MatchPhase.Dropping);
                    }
                    break;

                case MatchPhase.Dropping:
                    if (_phaseTimer <= 0)
                    {
                        TransitionToPhase(MatchPhase.Playing);
                    }
                    break;

                case MatchPhase.Playing:
                    break;

                case MatchPhase.PostMatch:
                    if (_phaseTimer <= 0)
                    {
                        var sceneLoader = FindObjectOfType<SceneLoader>();
                        if (sceneLoader != null)
                        {
                            sceneLoader.LoadScene("03_MainMenu");
                        }
                    }
                    break;
            }
        }

        private void TransitionToPhase(MatchPhase newPhase)
        {
            MatchPhase previous = _currentPhase;
            _currentPhase = newPhase;

            switch (newPhase)
            {
                case MatchPhase.Lobby:
                    _phaseTimer = _lobbyWaitTime;
                    break;
                case MatchPhase.PreDrop:
                    _phaseTimer = 10f;
                    break;
                case MatchPhase.Dropping:
                    _phaseTimer = _dropPhaseTime;
                    SpawnPlayers();
                    break;
                case MatchPhase.Playing:
                    _phaseTimer = float.MaxValue;
                    break;
                case MatchPhase.PostMatch:
                    _phaseTimer = _postMatchTime;
                    break;
            }

            Debug.Log($"[MatchManager] Phase: {previous} -> {newPhase}");
        }

        private void SpawnPlayers()
        {
            foreach (var player in _players.Values)
            {
                if (player.alive)
                {
                }
            }
        }

        public int GetPlayerPlacement(string playerId)
        {
            if (_players.ContainsKey(playerId))
                return _players[playerId].placement;
            return _players.Count > 0 ? _players.Count : 1;
        }

        public int GetPlayerScore(string playerId)
        {
            if (_players.ContainsKey(playerId))
                return _players[playerId].score;
            return 0;
        }

        public int GetPlayerKills(string playerId)
        {
            if (_players.ContainsKey(playerId))
                return _players[playerId].kills;
            return 0;
        }

        public int GetPlayerDamage(string playerId)
        {
            if (_players.ContainsKey(playerId))
                return _players[playerId].damageDealt;
            return 0;
        }

        public List<PlayerMatchData> GetRankings()
        {
            var rankings = new List<PlayerMatchData>(_players.Values);
            rankings.Sort((a, b) => a.placement.CompareTo(b.placement));
            return rankings;
        }

        private void OnPlayerDied(PlayerDiedEvent evt)
        {
            RecordKill(evt.KillerId, evt.PlayerId, "unknown", false);
        }

        private void OnPlayerEliminated(PlayerEliminatedEvent evt)
        {
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<PlayerEliminatedEvent>(OnPlayerEliminated);
        }
    }

    [System.Serializable]
    public class PlayerMatchData
    {
        public string playerId;
        public string playerName;
        public int kills;
        public int deaths;
        public int assists;
        public int damageDealt;
        public int damageTaken;
        public int headshots;
        public int score;
        public int placement = 99;
        public bool alive = true;
        public float survivalTime;
    }
}
