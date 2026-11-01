using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Surukle-birak ya da dokun-sec karti (3. gun finalinde gorevleri yardimcilara vermek icin).
/// Birakilinca Dropped(kart, ekran noktasi) cagrilir; kabul edilmezse kart yerine geri doner. Dokununca Clicked cagrilir.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DragChip : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public int index;
    public Action<DragChip> Clicked;
    public Func<DragChip, Vector2, bool> Dropped;

    RectTransform rt;
    CanvasGroup group;
    Vector2 home, grabOffset;
    bool dragging;

    public bool Locked { get; set; }

    void Awake()
    {
        rt = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        home = rt.anchoredPosition;
    }

    public void ReturnHome() => rt.anchoredPosition = home;

    public void OnBeginDrag(PointerEventData e)
    {
        if (Locked) return;
        dragging = true;
        group.blocksRaycasts = false;          // altindaki yardimci kartini gorebilsin
        transform.SetAsLastSibling();
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out var p);
        grabOffset = rt.anchoredPosition - p;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!dragging) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent, e.position, e.pressEventCamera, out var p);
        rt.anchoredPosition = p + grabOffset;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        group.blocksRaycasts = true;
        if (Dropped == null || !Dropped(this, e.position)) ReturnHome();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (Locked || e.dragging) return;
        Clicked?.Invoke(this);
    }
}
