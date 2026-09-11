// Test verisi. Hicbir projeye dahil degil, derlenmesi gerekmiyor.
namespace Clinic;

// Uploader'in ikinci parcasi. Abonelik burada duruyor, abone olan async void
// metot ise EventSubscription.cs icinde.
public partial class Uploader
{
    public void Attach(UploadQueue queue)
    {
        queue.Finished += OnUploadFinished;
    }
}
