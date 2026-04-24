using Enemies;
using Enemies.Data;
using UnityEngine;

namespace Spawning
{
    [DisallowMultipleComponent]
    public class SpawnedEnemyTracker : MonoBehaviour
    {
        private SpawnerController _owner;
        private EnemyDefinition _definition;
        private EnemyController _controller;
        private bool _isBound;

        public void Bind(SpawnerController owner, EnemyDefinition definition, EnemyController controller)
        {
            _owner = owner;
            _definition = definition;
            _controller = controller;
            _isBound = true;
        }

        private void OnDisable()
        {
            NotifyReleased(returnToPool: true);
        }

        private void OnDestroy()
        {
            NotifyReleased(returnToPool: false);
        }

        private void NotifyReleased(bool returnToPool)
        {
            if (!_isBound)
            {
                return;
            }

            SpawnerController owner = _owner;
            EnemyDefinition definition = _definition;
            EnemyController controller = _controller;

            _owner = null;
            _definition = null;
            _controller = null;
            _isBound = false;

            owner?.NotifyEnemyReleased(definition, controller, returnToPool);
        }
    }
}
