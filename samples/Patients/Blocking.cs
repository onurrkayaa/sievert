// Test verisi. Hicbir projeye dahil degil, derlenmesi gerekmiyor.
using System.Threading.Tasks;

namespace Clinic;

// SV002'nin ornekleri. Kural ad temelli calisiyor: uzerinde .Result / .Wait() /
// .GetAwaiter().GetResult() cagrilan ifade Task gibi duruyorsa bulgu uretiyor.
public class Scheduler
{
    // Bulgu: Async ile biten bir cagrinin .Result'i aliniyor.
    public int ReadCount()
    {
        return LoadCountAsync().Result;
    }

    // Bulgu: .Wait() ile bekleniyor.
    public void Flush()
    {
        FlushAsync().Wait();
    }

    // Bulgu: GetAwaiter().GetResult() zinciri.
    public string ReadName()
    {
        return LoadNameAsync().GetAwaiter().GetResult();
    }

    // Bulgu: alan adi task gibi duruyor.
    public void WaitForPending()
    {
        _pendingTask.Wait();
    }

    // Muaf: Main. Konsol uygulamasinin girisinde bloklamak mesru olabiliyor,
    // orada beklenecek bir senkronizasyon baglami zaten yok.
    public static void Main()
    {
        StartupAsync().Wait();
    }

    // Bulgu uretmemeli: Task gibi durmayan bir sey uzerinde .Result.
    // (Kendi Result property'si olan siradan bir nesne.)
    public int ReadLookupResult()
    {
        return _lookup.Result;
    }

    // Bulgu uretmemeli: await dogru kullanim.
    public async Task<int> ReadCountAsync()
    {
        return await LoadCountAsync();
    }

    private Task _pendingTask = Task.CompletedTask;
    private Lookup _lookup = new();

    private Task<int> LoadCountAsync() => Task.FromResult(1);

    private Task FlushAsync() => Task.CompletedTask;

    private Task<string> LoadNameAsync() => Task.FromResult("x");

    private static Task StartupAsync() => Task.CompletedTask;
}
