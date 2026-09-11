using Sievert.Analysis;
using Sievert.Core.Analysis;

namespace Sievert.Tests;

public class FileAnalyzerTests
{
    private static FileAnalysis AnalyzeSample(string fileName) =>
        FileAnalyzer.AnalyzeFile(Path.Combine(AppContext.BaseDirectory, "Patients", fileName));

    [Fact]
    public void Normal_TypeCountIsCorrect()
    {
        FileAnalysis analysis = AnalyzeSample("Normal.cs");

        // Partial olan PatientService tek tip sayilir.
        Assert.Equal(
            ["Diagnosis", "IRegistry", "Measurement", "PatientService", "PatientService.Appointment"],
            analysis.Types.Select(type => type.Name).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Normal_MethodCountIsCorrect()
    {
        FileAnalysis analysis = AnalyzeSample("Normal.cs");

        Assert.Equal(8, analysis.Types.Sum(type => type.Methods.Count));
    }

    [Fact]
    public void Normal_AsyncMethodsAreMarked()
    {
        FileAnalysis analysis = AnalyzeSample("Normal.cs");

        string[] asyncOnes = analysis.Types
            .SelectMany(type => type.Methods)
            .Where(method => method.IsAsync)
            .Select(method => method.Name)
            .ToArray();

        Assert.Equal(2, asyncOnes.Length);
        Assert.Contains("GetPatientCountAsync", asyncOnes);
        Assert.Contains("CancelAsync", asyncOnes);
    }

    [Fact]
    public void Normal_NestedClassIsFound()
    {
        FileAnalysis analysis = AnalyzeSample("Normal.cs");

        SievertType appointment = analysis.Types.Single(type => type.Name == "PatientService.Appointment");

        Assert.Equal(SievertTypeKind.Class, appointment.Kind);
        Assert.Equal(["Code", "CancelAsync"], appointment.Methods.Select(method => method.Name).ToArray());
    }

    [Fact]
    public void Normal_LocalFunctionAndConstructorAreNotCountedAsMethods()
    {
        FileAnalysis analysis = AnalyzeSample("Normal.cs");

        SievertType service = analysis.Types.Single(type => type.Name == "PatientService");

        // Partial'in iki parcasindaki metotlar birlesir: iki normal metot + IsActive.
        Assert.Equal(["GetPatientCountAsync", "FormatName", "IsActive"], service.Methods.Select(method => method.Name).ToArray());
        Assert.DoesNotContain(service.Methods, method => method.Name == "Clean");
        Assert.DoesNotContain(service.Methods, method => method.Name == "PatientService");
        Assert.DoesNotContain(service.Methods, method => method.Name == "ConnectionString");
    }

    [Fact]
    public void Normal_ExpressionBodiedMethodIsCounted()
    {
        FileAnalysis analysis = AnalyzeSample("Normal.cs");

        SievertMethod percent = analysis.Types.Single(type => type.Name == "Measurement").Methods.Single();

        Assert.Equal("Percent", percent.Name);
        Assert.Equal(1, percent.ParameterCount);
        Assert.Equal("double", percent.ReturnType);
        Assert.False(percent.IsAsync);
    }

    [Fact]
    public void Broken_DoesNotThrow_ListsErrors()
    {
        FileAnalysis analysis = AnalyzeSample("Broken.cs");

        Assert.NotEmpty(analysis.ParseErrors);
        // Cozulebilen kisim yine de geri gelmeli.
        Assert.Contains(analysis.Types, type => type.Name == "BrokenService");
        Assert.Contains(analysis.Types.SelectMany(type => type.Methods), method => method.Name == "Add");
    }

    [Fact]
    public void Normal_HasNoErrors()
    {
        FileAnalysis analysis = AnalyzeSample("Normal.cs");

        Assert.Empty(analysis.ParseErrors);
    }
}
