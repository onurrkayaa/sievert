using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Sievert.Web.Tests;

/// <summary>
/// On-isleme durumunun test karsiligi.
///
/// Gercek <c>PersistentComponentState</c> testte kurulamiyor (yapicisi disari kapali),
/// ama sinanmak istenen sey zaten cerceve degil: bir bilesenin anahtari nasil kurdugu,
/// veriyi bir kez mi aldigi ve birakilirken aboneligi cozup cozmedigi.
///
/// Deger JSON'a cevrilip saklaniyor. Referansi tutmak, bilesenin sakladigi nesneyi
/// sonradan degistirmesi halinde testin gercekte tasinmayan bir seyi tasinmis saymasina
/// yol acardi.
/// </summary>
internal sealed class FakePageState : IPageState
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<string, string> stored = new(StringComparer.Ordinal);

    private readonly List<Subscription> open = [];

    /// <summary>Hangi anahtarlar soruldu; bilesenin anahtar kurulumu buradan gorulur.</summary>
    public List<string> Asked { get; } = [];

    /// <summary>Kac abonelik acildi ve kac tanesi cozuldu.</summary>
    public int Subscriptions { get; private set; }

    public int Released { get; private set; }

    public bool TryTake<T>(string key, [NotNullWhen(true)] out T? value)
    {
        Asked.Add(key);

        if (stored.Remove(key, out string? json)
            && JsonSerializer.Deserialize<T>(json, Json) is T taken)
        {
            value = taken;

            return true;
        }

        value = default;

        return false;
    }

    public IDisposable Persist<T>(string key, Func<T?> snapshot)
    {
        Subscriptions++;

        Subscription subscription = new(this, key, () => snapshot() is T value
            ? JsonSerializer.Serialize(value, Json)
            : null);

        subscription.Attach();

        return subscription;
    }

    /// <summary>
    /// On-islemenin bittigi ani taklit eder: kayitli anlik goruntuleri yazar.
    ///
    /// Butce kurali gercek uygulamadakiyle ayni. Olmasaydi testler, uretimde devreyi
    /// oldurecek kadar buyuk bir paketin saklandigini varsayardi.
    /// </summary>
    public void Flush()
    {
        int used = 0;

        foreach (Subscription subscription in open)
        {
            if (subscription.Snapshot() is not string json)
            {
                continue;
            }

            int size = System.Text.Encoding.UTF8.GetByteCount(json);

            if (!PersistentPageState.Fits(size, used))
            {
                Skipped.Add(subscription.Key);

                continue;
            }

            stored[subscription.Key] = json;
            used += size;
        }
    }

    /// <summary>Butceyi astigi icin saklanmayan anahtarlar.</summary>
    public List<string> Skipped { get; } = [];

    /// <summary>Belli bir anahtar icin saklanmis veri var mi.</summary>
    public bool Has(string key) => stored.ContainsKey(key);

    /// <summary>Saklanan ham JSON; hassas veri taramasi bunu okuyor.</summary>
    public string All() => string.Join("\n", stored.Select(pair => pair.Key + " => " + pair.Value));

    public void Seed<T>(string key, T value) => stored[key] = JsonSerializer.Serialize(value, Json);

    private sealed class Subscription(FakePageState owner, string key, Func<string?> snapshot) : IDisposable
    {
        public string Key => key;

        public Func<string?> Snapshot => snapshot;

        public void Dispose()
        {
            owner.Released++;
            owner.open.Remove(this);
        }

        public void Attach() => owner.open.Add(this);
    }
}
