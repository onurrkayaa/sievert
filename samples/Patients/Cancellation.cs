// Test verisi. Hicbir projeye dahil degil, derlenmesi gerekmiyor.
using System.Threading;
using System.Threading.Tasks;

namespace Clinic;

// SV006'nin ornekleri. Task donen public metotlarda CancellationToken yoksa
// cagiran islemi iptal edemiyor. Bu bir hata degil, eksik yetenek: info.
public class PatientService : IPatientService
{
    // Bulgu: public, Task donuyor, token almiyor.
    public Task SaveAsync(string name)
    {
        return Task.CompletedTask;
    }

    // Bulgu: ValueTask de sayiliyor.
    public ValueTask<int> CountAsync()
    {
        return new ValueTask<int>(0);
    }

    // Bulgu uretmemeli: token zaten var.
    public Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    // Muaf: private. Cagiran disaridan gelmiyor, imza serbestce degistirilebilir.
    private Task CacheAsync()
    {
        return Task.CompletedTask;
    }

    // Muaf: explicit interface uygulamasi, imza degistirilemez.
    Task IPatientService.LoadAsync()
    {
        return Task.CompletedTask;
    }

    // Bulgu uretmemeli: Task donmuyor.
    public string Describe()
    {
        return "hasta";
    }
}

public class AuditedPatientService : PatientService
{
    // Muaf: override, imza taban siniftan geliyor.
    public override Task SaveAsync(string name)
    {
        return Task.CompletedTask;
    }
}
