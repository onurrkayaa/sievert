using Microsoft.CodeAnalysis;

namespace Sievert.Analysis.Rules;

/// <summary>Taranan tek bir dosya: diskteki yolu, rapora yazilan goreli yolu ve agaci.</summary>
/// <param name="AbsolutePath">Dosyanin diskteki tam yolu.</param>
/// <param name="RelativePath">Tarama kokune gore goreli yol; bulgularda bu yaziliyor.</param>
/// <param name="Tree">Dosyanin bir kez ayristirilmis sozdizimi agaci.</param>
public sealed record ScannedFile(string AbsolutePath, string RelativePath, SyntaxTree Tree);

/// <summary>
/// Bir kurala tek bir dosya icin verilen baglam. Kurallar diske bakmiyor; dosya sinirini
/// asmasi gereken bir kural, komsu dosyalari buradan aliyor.
///
/// Bunun sebebi <c>--exclude</c>: kural klasoru kendisi okusaydi, kullanicinin tarama disi
/// biraktigi bir dosya yine de sonuca karisirdi. Burada duran kume, elemeden gecmis
/// kumenin ta kendisi. Yan faydasi, ayni dosyanin iki kez okunmamasi.
/// </summary>
/// <param name="File">Incelenen dosya.</param>
/// <param name="FilesInSameFolder">
/// Ayni klasordeki diger taranan dosyalar, kendisi haric. Klasor su an elimizdeki en iyi
/// "birlikte duran dosyalar" olcutu; semantic model olmadigi icin proje ya da derleme
/// birimi gibi daha dogru bir sinirimiz yok.
/// </param>
public sealed record RuleContext(ScannedFile File, IReadOnlyList<ScannedFile> FilesInSameFolder);
