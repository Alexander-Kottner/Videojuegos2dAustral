using System.Collections.Generic;
using Enemies;
using Enemies.Data;
using UnityEngine;

namespace Spawning
{
    [DisallowMultipleComponent]
    public class SpawnerController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private WaveDirectorConfig config;
        [SerializeField] private List<SpawnZone> spawnZones = new();

        [Header("Targeting")]
        [SerializeField] private Transform targetOverride;
        [SerializeField] private bool autoAcquireTarget = true;
        [SerializeField] private string targetTag = "Player";
        [SerializeField, Min(0.1f)] private float retargetInterval = 0.5f;

        [Header("Fallback Spawning")]
        [SerializeField, Min(0.5f)] private float minSpawnDistance = 8f;
        [SerializeField, Min(1f)] private float maxSpawnDistance = 14f;
        [SerializeField, Min(1)] private int spawnPositionAttempts = 10;
        [SerializeField] private bool startOnAwake = true;

        private readonly Dictionary<EnemyDefinition, int> _aliveCounts = new();
        private readonly Dictionary<EnemyDefinition, Queue<EnemyController>> _pooledEnemies = new();
        private readonly List<EnemyDefinition> _candidateBuffer = new();

        private int _aliveEnemyCount;
        private int _currentWave;
        private int _remainingBudget;
        private int _currentConcurrentCap;
        private float _nextRetargetTime;
        private float _stateEndTime;
        private float _nextSpawnTime;
        private float _currentWaveDuration;
        private bool _isRunning;
        private bool _isIntermission;

        public int CurrentWave => _currentWave;
        public bool IsIntermission => _isIntermission;
        public Transform Target => targetOverride;

        private void Start()
        {
            if (!startOnAwake || config == null)
            {
                return;
            }

            BeginRun();
        }

        private void Update()
        {
            if (!_isRunning || config == null)
            {
                return;
            }

            RefreshTarget();

            if (_isIntermission)
            {
                if (Time.time >= _stateEndTime)
                {
                    StartNextWave();
                }

                return;
            }

            if (Time.time >= _stateEndTime)
            {
                BeginIntermission();
                return;
            }

            if (_aliveEnemyCount >= _currentConcurrentCap)
            {
                return;
            }

            while (Time.time >= _nextSpawnTime)
            {
                float interval = config.GetSpawnInterval(_currentWave);
                _nextSpawnTime += interval;

                if (_aliveEnemyCount >= _currentConcurrentCap || !TrySpawnEnemy())
                {
                    break;
                }
            }
        }

        public void BeginRun()
        {
            if (config == null)
            {
                return;
            }

            _aliveCounts.Clear();
            _candidateBuffer.Clear();

            _aliveEnemyCount = 0;
            _currentWave = 0;
            _remainingBudget = 0;
            _isRunning = true;
            _isIntermission = true;
            _stateEndTime = Time.time + config.InitialDelay;
            _nextSpawnTime = Time.time;
        }

        public void StopRun()
        {
            _isRunning = false;
        }

        public void NotifyEnemyReleased(EnemyDefinition definition, EnemyController controller, bool returnToPool)
        {
            if (definition != null && _aliveCounts.TryGetValue(definition, out int aliveCount))
            {
                _aliveCounts[definition] = Mathf.Max(0, aliveCount - 1);
            }

            _aliveEnemyCount = Mathf.Max(0, _aliveEnemyCount - 1);

            if (!returnToPool || definition == null || controller == null)
            {
                return;
            }

            Queue<EnemyController> pool = GetPool(definition);
            pool.Enqueue(controller);
        }

        private void BeginIntermission()
        {
            _isIntermission = true;
            _stateEndTime = Time.time + config.IntermissionDuration;
        }

        private void StartNextWave()
        {
            _isIntermission = false;
            _currentWave++;
            _remainingBudget = config.GetBudgetForWave(_currentWave);
            _currentConcurrentCap = config.GetMaxConcurrent(_currentWave);
            _currentWaveDuration = config.GetWaveDuration(_currentWave);
            _stateEndTime = Time.time + _currentWaveDuration;
            _nextSpawnTime = Time.time;
        }

        private bool TrySpawnEnemy()
        {
            EnemyDefinition definition = PickEnemyDefinition();
            if (definition == null)
            {
                return false;
            }

            if (!TryGetSpawnPosition(out Vector3 spawnPosition))
            {
                return false;
            }

            EnemyController enemy = GetOrCreateEnemy(definition);
            if (enemy == null)
            {
                return false;
            }

            enemy.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
            enemy.SetTarget(targetOverride);

            SpawnedEnemyTracker tracker = enemy.GetComponent<SpawnedEnemyTracker>();
            if (tracker == null)
            {
                tracker = enemy.gameObject.AddComponent<SpawnedEnemyTracker>();
            }

            tracker.Bind(this, definition, enemy);

            if (!enemy.gameObject.activeSelf)
            {
                enemy.gameObject.SetActive(true);
            }

            _remainingBudget = Mathf.Max(0, _remainingBudget - definition.SpawnCost);
            _aliveEnemyCount++;
            _aliveCounts[definition] = GetAliveCount(definition) + 1;
            return true;
        }

