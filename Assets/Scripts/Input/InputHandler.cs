using UnityEngine;

namespace Input
{
    public class InputHandler : MonoBehaviour
    {
        private PlayerInputActions _inputActions;

        [SerializeField] private VirtualJoystick joystick;

        public Vector2 MoveInput { get; private set; }

        private void Awake()
        {
            _inputActions = new PlayerInputActions();
        }

        private void OnEnable()
        {
            _inputActions.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Disable();
        }

        private void Update()
        {
            Vector2 keyboardInput = _inputActions.Gameplay.Movement.ReadValue<Vector2>().normalized;

            if (joystick != null && joystick.InputVector != Vector2.zero)
            {
                MoveInput = joystick.InputVector;
            }
            else
            {
                MoveInput = keyboardInput;
            }
        }
    }
}