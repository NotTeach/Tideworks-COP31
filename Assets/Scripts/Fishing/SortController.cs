using System;
using System.Collections;
using System.Collections.Generic;
using Tideworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gorev 2 - Toplananlari ayir: her cop dogru kutuya atilir. Dogru +3 altin, yanlis -2 altin.
/// Yanlista esya yine de dogru kutuya gider (oyuncu dogrusunu gorur). Hepsi bitince sonuc karti, ardindan kumsal.
/// </summary>
public class SortController : MonoBehaviour
{
    [SerializeField] string menuSceneName = "MainMenu";
    [SerializeField] string beachSceneName = "Beach";
    [Tooltip("2. gunun kumsali (caretta kurtarma)")]
    [SerializeField] string day2BeachSceneName = "Day2Beach";
    [SerializeField] CanvasGroup screenFader;
    [SerializeField] Sprite[] trashSprites;   // TrashCatalog.Trash sirasiyla

    [Header("Ust bar")]
    [SerializeField] Text placeText;
    [SerializeField] RectTransform healthFill;
    [SerializeField] float healthWidth = 300f;
    [SerializeField] Text healthText;
    [SerializeField] int rightCoins = 3;
    [SerializeField] int wrongCoins = 2;

    [Header("Kutular (TrashBin sirasiyla)")]
    [SerializeField] Image[] binRows;
    [SerializeField] Text[] binCounts;
    [SerializeField] Sprite rowNormal, rowRight, rowWrong;

    [Header("Ortadaki esya")]
    [SerializeField] Text itemIndex;
    [SerializeField] Text itemName;
    [SerializeField] Text question;
    [SerializeField] Image itemImage;
    [SerializeField] float itemScale = 2.4f;

    [Header("Skor ve canta")]
    [SerializeField] Text rightCount;
    [SerializeField] Text wrongCount;
    [SerializeField] Text coinsSoFar;
    [SerializeField] Text coinText;
    [SerializeField] Text bagTitle;
    [SerializeField] Image[] bagTiles;
    [SerializeField] Text floatingText;

    [Header("Sonuc")]
    [SerializeField] GameObject finishOverlay;
    [SerializeField] RectTransform finishCard;
    [SerializeField] Text finishCoins;
    [SerializeField] Text finishScore;

    List<int> queue;
    Sprite[] sprites;
    readonly int[] counts = new int[5];
    int index, right, wrong, earned;
    bool busy;

    void Start()
    {
        Time.timeScale = 1f;
        foreach (var button in GetComponentsInChildren<Button>(true))
            if (!button.GetComponent<TwButonSesi>())
                button.gameObject.AddComponent<TwButonSesi>();
        placeText.text = Loc.F("Recycling yard · Day {0}", GameProgress.CurrentDay);
        float health = GameProgress.OceanHealth / 100f;
        healthFill.sizeDelta = new Vector2(Mathf.Max(18f, healthWidth * health), healthFill.sizeDelta.y);
        healthText.text = GameProgress.OceanHealth + "%";
        // Recycling dogrudan acildiysa (deneme) butun copler gelir
        var items = CatchBag.Items != null && CatchBag.Items.Count > 0
            ? new List<int>(CatchBag.Items)
            : new List<int>(System.Linq.Enumerable.Range(0, trashSprites.Length));
        Begin(items, trashSprites);
        TwSes.EkranGecisi(Ses.MuzikTamirciCaz, Ses.AmbiyansAtolye, null);
        if (screenFader) StartCoroutine(FadeScreen(1f, 0f, null));
    }

    public void Begin(List<int> items, Sprite[] trashSprites)
    {
        queue = items;
        sprites = trashSprites;
        index = right = wrong = earned = 0;
        Array.Clear(counts, 0, counts.Length);
        finishOverlay.SetActive(false);
        floatingText.gameObject.SetActive(false);
        for (int i = 0; i < binRows.Length; i++) { binRows[i].sprite = rowNormal; binCounts[i].text = "0"; }
        ShowItem();
        RefreshScore();
    }

    void ShowItem()
    {
        int item = queue[index];
        var e = TrashCatalog.Trash[item];
        itemIndex.text = Loc.F("ITEM {0} OF {1}", index + 1, queue.Count);
        itemName.text = Loc.T(e.name);
        question.text = Loc.T("Which bin does it go in?");
        itemImage.sprite = sprites[item];
        itemImage.SetNativeSize();
        itemImage.rectTransform.sizeDelta *= itemScale;
        itemImage.rectTransform.localScale = Vector3.one;
        var c = itemImage.color; c.a = 1f; itemImage.color = c;

        int left = queue.Count - index - 1;
        bagTitle.text = Loc.F("STILL IN THE BAG · {0}", left);
        for (int i = 0; i < bagTiles.Length; i++)
        {
            int q = index + 1 + i;
            var icon = bagTiles[i].transform.GetChild(0).GetComponent<Image>();
            bagTiles[i].gameObject.SetActive(q < queue.Count);
            if (q < queue.Count) icon.sprite = sprites[queue[q]];
        }
    }

