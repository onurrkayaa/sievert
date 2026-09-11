// Test verisi. Hicbir projeye dahil degil, derlenmesi gerekmiyor.
using System.Threading.Tasks;

namespace Clinic;

public interface IRegistry
{
    Task<int> GetPatientCountAsync();
}

public partial class PatientService : IRegistry
{
    private readonly string _connectionString;

    public PatientService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string ConnectionString => _connectionString;

    public async Task<int> GetPatientCountAsync()
    {
        await Task.Delay(10);
        return 42;
    }

    public string FormatName(string firstName, string lastName)
    {
        string Clean(string value) => value.Trim();

        return $"{Clean(firstName)} {Clean(lastName)}";
    }

    public class Appointment
    {
        public string Code() => "R-1";

        public async Task CancelAsync()
        {
            await Task.CompletedTask;
        }
    }
}

public partial class PatientService
{
    public bool IsActive() => true;
}

public record Diagnosis(string Code, string Description)
{
    public string Summary() => $"{Code}: {Description}";
}

public struct Measurement
{
    public double Value;

    public double Percent(double total) => total == 0 ? 0 : Value / total * 100;
}
