namespace Sievert.Cli;

/// <summary>Bir cikti parcasinin ne ise yaradigi. Renge ekranda karar veriliyor.</summary>
public enum CiktiRengi
{
    Normal,
    Soluk,
    Baslik,
    Etiket,
    Uyari,
}

/// <summary>Tek renkte basilacak bir metin parcasi.</summary>
public readonly record struct CiktiParcasi(string Metin, CiktiRengi Renk);

/// <summary>Parcalardan olusan tek bir cikti satiri.</summary>
public sealed record CiktiSatiri(IReadOnlyList<CiktiParcasi> Parcalar)
{
    /// <summary>Satirin renksiz hali. Testler ve yonlendirilmis cikti bunu kullanir.</summary>
    public string DuzMetin => string.Concat(Parcalar.Select(parca => parca.Metin));
}
