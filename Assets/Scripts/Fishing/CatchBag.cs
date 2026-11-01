using System.Collections.Generic;

/// <summary>
/// Denizde toplanan coplerin listesi (TrashCatalog.Trash indeksleri): Fishing sahnesi doldurur,
/// Recycling sahnesi okur. Recycling dogrudan acilirsa bos gelir; o zaman butun copler kullanilir.
/// </summary>
public static class CatchBag
{
    public static List<int> Items = new List<int>();
}
