using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Yazisi gorsele gomulu sprite'lar: Turkce'de yazisi silinmis kopya gosterilir,
/// ustundeki LocalizedText (turkishOnly) etiketleri Turkce yaziyi koyar.
/// </summary>
[RequireComponent(typeof(Image))]
public class LocalizedImage : MonoBehaviour
{
    [SerializeField] Sprite english;
    [SerializeField] Sprite turkish;

    Image image;

    public void Setup(Sprite en, Sprite tr)
    {
        english = en;
        turkish = tr;
    }

    void Awake() => image = GetComponent<Image>();

    void OnEnable()
    {
        Loc.LanguageChanged += Apply;
        Apply();
    }

    void OnDisable() => Loc.LanguageChanged -= Apply;

    void Apply()
    {
        var want = Loc.Turkish && turkish ? turkish : english;
        if (want && image.sprite != want) image.sprite = want;
    }
}
