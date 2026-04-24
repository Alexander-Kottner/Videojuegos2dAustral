using Enemies;
using UnityEngine;

namespace Enemies.Data
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Game/Enemies/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private EnemyController prefab;
        [SerializeField] private EnemyMovementBehaviour movementBehaviour;
        [SerializeField, Min(1)] private int spawnCost = 1;
        [SerializeField, Min(1)] private int unlockWave = 1;
        [SerializeField, Min(0.01f)] private float spawnWeight = 1f;
        [SerializeField] private EnemySpawnRole spawnRole = EnemySpawnRole.Common;
        [SerializeField] private int maxAlive = 0;

        public EnemyController Prefab => prefab;
        public EnemyMovementBehaviour MovementBehaviour => movementBehaviour;
        public int SpawnCost => Mathf.Max(1, spawnCost);
        public int UnlockWave => Mathf.Max(1, unlockWave);
        public float SpawnWeight => Mathf.Max(0f, spawnWeight);
        public EnemySpawnRole SpawnRole => spawnRole;
        public int MaxAlive => maxAlive;

        public bool IsUnlockedAtWave(int wave)
        {
            return wave >= UnlockWave;
        }

        public bool HasAliveCap => maxAlive > 0;
    }
}
