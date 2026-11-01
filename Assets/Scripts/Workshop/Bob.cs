using UnityEngine;

/// <summary>
/// Yavas sinus hareketi: dalgalar, tekne, bulutlar.
/// Reduce motion acikken yerinde durur.
/// </summary>
public class Bob : MonoBehaviour
{
    [Tooltip("Ileri-geri kayma (artboard pikseli)")] public Vector2 move = new Vector2(10f, 3f);
    [Tooltip("Sallanma acisi (derece)")] public float tilt;
    [Tooltip("Saniyedeki dongu sayisi")] public float speed = 0.25f;
    [Range(0f, 1f)] public float phase;

    RectTransform rt;
    Vector2 basePos;
    Quaternion baseRot;

    void Awake()
    {
        rt = (RectTransform)transform;
        basePos = rt.anchoredPosition;
        baseRot = rt.localRotation;
    }

    void OnDisable()
    {
        if (!rt) return;
        rt.anchoredPosition = basePos;
        rt.localRotation = baseRot;
    }

    void Update()
    {
        if (GameSettings.ReduceMotion)
        {
            rt.anchoredPosition = basePos;
            rt.localRotation = baseRot;
            return;
        }
        float a = (Time.unscaledTime * speed + phase) * Mathf.PI * 2f;
        // Dikey hareket yatayin iki kati hizda: dalga gibi kucuk bir daire cizer
        rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(a) * move.x, Mathf.Sin(a * 2f) * move.y * 0.5f + Mathf.Cos(a) * move.y * 0.5f);
        rt.localRotation = baseRot * Quaternion.Euler(0, 0, Mathf.Sin(a + 0.6f) * tilt);
    }
}
