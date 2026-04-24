using UnityEngine;

namespace Enemies.Data
{
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Game/Enemies/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private EnemyMovementBehaviour movementBehaviour;

        public EnemyMovementBehaviour MovementBehaviour => movementBehaviour;
    }
}
