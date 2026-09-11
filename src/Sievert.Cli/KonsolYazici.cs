namespace Sievert.Cli;

/// <summary>Bicimlendirilmis satirlari ekrana basar. Renk sadece burada devreye giriyor.</summary>
public static class KonsolYazici
{
    /// <summary>Cikti dosyaya ya da boruya yonlendirildiyse renk kapanir.</summary>
    public static bool RenkKullanilsinMi() => !Console.IsOutputRedirected;

    public static void Yaz(IReadOnlyList<CiktiSatiri> satirlar, bool renkli)
    {
        foreach (CiktiSatiri satir in satirlar)
        {
            if (!renkli)
            {
                Console.WriteLine(satir.DuzMetin);
                continue;
            }

            foreach (CiktiParcasi parca in satir.Parcalar)
            {
                Console.ForegroundColor = Renk(parca.Renk);
                Console.Write(parca.Metin);
            }

            Console.ResetColor();
            Console.WriteLine();
        }
    }

    private static ConsoleColor Renk(CiktiRengi renk) => renk switch
    {
        CiktiRengi.Soluk => ConsoleColor.DarkGray,
        CiktiRengi.Baslik => ConsoleColor.Blue,
        CiktiRengi.Etiket => ConsoleColor.Cyan,
        CiktiRengi.Uyari => ConsoleColor.Yellow,
        _ => ConsoleColor.Gray,
    };
}
