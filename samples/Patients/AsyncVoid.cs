// Test verisi. Hicbir projeye dahil degil, derlenmesi gerekmiyor.
using System;
using System.Threading.Tasks;

namespace Clinic;

public class Notifier
{
    // SV001 bunu yakalamali: async isaretli ama void donuyor.
    public async void Save(string name)
    {
        await Task.Delay(10);
    }

    // Event handler kalibina uyuyor, muaf tutuluyor.
    public async void OnSaved(object sender, EventArgs e)
    {
        await Task.Delay(10);
    }

    // Dogru yazilmis async, bulgu uretmemeli.
    public async Task SaveAsync(string name)
    {
        await Task.Delay(10);
    }

    // Sinir durum: parametresiz async void. Event handler olamaz, yakalanmali.
    public async void Tick()
    {
        await Task.CompletedTask;
    }
}
