// Test verisi. Hicbir projeye dahil degil, derlenmesi gerekmiyor.
using System;
using System.Threading.Tasks;

namespace Clinic;

// SV001'in abonelik kanitini denemek icin. Buradaki metotlarin hicbiri
// (object sender, EventArgs e) kalibina uymuyor, yani imza heuristigi hepsini
// birakiyor; karari abonelik kaniti veriyor.
public partial class Uploader
{
    private Action<DropData>? _dropHandler;

    // Muaf: abonelik ayni partial sinifin baska bir parcasinda,
    // Uploader.Subscriptions.cs icinde "queue.Finished += OnUploadFinished".
    public async void OnUploadFinished(UploadResult result)
    {
        await Task.Delay(10);
    }

    // Muaf: abonelik ayni dosyada, asagidaki Wire metodunda.
    public async void OnRetryRequested(RetryInfo info)
    {
        await Task.Delay(10);
    }

    public void Wire(Retrier retrier)
    {
        retrier.RetryRequested += OnRetryRequested;
    }

    // Bulgu: ne abonelik var ne de imza uyuyor. Ayni klasordeki
    // OtherSubscriber.cs "poller.Fired += Refresh" yaziyor ama o dosya
    // Uploader'in bir parcasi degil, o yuzden kanit sayilmiyor.
    public async void Refresh()
    {
        await Task.Delay(10);
    }

    // Bulgu: abonelik sadece yorum satirinda yaziyor.
    // progress.Changed += OnProgress;
    public async void OnProgress(int percent)
    {
        await Task.Delay(10);
    }

    // Bulgu: duz atama abonelik sayilmiyor. Action alani bir olay degil,
    // hata yine kaybolur.
    public async void OnDropped(DropData data)
    {
        await Task.Delay(10);
    }

    public void UseDropHandler()
    {
        _dropHandler = OnDropped;
    }
}
