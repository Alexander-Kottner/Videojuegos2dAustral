using Enemies;
using UnityEngine;

namespace Enemies.Data
{
    public abstract class EnemyAttackBehaviour : ScriptableObject
    {
        public abstract void Tick(EnemyRuntimeContext context);
    }
}
