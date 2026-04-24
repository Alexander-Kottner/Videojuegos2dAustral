using UnityEngine;

namespace Input
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private InputHandler inputHandler;

        private void Update()
        {
            Move();
        }

        private void Move()
        {
            Vector2 input = inputHandler.MoveInput.normalized;

            Vector3 movement = new Vector3(input.x, input.y, 0f);

            transform.position += movement * moveSpeed * Time.deltaTime;
        }
    }
}