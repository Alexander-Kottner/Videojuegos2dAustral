using UnityEngine;
using UnityEngine.EventSystems;

namespace Input
{
    public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;

        public Vector2 InputVector { get; private set; }

        private float _radius;

        private void Start()
        {
            _radius = background.sizeDelta.x / 2f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 position;
        
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                eventData.pressEventCamera,
                out position
            );

            position = Vector2.ClampMagnitude(position, _radius);

            handle.anchoredPosition = position;

            InputVector = position / _radius;
            
            InputVector = InputVector.normalized;
            if (InputVector.magnitude < 0.1f) InputVector = Vector2.zero;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            InputVector = Vector2.zero;
            handle.anchoredPosition = Vector2.zero;
        }
    }
}