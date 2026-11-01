public enum TrashBin { Metal, Glass, Paper, Plastic, Household }

/// <summary>
/// Denizde toplanan coplerin ve baliklarin listesi. Sprite adlari Assets/Art/Fishing'deki dosyalardir.
/// Sira onemli: FishingController ve SortController sprite dizilerini bu siraya gore tutar.
/// </summary>
public static class TrashCatalog
{
    public struct Entry
    {
        public string sprite, name;
        public TrashBin bin;
        public Entry(string sprite, string name, TrashBin bin) { this.sprite = sprite; this.name = name; this.bin = bin; }
    }

    public static readonly Entry[] Trash =
    {
        new Entry("TrashTinCan", "Tin can", TrashBin.Metal),
        new Entry("TrashTunaCan", "Tuna can", TrashBin.Metal),
        new Entry("TrashGlassBottle", "Glass bottle", TrashBin.Glass),
        new Entry("TrashGlassJar", "Glass jar", TrashBin.Glass),
        new Entry("TrashNewspaper", "Newspaper", TrashBin.Paper),
        new Entry("TrashWaterBottle", "Plastic water bottle", TrashBin.Plastic),
        new Entry("TrashPlasticBag", "Plastic bag", TrashBin.Plastic),
        new Entry("TrashSixPack", "Six-pack rings", TrashBin.Plastic),
        new Entry("TrashGhostNet", "Ghost net", TrashBin.Plastic),
        new Entry("TrashBuoy", "Fishing float", TrashBin.Plastic),
        new Entry("TrashRope", "Old rope", TrashBin.Household),
        new Entry("TrashFlipFlop", "Flip-flop", TrashBin.Household),
        new Entry("TrashPlank", "Dirty wood", TrashBin.Household),
    };

    public static readonly string[] FishSprites = { "FishSeaBream", "FishRedMullet", "FishAnchovy", "FishPuffer", "FishMackerel" };

    public static readonly string[] BinNames = { "Metal", "Glass", "Paper", "Plastic", "Household waste" };
    public static readonly string[] BinHints = { "cans, tins, lids", "bottles, jars", "newspaper, cartons", "bottles, bags, nets", "rubber, rope, dirty wood" };
}
