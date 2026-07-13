using UnityEngine;
using UnityEngine.AI;
using ArenaFall.Core;
using ArenaFall.Gameplay.Characters;
using ArenaFall.Gameplay.Weapons;
using ArenaFall.Gameplay.Inventory;
using ArenaFall.Interfaces;
using ArenaFall.Utilities;

namespace ArenaFall.Gameplay.AI
{
    /// <summary>
    /// AI controller for bot players.
    /// Uses NavMeshAgent for movement and state machine for behavior.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(CharacterHealth))]
    public class AIController : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private float _detectionRange = 50f;
        [SerializeField] private float _attackRange = 30f;
        [SerializeField] private float _accuracy = 0.7f;
        [SerializeField] private float _reactionTime = 0.5f;
        [SerializeField] private AIBehavior _behavior = AIBehavior.Aggressive;

        [Header("Loot")]
        [SerializeField] private float _lootDetectionRange = 20f;
        [SerializeField] private LayerMask _lootLayer;

        // Components
        private NavMeshAgent _agent;
        private CharacterHealth _health;
        private WeaponController _currentWeapon;
        private Animator _animator;

        // State machine
        private StateMachine<AIState> _stateMachine;
        private Transform _target;
        private Vector3 _targetPosition;
        private float _scanTimer;

        // Debug
        [Header("Debug")]
        [SerializeField] private bool _showGizmos = true;
        [SerializeField] private Color _gizmoColor = Color.red;

        public enum AIState
        {
            Idle,
            Patrolling,
            Investigating,
            Combat,
            Looting,
            Fleeing,
            Dead
        }

        public enum AIBehavior
        {
            Passive,
            Defensive,
            Aggressive,
            Reckless
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<CharacterHealth>();
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            InitializeStateMachine();
            _health.OnDied += OnDied;

            // Configure NavMeshAgent
            _agent.speed = 6f;
            _agent.angularSpeed = 360f;
            _agent.stoppingDistance = 2f;
        }

        private void InitializeStateMachine()
        {
            _stateMachine = new StateMachine<AIState>();
            _stateMachine.RegisterState(AIState.Idle, new AIIdleState(this));
            _stateMachine.RegisterState(AIState.Patrolling, new AIPatrolState(this));
            _stateMachine.RegisterState(AIState.Combat, new AICombatState(this));
            _stateMachine.RegisterState(AIState.Looting, new AILootState(this));
            _stateMachine.RegisterState(AIState.Fleeing, new AIFleeState(this));
            _stateMachine.RegisterState(AIState.Dead, new AIDeadState(this));

            _stateMachine.TransitionTo(AIState.Patrolling);
        }

        private void Update()
        {
            if (!_health.IsAlive) return;

            _stateMachine.Update();
            UpdateScanning();
            UpdateAnimations();
        }

