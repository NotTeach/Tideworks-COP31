using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sabit bir yaziyi secili dile cevirir. key bos ise sahnedeki Ingilizce metin anahtar olur.
/// turkishOnly: gorsele gomulu Ingilizce yazinin ustune konan etiket; sadece Turkce'de gorunur.
/// </summary>
[RequireComponent(typeof(Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] string key;
    [SerializeField] bool turkishOnly;

    Text text;

    public void Setup(string englishKey, bool onlyTurkish)
    {
        key = englishKey;
        turkishOnly = onlyTurkish;
    }

    void Awake()
    {
        text = GetComponent<Text>();
        if (string.IsNullOrEmpty(key)) key = text.text;
    }

    void OnEnable()
    {
        Loc.LanguageChanged += Apply;
        Apply();
    }

    void OnDisable() => Loc.LanguageChanged -= Apply;

    void Apply()
    {
        if (turkishOnly) text.enabled = Loc.Turkish;
        text.text = Loc.T(key);
    }
}
