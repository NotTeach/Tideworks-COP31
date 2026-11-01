using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Yan yana seceneklerden biri secili olan kutu (Auto / Day / Night gibi).
/// Secili "hap" secenegin arkasina kayar, secili yazi rengi degisir.
/// </summary>
public class SegmentedControl : MonoBehaviour
{
    public Button[] segments;
    public Text[] labels;
    public RectTransform pill;
    public Color selectedColor = new Color32(228, 186, 92, 255);
    public Color normalColor = new Color32(60, 36, 22, 255);
    public float speed = 18f;

    public event Action<int> ValueChanged;
    public int Value { get; private set; }

    void Awake()
    {
        for (int i = 0; i < segments.Length; i++)
        {
            int index = i;
            segments[i].onClick.AddListener(() => SetValue(index));
        }
    }

    void OnEnable() => Snap();

    public void SetValueWithoutNotify(int value)
    {
        Value = Mathf.Clamp(value, 0, segments.Length - 1);
        Snap();
    }

    void SetValue(int value)
    {
        if (value == Value) return;
        Value = value;
        UpdateLabels();
        ValueChanged?.Invoke(value);
    }

    void Update()
    {
        if (!pill) return;
        var target = Target();
        if (GameSettings.ReduceMotion) pill.anchoredPosition = target;
        else pill.anchoredPosition = Vector2.Lerp(pill.anchoredPosition, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
    }

    void Snap()
    {
        if (pill) pill.anchoredPosition = Target();
        UpdateLabels();
    }

    Vector2 Target() => ((RectTransform)segments[Value].transform).anchoredPosition;

    void UpdateLabels()
    {
        for (int i = 0; i < labels.Length; i++)
            if (labels[i]) labels[i].color = i == Value ? selectedColor : normalColor;
    }
}