        private EnemyDefinition PickEnemyDefinition()
        {
            _candidateBuffer.Clear();

            IReadOnlyList<EnemyDefinition> roster = config.EnemyRoster;
            float totalWeight = 0f;

            for (int i = 0; i < roster.Count; i++)
            {
                EnemyDefinition definition = roster[i];
                if (!IsEligible(definition))
                {
                    continue;
                }

                float weightedValue = definition.SpawnWeight * config.GetRoleWeightMultiplier(definition.SpawnRole, _currentWave);
                if (weightedValue <= 0f)
                {
                    continue;
                }

                _candidateBuffer.Add(definition);
                totalWeight += weightedValue;
            }

            if (_candidateBuffer.Count == 0 || totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.value * totalWeight;

            for (int i = 0; i < _candidateBuffer.Count; i++)
            {
                EnemyDefinition definition = _candidateBuffer[i];
                roll -= definition.SpawnWeight * config.GetRoleWeightMultiplier(definition.SpawnRole, _currentWave);

                if (roll <= 0f)
                {
                    return definition;
                }
            }

            return _candidateBuffer[_candidateBuffer.Count - 1];
        }

        private bool IsEligible(EnemyDefinition definition)
        {
            if (definition == null || definition.Prefab == null)
            {
                return false;
            }

            if (!definition.IsUnlockedAtWave(_currentWave))
            {
                return false;
            }

            if (!config.IsRoleAllowed(definition.SpawnRole, _currentWave))
            {
                return false;
            }

            if (definition.SpawnCost > _remainingBudget)
            {
                return false;
            }

            if (definition.HasAliveCap && GetAliveCount(definition) >= definition.MaxAlive)
            {
                return false;
            }

            return true;
        }

        private EnemyController GetOrCreateEnemy(EnemyDefinition definition)
        {
            Queue<EnemyController> pool = GetPool(definition);

            while (pool.Count > 0)
            {
                EnemyController pooledEnemy = pool.Dequeue();
                if (pooledEnemy != null)
                {
                    return pooledEnemy;
                }
            }

            EnemyController instance = Instantiate(definition.Prefab);
            instance.gameObject.SetActive(false);
            return instance;
        }

        private Queue<EnemyController> GetPool(EnemyDefinition definition)
        {
            if (!_pooledEnemies.TryGetValue(definition, out Queue<EnemyController> pool))
            {
                pool = new Queue<EnemyController>();
                _pooledEnemies[definition] = pool;
            }

            return pool;
        }

        private int GetAliveCount(EnemyDefinition definition)
        {
            if (definition == null)
            {
                return 0;
            }

            return _aliveCounts.TryGetValue(definition, out int aliveCount) ? aliveCount : 0;
        }

        private bool TryGetSpawnPosition(out Vector3 spawnPosition)
        {
            if (spawnZones.Count > 0)
            {
                SpawnZone zone = PickSpawnZone();
                if (zone != null && zone.TryGetSpawnPosition(targetOverride, minSpawnDistance, maxSpawnDistance, spawnPositionAttempts, out spawnPosition))
                {
                    return true;
                }
            }

            return TryGetFallbackSpawnPosition(out spawnPosition);
        }

        private SpawnZone PickSpawnZone()
        {
            float totalWeight = 0f;

            for (int i = 0; i < spawnZones.Count; i++)
            {
                if (spawnZones[i] != null)
                {
                    totalWeight += spawnZones[i].Weight;
                }
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.value * totalWeight;

            for (int i = 0; i < spawnZones.Count; i++)
            {
                SpawnZone zone = spawnZones[i];
                if (zone == null)
                {
                    continue;
                }

                roll -= zone.Weight;
                if (roll <= 0f)
                {
                    return zone;
                }
            }

            return null;
        }

        private bool TryGetFallbackSpawnPosition(out Vector3 spawnPosition)
        {
            Vector3 center = targetOverride != null ? targetOverride.position : transform.position;
            int attempts = Mathf.Max(1, spawnPositionAttempts);

            for (int i = 0; i < attempts; i++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                if (direction == Vector2.zero)
                {
                    direction = Vector2.up;
                }

                float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
                spawnPosition = center + new Vector3(direction.x, direction.y, 0f) * distance;
                return true;
            }

            spawnPosition = center;
            return false;
        }

        private void RefreshTarget()
        {
            if (!autoAcquireTarget || targetOverride != null || Time.time < _nextRetargetTime || string.IsNullOrWhiteSpace(targetTag))
            {
                return;
            }

            _nextRetargetTime = Time.time + retargetInterval;

            try
            {
                GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);
                if (targetObject != null)
                {
                    targetOverride = targetObject.transform;
                }
            }
            catch (UnityException)
            {
            }
        }
    }
}
