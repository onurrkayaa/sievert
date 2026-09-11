namespace Sievert.Tests;

/// <summary>
/// Console.SetOut butun surec icin gecerli. Ekrana yazan komutlari calistiran test
/// siniflari paralel kosarsa birbirinin ciktisini calarlar; bu koleksiyon onlari ayni
/// kovaya koyup sirayla calistiriyor.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ConsoleCollection
{
    public const string Name = "console";
}