    /// <summary>Kutu satirina tiklandi (TrashBin sirasi).</summary>
    public void Choose(int bin)
    {
        if (busy || queue == null || index >= queue.Count) return;
        StartCoroutine(Sort(bin));
    }

    IEnumerator Sort(int chosen)
    {
        busy = true;
        var e = TrashCatalog.Trash[queue[index]];
        int correct = (int)e.bin;
        bool ok = chosen == correct;
        if (ok)
        {
            right++;
            earned += rightCoins;
            PlayerWallet.Add(rightCoins);
            TwSes.Efekt(Ses.Onay);
            Float("+" + rightCoins, binRows[chosen].rectTransform, new Color32(40, 140, 80, 255));
        }
        else
        {
            wrong++;
            int lose = Mathf.Min(wrongCoins, GameProgress.Coins);
            earned -= lose;
            PlayerWallet.Add(-lose);
            TwSes.Efekt(Ses.Hata);
            Float("-" + wrongCoins, binRows[chosen].rectTransform, new Color32(190, 60, 40, 255));
            question.text = Loc.F("Not quite! It goes in {0}.", Loc.T(TrashCatalog.BinNames[correct]));
            binRows[chosen].sprite = rowWrong;
        }
        binRows[correct].sprite = rowRight;
        counts[correct]++;
        binCounts[correct].text = counts[correct].ToString();
        RefreshScore();

        // Esya dogru kutuya ucar
        var rt = itemImage.rectTransform;
        Vector2 from = rt.anchoredPosition;
        Vector2 to = LocalIn((RectTransform)rt.parent, binRows[correct].rectTransform);
        yield return new WaitForSecondsRealtime(ok ? 0.15f : 0.8f);
        yield return Tween(0.4f, t =>
        {
            rt.anchoredPosition = Vector2.Lerp(from, to, t * t);
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.25f, t);
        });
        rt.anchoredPosition = from;
        binRows[correct].sprite = rowNormal;
        binRows[chosen].sprite = rowNormal;

        index++;
        if (index >= queue.Count) { yield return Finish(); yield break; }
        ShowItem();
        busy = false;
    }

    IEnumerator Finish()
    {
        GameSettings.Save();
        TwSes.Efekt(Ses.OnarimTamam);
        itemImage.gameObject.SetActive(false);
        finishCoins.text = (earned >= 0 ? "+" : "") + Loc.F("{0} coins", earned);
        finishScore.text = Loc.F("Right {0} · Wrong {1}", right, wrong);
        finishOverlay.SetActive(true);
        yield return Tween(0.35f, t => finishCard.localScale = Vector3.one * EaseOutBack(t));
    }

    /// <summary>Sonuc kartindaki "Continue": Gorev 3 (carettalara yardim) kumsalina gecer.</summary>
    public void Done() => Leave(GameProgress.CurrentDay == 2 ? day2BeachSceneName : beachSceneName);

    /// <summary>Ust bardaki menu butonu.</summary>
    public void BackToMenu() => Leave(menuSceneName);

    bool leaving;

    void Leave(string scene)
    {
        if (leaving) return;
        leaving = true;
        GameSettings.Save();
        CatchBag.Items.Clear();
        StartCoroutine(FadeScreen(0f, 1f, () => SceneManager.LoadScene(scene)));
    }

    IEnumerator FadeScreen(float from, float to, Action done)
    {
        if (!screenFader) { done?.Invoke(); yield break; }
        screenFader.gameObject.SetActive(true);
        screenFader.blocksRaycasts = true;
        yield return Tween(0.45f, t => screenFader.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)));
        if (to == 0f) screenFader.gameObject.SetActive(false);
        done?.Invoke();
    }

    void RefreshScore()
    {
        rightCount.text = right.ToString();
        wrongCount.text = wrong.ToString();
        coinsSoFar.text = Loc.F("{0} coins so far", (earned >= 0 ? "+" : "") + earned);
        coinText.text = GameProgress.Coins.ToString();
    }

    void Float(string text, RectTransform over, Color color)
    {
        var t = Instantiate(floatingText, floatingText.transform.parent);
        t.gameObject.SetActive(true);
        t.text = text;
        t.color = color;
        var start = LocalIn((RectTransform)t.transform.parent, over) + new Vector2(200, 0);
        StartCoroutine(FloatUp(t, start));
    }

    IEnumerator FloatUp(Text t, Vector2 start)
    {
        yield return Tween(1f, k =>
        {
            t.rectTransform.anchoredPosition = start + new Vector2(0, 80f * k);
            var c = t.color; c.a = 1f - k * k; t.color = c;
        });
        Destroy(t.gameObject);
    }

    static Vector2 LocalIn(RectTransform parent, RectTransform target)
    {
        Vector2 p = parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
        return p - new Vector2(parent.rect.xMin, parent.rect.yMax);
    }

    static float EaseOutBack(float t)
    {
        const float c = 1.7f;
        float u = t - 1f;
        return 1f + (c + 1f) * u * u * u + c * u * u;
    }

    static IEnumerator Tween(float duration, Action<float> step)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            step(t / duration);
            yield return null;
        }
        step(1f);
    }
}
