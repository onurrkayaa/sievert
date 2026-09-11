// Test verisi. Hicbir projeye dahil degil, derlenmesi gerekmiyor.
namespace Clinic;

// Uploader ile ayni klasorde ama onun bir parcasi degil. Kendi Refresh metodunu
// abone ediyor; Uploader.Refresh ile sadece adi ayni. Arama kapsami dar oldugu
// icin bu satir Uploader.Refresh'i muaf tutmamali.
public class OtherSubscriber
{
    public void Wire(Poller poller)
    {
        poller.Fired += Refresh;
    }

    private void Refresh()
    {
    }
}
