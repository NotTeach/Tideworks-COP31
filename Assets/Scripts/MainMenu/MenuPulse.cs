using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bir gorselin saydamligini ve boyutunu yavasca dalgalandirir (ay isiltisi, yildiz parlamasi).
/// </summary>
[RequireComponent(typeof(Graphic))]
public class MenuPulse : MonoBehaviour
{
    public float minAlpha = 0.2f;
    public float maxAlpha = 1f;
    [Tooltip("Saniyedeki dongu sayisi")] public float speed = 0.5f;
    [Range(0f, 1f)] public float phase;
    public float scaleAmount = 0.1f;

    Graphic graphic;
    Vector3 baseScale;

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        float t = 0.5f + 0.5f * Mathf.Sin((Time.unscaledTime * speed + phase) * Mathf.PI * 2f);
        var c = graphic.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        graphic.color = c;
        transform.localScale = baseScale * (1f + scaleAmount * t);
    }
}