        private void UpdateScanning()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0)
            {
                _scanTimer = 1f;
                ScanForTargets();
                ScanForLoot();
            }
        }

        private void ScanForTargets()
        {
            // Find nearest enemy
            Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRange);
            float closestDist = float.MaxValue;
            Transform closestTarget = null;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                var health = hit.GetComponent<CharacterHealth>();
                if (health != null && health.IsAlive && health.TeamId != _health.TeamId)
                {
                    float dist = Vector3.Distance(transform.position, hit.transform.position);
                    if (dist < closestDist)
                    {
                        // Line of sight check
                        if (HasLineOfSight(hit.transform.position))
                        {
                            closestDist = dist;
                            closestTarget = hit.transform;
                        }
                    }
                }
            }

            if (closestTarget != null)
            {
                _target = closestTarget;
                _stateMachine.TransitionTo(AIState.Combat);
            }
        }

        private void ScanForLoot()
        {
            if (_currentWeapon != null) return; // Already have weapon

            Collider[] hits = Physics.OverlapSphere(transform.position, _lootDetectionRange, _lootLayer);
            foreach (var hit in hits)
            {
                var loot = hit.GetComponent<Inventory.LootItem>();
                if (loot != null && loot.CanPickup)
                {
                    _targetPosition = hit.transform.position;
                    _stateMachine.TransitionTo(AIState.Looting);
                    return;
                }
            }
        }

        private bool HasLineOfSight(Vector3 targetPos)
        {
            Vector3 direction = targetPos - transform.position;
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 1.5f, direction.normalized, out hit, _detectionRange))
            {
                return hit.collider.GetComponent<CharacterHealth>() != null;
            }
            return false;
        }

        private void UpdateAnimations()
        {
            if (_animator != null)
            {
                _animator.SetFloat("Speed", _agent.velocity.magnitude);
                _animator.SetBool("IsGrounded", _agent.isOnOffMeshLink == false);
            }
        }

        /// <summary>
        /// Get the AI's current target.
        /// </summary>
        public Transform CurrentTarget => _target;

        /// <summary>
        /// Set the AI's current weapon.
        /// </summary>
        public void SetWeapon(WeaponController weapon)
        {
            _currentWeapon = weapon;
        }

        /// <summary>
        /// Get a random patrol destination within range.
        /// </summary>
        public Vector3 GetRandomPatrolPoint(float range = 30f)
        {
            Vector3 randomDir = Random.insideUnitSphere * range;
            randomDir += transform.position;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, range, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return transform.position;
        }

        private void OnDied(GameObject killer)
        {
            _stateMachine.TransitionTo(AIState.Dead);
            _agent.isStopped = true;
            _agent.enabled = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _lootDetectionRange);

            if (_target != null)
            {
                Gizmos.color = _gizmoColor;
                Gizmos.DrawLine(transform.position, _target.position);
            }
        }

        // ── State Implementations ──

        private class AIIdleState : IState
        {
            private AIController _ai;
            private float _idleTimer;

            public AIIdleState(AIController ai) { _ai = ai; }

            public void Enter()
            {
                _idleTimer = Random.Range(2f, 5f);
                _ai._agent.isStopped = true;
            }

            public void Update()
            {
                _idleTimer -= Time.deltaTime;
                if (_idleTimer <= 0)
                {
                    _ai._stateMachine.TransitionTo(AIState.Patrolling);
                }
            }

            public void FixedUpdate() { }
            public void Exit() { _ai._agent.isStopped = false; }
        }

        private class AIPatrolState : IState
        {
            private AIController _ai;
            private float _patrolTimer;

            public AIPatrolState(AIController ai) { _ai = ai; }

            public void Enter()
            {
                _patrolTimer = Random.Range(5f, 15f);
                _ai._agent.SetDestination(_ai.GetRandomPatrolPoint(40f));
                _ai._agent.speed = 4f; // Walking speed
            }

            public void Update()
            {
                _patrolTimer -= Time.deltaTime;
                if (_patrolTimer <= 0 || _ai._agent.remainingDistance < 1f)
                {
                    _ai._stateMachine.TransitionTo(AIState.Idle);
                }
            }

            public void FixedUpdate() { }
            public void Exit() { }
        }

        private class AICombatState : IState
        {
            private AIController _ai;
            private float _shootTimer;

            public AICombatState(AIController ai) { _ai = ai; }

            public void Enter()
            {
                _ai._agent.speed = 8f; // Combat speed
                _shootTimer = 0f;
            }

            public void Update()
            {
                if (_ai._target == null)
                {
                    _ai._stateMachine.TransitionTo(AIState.Patrolling);
                    return;
                }

                float dist = Vector3.Distance(_ai.transform.position, _ai._target.position);

                // Move to effective range
                if (dist > _ai._attackRange * 0.8f)
                {
                    _ai._agent.SetDestination(_ai._target.position);
                    _ai._agent.isStopped = false;
                }
                else if (dist < _ai._attackRange * 0.3f)
                {
                    // Too close, back up
                    Vector3 retreatDir = (_ai.transform.position - _ai._target.position).normalized;
                    _ai._agent.SetDestination(_ai.transform.position + retreatDir * 5f);
                }
                else
                {
                    _ai._agent.isStopped = true;
                    _ai.transform.LookAt(_ai._target.position);

                    // Shoot
                    _shootTimer -= Time.deltaTime;
                    if (_shootTimer <= 0 && _ai._currentWeapon != null)
                    {
                        float aimChance = _ai._accuracy * (1f - (dist / _ai._detectionRange));
                        if (Random.value < aimChance)
                        {
                            _ai._currentWeapon.StartFire();
                        }
                        _shootTimer = Random.Range(0.2f, 0.8f);
                    }
                }

                // Lost target
                float timeSinceSeen = 0;
                if (!_ai.HasLineOfSight(_ai._target.position))
                {
                    timeSinceSeen += Time.deltaTime;
                    if (timeSinceSeen > 5f)
                    {
                        _ai._target = null;
                        _ai._stateMachine.TransitionTo(AIState.Patrolling);
                    }
                }
            }

            public void FixedUpdate() { }
            public void Exit() { _ai._agent.isStopped = false; }
        }

        private class AILootState : IState
        {
            private AIController _ai;

            public AILootState(AIController ai) { _ai = ai; }

            public void Enter()
            {
                _ai._agent.SetDestination(_ai._targetPosition);
                _ai._agent.speed = 6f;
            }

            public void Update()
            {
                if (_ai._agent.remainingDistance < 2f)
                {
                    _ai._stateMachine.TransitionTo(AIState.Patrolling);
                }
            }

            public void FixedUpdate() { }
            public void Exit() { }
        }

        private class AIFleeState : IState
        {
            private AIController _ai;
            private float _fleeTimer;

            public AIFleeState(AIController ai) { _ai = ai; }

            public void Enter()
            {
                _fleeTimer = Random.Range(3f, 6f);
                Vector3 fleeDir = (_ai.transform.position - _ai._target.position).normalized;
                _ai._agent.SetDestination(_ai.transform.position + fleeDir * 30f);
                _ai._agent.speed = 9f; // Sprint
            }

            public void Update()
            {
                _fleeTimer -= Time.deltaTime;
                if (_fleeTimer <= 0)
                {
                    _ai._stateMachine.TransitionTo(AIState.Patrolling);
                }
            }

            public void FixedUpdate() { }
            public void Exit() { }
        }

        private class AIDeadState : IState
        {
            private AIController _ai;

            public AIDeadState(AIController ai) { _ai = ai; }

            public void Enter() { }
            public void Update() { }
            public void FixedUpdate() { }
            public void Exit() { }
        }
    }
}
