using Enemies;
using UnityEngine;

namespace Enemies.Data
{
    public abstract class EnemyMovementBehaviour : ScriptableObject
    {
        public abstract void Tick(EnemyRuntimeContext context);
    }
}
