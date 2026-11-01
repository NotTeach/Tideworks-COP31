using System;
using System.Collections;
using Tideworks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dukkandaki tek urun karti. Durumlar: alinabilir (sari) · altin yetmiyor (gri) · alindi, kullanimda degil ("Use this")
/// · kullanimda ("Using it ✓", sari cerceve) · bugun gerekli (kirmizi "NEEDED TODAY" rozeti).
/// </summary>
public class ShopCard : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] Text nameText;
    [SerializeField] Text description;
    [SerializeField] GameObject notePill;
    [SerializeField] Text noteText;
    [SerializeField] Button button;
    [SerializeField] Image buttonImage;
    [SerializeField] Text buttonLabel;
    [SerializeField] GameObject neededBadge;
    [SerializeField] GameObject selectedFrame;
    [SerializeField] Text feedback;
    [SerializeField] Sprite yellow, grey, teal;

    ShopItem item;
    bool intro;
    Action<ShopCard> clicked;

    public ShopItem Item => item;

    public void Bind(ShopItem it, bool introMode, Action<ShopCard> onClick)
    {
        item = it;
        intro = introMode;
        clicked = onClick;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => clicked?.Invoke(this));
        feedback.gameObject.SetActive(false);
        Refresh();
    }

    public void Refresh()
    {
        icon.sprite = item.icon;
        nameText.text = Loc.T(item.displayName);
        description.text = Loc.T(item.description);
        notePill.SetActive(!string.IsNullOrEmpty(item.recycleNote));
        if (notePill.activeSelf) noteText.text = Loc.T(item.recycleNote);
        neededBadge.SetActive(item.neededOnDay > 0 && item.neededOnDay == GameProgress.CurrentDay && !ShopManager.Owns(item.id));

        bool owned = ShopManager.Owns(item.id);
        bool inUse = owned && (item.type != ShopItem.ItemType.Tool || EquipmentState.CurrentTool == item.toolSlot);
        selectedFrame.SetActive(!intro && inUse);
        button.interactable = !intro;
        if (intro)
        {
            buttonImage.sprite = teal;
            buttonLabel.text = Loc.F("{0} coins", item.price);
            buttonLabel.color = Color.white;
        }
        else if (inUse)
        {
            buttonImage.sprite = teal;
            buttonLabel.text = Loc.T("Using it ✓");
            buttonLabel.color = Color.white;
        }
        else if (owned)
        {
            buttonImage.sprite = teal;
            buttonLabel.text = Loc.T("Use this");
            buttonLabel.color = Color.white;
        }
        else
        {
            bool afford = PlayerWallet.Coins >= item.price;
            buttonImage.sprite = afford ? yellow : grey;
            buttonLabel.text = Loc.F("Buy · {0}", item.price);
            buttonLabel.color = afford ? new Color32(70, 46, 20, 255) : new Color32(120, 114, 106, 255);
        }
    }

    /// <summary>Kartin ustunde kisa yazi ("+ Bought!") ve hafif parilti.</summary>
    public void Flash(string text)
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine(text));
    }

    IEnumerator FlashRoutine(string text)
    {
        feedback.text = text;
        feedback.gameObject.SetActive(true);
        var rt = feedback.rectTransform;
        var home = rt.anchoredPosition;
        for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime)
        {
            float k = t / 1.2f;
            rt.anchoredPosition = home + new Vector2(0, 50f * k);
            var c = feedback.color; c.a = 1f - k * k; feedback.color = c;
            transform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Mathf.Min(1f, k * 3f) * Mathf.PI));
            yield return null;
        }
        rt.anchoredPosition = home;
        transform.localScale = Vector3.one;
        feedback.gameObject.SetActive(false);
    }

    /// <summary>Altin yetmeyince butonda kisa sallanma.</summary>
    public void Shake()
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine());
    }

    IEnumerator ShakeRoutine()
    {
        var rt = (RectTransform)button.transform;
        var home = rt.anchoredPosition;
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            rt.anchoredPosition = home + new Vector2(Mathf.Sin(t * 60f) * 10f * (1f - t / 0.35f), 0);
            yield return null;
        }
        rt.anchoredPosition = home;
    }
}
