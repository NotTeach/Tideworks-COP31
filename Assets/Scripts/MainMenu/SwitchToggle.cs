using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggle'i acik/kapali anahtar gibi gosterir: ray rengi degisir, beyaz top saga/sola kayar.
/// </summary>
[RequireComponent(typeof(Toggle))]
public class SwitchToggle : MonoBehaviour
{
    public Image track;
    public Sprite onSprite;
    public Sprite offSprite;
    public RectTransform knob;
    public float offX = 41f;
    public float onX = 99f;
    public float speed = 16f;

    Toggle toggle;

    void Awake() => toggle = GetComponent<Toggle>();

    void OnEnable() => Refresh(true);

    void Update() => Refresh(GameSettings.ReduceMotion);

    void Refresh(bool snap)
    {
        if (!toggle) toggle = GetComponent<Toggle>();
        bool on = toggle.isOn;
        if (track) track.sprite = on ? onSprite : offSprite;
        if (!knob) return;

        var p = knob.anchoredPosition;
        float target = on ? onX : offX;
        p.x = snap ? target : Mathf.Lerp(p.x, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        knob.anchoredPosition = p;
    }
}
