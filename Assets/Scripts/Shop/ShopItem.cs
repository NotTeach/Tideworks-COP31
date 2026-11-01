using UnityEngine;

/// <summary>Dukkandaki bir urun. Varliklar Assets/Resources/Shop altinda (Tideworks > Rebuild Shop ile uretilir).</summary>
[CreateAssetMenu(menuName = "Tideworks/Shop Item", fileName = "ShopItem")]
public class ShopItem : ScriptableObject
{
    public enum ItemType { Tool, Material, Upgrade }

    [Tooltip("Kayitta kullanilan kimlik: sewing_kit, big_net, sturdy_rod, amber_lamp, solar_motor")]
    public string id;
    public string displayName;
    [TextArea(2, 3)] public string description;
    [Tooltip("Kucuk yesil etiket; bossa gosterilmez")]
    public string recycleNote;
    public int price;
    public Sprite icon;
    public ItemType type;
    [Tooltip("Bu gunden itibaren satilir")]
    public int unlockDay = 2;
    [Tooltip("Bu gun \"NEEDED TODAY\" rozeti cikar (0 = yok)")]
    public int neededOnDay;
    [Tooltip("Dukkanda gosterilme sirasi")]
    public int order;
    [Tooltip("Tool ise denizde kusanilan alet: rod ya da net")]
    public string toolSlot;
}
