using UnityEngine;

/// <summary>
/// Sabit boyutlu bir cizimi (giris ekrani gorseli) ekrana yerlestirir.
/// Cocuklar gorselin piksel koordinatlarinda durur, bu yuzden her cozunurlukte gorselle hizali kalir.
/// Contain: gorselin tamami her oranda gorunur (bos kalan kenarlari Backdrop doldurur).
/// Cover: ekrani tamamen kaplar, tasan kisim kirpilir (sol kenar sabit, defter hep gorunur).
/// </summary>
[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public class ArtboardFitter : MonoBehaviour
{
    public enum FitMode { Contain, Cover }

    public Vector2 artSize = new Vector2(2752, 1536);
    public FitMode mode = FitMode.Contain;

    void OnEnable() => Fit();

    void Update() => Fit();

    void Fit()
    {
        var rt = (RectTransform)transform;
        var parent = rt.parent as RectTransform;
        if (!parent) return;
        var size = parent.rect.size;
        if (size.x <= 0 || size.y <= 0) return;

        float sx = size.x / artSize.x, sy = size.y / artSize.y;
        float s = mode == FitMode.Contain ? Mathf.Min(sx, sy) : Mathf.Max(sx, sy);
        var pin = mode == FitMode.Contain ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
        if (rt.anchorMin != pin) rt.anchorMin = pin;
        if (rt.anchorMax != pin) rt.anchorMax = pin;
        if (rt.pivot != pin) rt.pivot = pin;
        if (rt.sizeDelta != artSize) rt.sizeDelta = artSize;
        if (rt.anchoredPosition != Vector2.zero) rt.anchoredPosition = Vector2.zero;
        var scale = new Vector3(s, s, 1f);
        if (rt.localScale != scale) rt.localScale = scale;
    }
}
