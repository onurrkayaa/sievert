using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Microsoft.AspNetCore.Components;

namespace Sievert.Web;

/// <summary>
/// Sayfanin on-islemede cektigi veriyi etkilesimli asamaya tasir.
///
/// Blazor Interactive Server bir sayfayi **iki kez** olusturuyor: once sunucuda on-isleme
/// icin, sonra devre baglaninca etkilesimli olarak. Iki olusturma da veriyi kendisi
/// cekerse ayni istek iki kez gidiyor; Adim 4'un olcumunde ilk iki istek arasinda 134 ms
/// vardi ve ikisi ayni cevabi getiriyordu.
///
/// Arayuz olarak duruyor cunku cerceve tipi (<c>PersistentComponentState</c>) testte
/// kurulamiyor; bilesenler bu arayuzu goruyor, testler kendi uygulamasini veriyor.
/// </summary>
public interface IPageState
{
    /// <summary>
    /// On-islemeden kalan veriyi alir ve **tuketir**. Ikinci cagri bos doner: veri tek
    /// kullanimlik, yoksa eski bir cevap sonsuza kadar ekranda kalirdi.
    /// </summary>
    bool TryTake<T>(string key, [NotNullWhen(true)] out T? value);

    /// <summary>
    /// On-isleme bitince <paramref name="snapshot"/> cagrilir ve donen deger saklanir.
    /// Donen abonelik bilesen birakildiginda cozulmeli.
    /// </summary>
    IDisposable Persist<T>(string key, Func<T?> snapshot);
}

/// <summary>
/// Durum anahtarlari.
///
/// Anahtar sonucu degistiren **butun** girdileri icermek zorunda: sayfa, depo, is, sha,
/// sayfa numarasi, boyut, siralama, filtreler, pencere. Biri eksik kalirsa farkli bir
/// filtreyle acilan sayfa on-islemeden kalan eski veriyi alir ve kullanici filtresinin
/// calistigini sanir.
/// </summary>
public static class PageStateKey
{
    public static string For(string page, params (string Name, object? Value)[] parts)
    {
        string tail = string.Join(
            "|",
            parts.Select(part => $"{part.Name}={Text(part.Value)}"));

        return tail.Length == 0 ? page : page + "|" + tail;
    }

    /// <summary>
    /// Deger metni degismez kulturle uretiliyor. Sunucunun kulturu anahtari
    /// degistirmemeli; aksi halde ayni sayfa iki kulturde iki farkli anahtar uretir ve
    /// on-islemeden kalan veri hic alinamaz.
    /// </summary>
    private static string Text(object? value) => value switch
    {
        null => "-",
        bool flag => flag ? "true" : "false",
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "-",
    };
}

/// <summary>Bilesenlerin tek satirda kullandigi yardimci.</summary>
public static class PageStateExtensions
{
    /// <summary>
    /// Veriyi once on-islemeden almayi dener, yoksa API'den ceker.
    ///
    /// Bilerek <see cref="Sievert.Web.Api.ApiResult{T}"/> donuyor: cagiran taraf iki
    /// yolu ayirt etmek zorunda kalmasin ve hata gosterimi tek yerde kalsin.
    /// </summary>
    public static async Task<Api.ApiResult<T>> TakeOrFetchAsync<T>(
        this IPageState state,
        string key,
        Func<Task<Api.ApiResult<T>>> fetch) =>
        state.TryTake(key, out T? cached) ? Api.ApiResult<T>.Ok(cached) : await fetch();
}

/// <summary>Cercevenin <c>PersistentComponentState</c> nesnesini saran hali.</summary>
public sealed class PersistentPageState(PersistentComponentState state) : IPageState
{
    public bool TryTake<T>(string key, [NotNullWhen(true)] out T? value)
    {
        if (state.TryTakeFromJson(key, out T? taken) && taken is not null)
        {
            value = taken;

            return true;
        }

        value = default;

        return false;
    }

    public IDisposable Persist<T>(string key, Func<T?> snapshot) =>
        state.RegisterOnPersisting(() =>
        {
            if (snapshot() is T value)
            {
                // Hassas veri buraya girmiyor: saklanan sey API'nin zaten dondugu
                // sozlesme nesnesi ve o nesnelerde yol, baglanti dizesi ya da yazar
                // e-postasi yok (ContractShapeTests).
                state.PersistAsJson(key, value);
            }

            return Task.CompletedTask;
        });
}

/// <summary>
/// Birden fazla abonelik tek bir <c>IDisposable</c> gibi tutulsun diye.
///
/// Bir sayfa birden cok veri parcasi cekiyorsa her parcanin kendi anahtari oluyor; o
/// zaman da her parca icin ayri bir abonelik doguyor ve hepsinin birakilmasi gerekiyor.
/// </summary>
public sealed class Subscriptions(params IDisposable[] items) : IDisposable
{
    public void Dispose()
    {
        foreach (IDisposable item in items)
        {
            item.Dispose();
        }
    }
}
