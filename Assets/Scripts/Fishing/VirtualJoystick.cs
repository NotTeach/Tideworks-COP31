using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Telefon icin ekran joystick'i: Value -1..1 araliginda yon verir (yukari = +y).</summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] RectTransform knob;
    [Tooltip("Knob'un merkezden en fazla uzakligi (yerel piksel)")]
    [SerializeField] float radius = 110f;

    public Vector2 Value { get; private set; }

    public void OnPointerDown(PointerEventData e) => OnDrag(e);

    public void OnDrag(PointerEventData e)
    {
        var rt = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local)) return;
        local -= rt.rect.center;
        var clamped = Vector2.ClampMagnitude(local, radius);
        knob.anchoredPosition = clamped;
        Value = clamped / radius;
    }

    public void OnPointerUp(PointerEventData e)
    {
        knob.anchoredPosition = Vector2.zero;
        Value = Vector2.zero;
    }

    void OnDisable()
    {
        if (knob) knob.anchoredPosition = Vector2.zero;
        Value = Vector2.zero;
    }
}
