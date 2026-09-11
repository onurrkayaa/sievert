// Test verisi. Hicbir projeye dahil degil, derlenmesi gerekmiyor.
using System;
using System.Threading.Tasks;

namespace Clinic;

// SV003'un ornekleri. Kural ad temelli: tek basina ifade olarak duran ve adi Async ile
// biten cagrilari Task donuyor sayip bulgu uretiyor.
public class Dispatcher
{
    // Bulgu: donen Task hicbir sey yapilmadan birakiliyor.
    public void Send()
    {
        SendAsync();
    }

    // Bulgu: ConfigureAwait zinciri de await edilmemis. ConfiguredTaskAwaitable donuyor
    // ve o da beklenmiyor, yani gorev yine kayip.
    public void SendConfigured()
    {
        SendAsync().ConfigureAwait(false);
    }

    // Muaf: _ = ile bilincli olarak atiliyor. Yazan kisi donen gorevi umursamadigini
    // acikca soylemis; kural bunu tartismiyor.
    public void SendAndForget()
    {
        _ = SendAsync();
    }

    // Muaf: Task.Run icinde bilincli fire-and-forget. Cagri zaten ayri bir goreve
    // veriliyor, cagiranin onu beklemesi beklenmiyor.
    public void SendInBackground()
    {
        Task.Run(() => { SendAsync(); });
    }

    // Bulgu uretmemeli: await ediliyor.
    public async Task SendProperlyAsync()
    {
        await SendAsync();
    }

    // Bulgu uretmemeli: donen gorev bir degiskene aliniyor.
    public Task SendAndReturn()
    {
        Task pending = SendAsync();
        return pending;
    }

    // Bulgu uretmemeli: adi Async ile bitmiyor, Task donduguna dair bir isaret yok.
    public void Log()
    {
        Console.WriteLine("gonderildi");
    }

    private Task SendAsync() => Task.CompletedTask;
}
