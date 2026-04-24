using UnityEngine;

namespace Spawning
{
    public class SpawnZone : MonoBehaviour
    {
        [SerializeField, Min(0.25f)] private float radius = 3f;
        [SerializeField, Min(0f)] private float weight = 1f;

        public float Weight => Mathf.Max(0f, weight);

        public bool TryGetSpawnPosition(Transform target, float minDistance, float maxDistance, int attempts, out Vector3 position)
        {
            Vector3 center = transform.position;
            int maxAttempts = Mathf.Max(1, attempts);

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                Vector3 candidate = center + new Vector3(offset.x, offset.y, 0f);

                if (IsValidForTarget(candidate, target, minDistance, maxDistance))
                {
                    position = candidate;
                    return true;
                }
            }

            position = center;
            return false;
        }

        private static bool IsValidForTarget(Vector3 candidate, Transform target, float minDistance, float maxDistance)
        {
            if (target == null)
            {
                return true;
            }

            float distance = Vector2.Distance(candidate, target.position);
            return distance >= minDistance && distance <= maxDistance;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.9f, 0.35f, 0.15f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
