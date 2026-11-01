using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Buton uzerine gelince (veya klavye/gamepad ile secilince) hafifce buyutur.
/// </summary>
public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public float hoverScale = 1.05f;
    public float speed = 14f;

    Selectable selectable;
    Vector3 baseScale;
    bool hovered, selected;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
        baseScale = transform.localScale;
    }

    void OnDisable()
    {
        hovered = selected = false;
        transform.localScale = baseScale;
    }

    void Update()
    {
        bool active = (hovered || selected) && (selectable == null || selectable.IsInteractable());
        var target = baseScale * (active ? hoverScale : 1f);
        transform.localScale = GameSettings.ReduceMotion ? target
            : Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) => hovered = false;
    public void OnSelect(BaseEventData e) => selected = true;
    public void OnDeselect(BaseEventData e) => selected = false;
}
