using System;
using System.Collections;
using Tideworks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Grandpa's Shop" penceresi. Her sahneden ShopPanel.Open() ile acilir: prefab Resources/UI/ShopPanel'dan
/// kendi Canvas'iyla en uste gelir. Intro modu (2. gun ilk acilis): urunler sadece gosterilir, "Great!" kapatir.
/// </summary>
public class ShopPanel : MonoBehaviour
{
    [SerializeField] RectTransform modal;
    [SerializeField] Text kicker;
    [SerializeField] Text title;
    [SerializeField] Text subtitle;
    [SerializeField] Text coinText;
    [SerializeField] Text infoText;
    [SerializeField] GameObject backButton;
    [SerializeField] GameObject greatButton;
    [SerializeField] ShopCard[] cards;

    static ShopPanel instance;
    Action onClosed;
    bool intro;

    public static bool IsOpen => instance && instance.gameObject.activeSelf;

    /// <summary>Dukkani acar. intro: sadece tanitim. onClosed: kapaninca cagrilir.</summary>
    public static ShopPanel Open(Action onClosed = null, bool intro = false)
    {
        if (!instance)
        {
            var prefab = Resources.Load<ShopPanel>("UI/ShopPanel");
            if (!prefab) { Debug.LogError("[Tideworks] Resources/UI/ShopPanel yok: Tideworks > Rebuild Shop"); onClosed?.Invoke(); return null; }
            instance = Instantiate(prefab);
            instance.name = "ShopPanel";
        }
        instance.Show(onClosed, intro);
        return instance;
    }

    void OnEnable() => PlayerWallet.OnChanged += OnCoins;

    void OnDisable() => PlayerWallet.OnChanged -= OnCoins;

    void OnCoins(int coins)
    {
        coinText.text = coins.ToString();
        foreach (var c in cards) if (c.gameObject.activeSelf) c.Refresh();
    }

    void Show(Action closed, bool introMode)
    {
        onClosed = closed;
        intro = introMode;
        gameObject.SetActive(true);
        kicker.gameObject.SetActive(intro);
        kicker.text = Loc.T("NEW · SHOP UNLOCKED");
        title.text = Loc.T("Grandpa's Shop");
        subtitle.text = Loc.T("BOAT & WORKSHOP SUPPLIES");
        infoText.text = Loc.T(intro ? "Spend your coins on better tools. Most of them are made from recycled things!"
                                    : "Buy a net or keep going with the rod. It's your choice!");
        backButton.SetActive(!intro);
        greatButton.SetActive(intro);
        coinText.text = PlayerWallet.Coins.ToString();
        var catalog = ShopManager.Catalog;
        for (int i = 0; i < cards.Length; i++)
        {
            bool on = i < catalog.Count;
            cards[i].gameObject.SetActive(on);
            if (on) cards[i].Bind(catalog[i], intro, OnCard);
        }
        TwSes.Efekt(Ses.GecisParilti, 0.7f);
        StopAllCoroutines();
        StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
        {
            float u = t / 0.3f - 1f;
            modal.localScale = Vector3.one * (1f + 2.7f * u * u * u + 1.7f * u * u);
            yield return null;
        }
        modal.localScale = Vector3.one;
    }

    void OnCard(ShopCard card)
    {
        var item = card.Item;
        if (ShopManager.Owns(item.id))
        {
            if (item.type == ShopItem.ItemType.Tool && !string.IsNullOrEmpty(item.toolSlot))
            {
                EquipmentState.Equip(item.toolSlot);
                TwSes.Efekt(Ses.Tik);
                infoText.text = Loc.F("{0} equipped.", Loc.T(item.displayName));
            }
            else infoText.text = Loc.F("You already have the {0}.", Loc.T(item.displayName));
            foreach (var c in cards) if (c.gameObject.activeSelf) c.Refresh();
            return;
        }
        switch (ShopManager.TryBuy(item))
        {
            case ShopManager.BuyResult.NotEnoughCoins:
                TwSes.Efekt(Ses.Hata, 0.7f);
                card.Shake();
                infoText.text = Loc.T("Not enough coins yet. Keep cleaning!");
                break;
            case ShopManager.BuyResult.Bought:
                TwSes.Efekt(Ses.AtikTopla);
                TwSes.Efekt(Ses.Onay, 0.6f);
                card.Flash(Loc.T("+ Bought!"));
                infoText.text = Loc.F("{0} is yours. It stays with you!", Loc.T(item.displayName));
                break;
        }
        foreach (var c in cards) if (c.gameObject.activeSelf) c.Refresh();
    }

    /// <summary>"Back" ve "Great!" butonlari.</summary>
    public void Close()
    {
        if (intro) ShopManager.IntroSeen = true;
        gameObject.SetActive(false);
        var cb = onClosed;
        onClosed = null;
        cb?.Invoke();
    }
}
