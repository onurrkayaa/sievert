namespace Sievert.Core.Analysis;

/// <summary>Bir dosyada bulunan tek bir metodun özeti.</summary>
/// <param name="Name">Metodun adı.</param>
/// <param name="StartLine">Metodun başladığı satır (1'den başlar).</param>
/// <param name="LineCount">İmzadan gövdenin sonuna kadar kaç satır tuttuğu.</param>
/// <param name="IsAsync">Metot async olarak işaretlenmiş mi.</param>
/// <param name="ParameterCount">Metodun aldığı parametre sayısı.</param>
/// <param name="ReturnType">Dönüş tipinin kaynak koddaki yazılışı, örneğin "Task&lt;int&gt;".</param>
public sealed record SievertMethod(
    string Name,
    int StartLine,
    int LineCount,
    bool IsAsync,
    int ParameterCount,
    string ReturnType);
