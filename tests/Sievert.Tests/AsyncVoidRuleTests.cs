using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Sievert.Analysis.Rules;
using Sievert.Core.Rules;

namespace Sievert.Tests;

public class AsyncVoidRuleTests
{
    private static readonly AsyncVoidRule Rule = new();

    [Fact]
    public void PlainAsyncVoid_ProducesAFinding()
    {
        Finding finding = Inspect(SampleFile()).Single(found => found.MethodName == "Save");

        Assert.Equal("SV001", finding.RuleCode);
        Assert.Equal(Severity.Error, finding.Severity);
        Assert.Equal("Patients/AsyncVoid.cs", finding.FilePath);
    }

    [Fact]
    public void EventHandler_IsLeftAlone()
    {
        // Ikinci parametresinin tip adi EventArgs ile bitiyor, bulgu uretmiyoruz.
        Assert.DoesNotContain(Inspect(SampleFile()), found => found.MethodName == "OnSaved");
    }

    [Fact]
    public void AsyncTask_ProducesNoFinding()
    {
        Assert.DoesNotContain(Inspect(SampleFile()), found => found.MethodName == "SaveAsync");
    }

    [Fact]
    public void AsyncVoidWithoutParameters_IsAlsoFound()
    {
        // Parametresi olmadigi icin event handler istisnasina giremez.
        Finding finding = Inspect(SampleFile()).Single(found => found.MethodName == "Tick");

        Assert.Equal(28, finding.Line);
    }

    [Fact]
    public void SignatureExemption_IsRecordedWithItsOwnReason()
    {
        Exemption exemption = Run(SampleFile()).Exemptions.Single(skipped => skipped.MethodName == "OnSaved");

        Assert.Equal("SV001", exemption.RuleCode);
        Assert.Equal(ExemptionReason.Signature, exemption.Reason);
    }

    [Fact]
    public void SubscriptionInTheSameFile_IsExempt()
    {
        // Ozel delegate tipli abone: imza kalibina uymuyor, karari "+= OnRetryRequested" veriyor.
        RuleResult result = Run(SampleFile("EventSubscription.cs"));

        Assert.DoesNotContain(result.Findings, found => found.MethodName == "OnRetryRequested");
        Assert.Equal(
            ExemptionReason.Subscription,
            result.Exemptions.Single(skipped => skipped.MethodName == "OnRetryRequested").Reason);
    }

    [Fact]
    public void SubscriptionInAnotherPartOfTheSamePartialClass_IsExempt()
    {
        // Abonelik Uploader.Subscriptions.cs icinde, ayni klasorde, ayni partial sinifin parcasi.
        RuleResult result = Run(SampleFile("EventSubscription.cs"));

        Assert.DoesNotContain(result.Findings, found => found.MethodName == "OnUploadFinished");
        Assert.Equal(
            ExemptionReason.Subscription,
            result.Exemptions.Single(skipped => skipped.MethodName == "OnUploadFinished").Reason);
    }

    [Fact]
    public void NoSubscriptionAndNoMatchingSignature_ProducesAFinding()
    {
        // Dar kapsam testi: OtherSubscriber.cs ayni klasorde "poller.Fired += Refresh" yaziyor
        // ama Uploader'in bir parcasi degil, o yuzden Refresh bulgu olarak kalmali.
        Assert.Contains(Run(SampleFile("EventSubscription.cs")).Findings, found => found.MethodName == "Refresh");
    }

    [Fact]
    public void SubscriptionInAnotherFolder_DoesNotExempt()
    {
        string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string near = Path.Combine(root, "Views");
        string far = Path.Combine(root, "Wiring");

        try
        {
            Directory.CreateDirectory(near);
            Directory.CreateDirectory(far);

            string handlerFile = Path.Combine(near, "Screen.cs");
            File.WriteAllText(handlerFile, "public partial class Screen { public async void OnShown(Args a) { } }");
            File.WriteAllText(
                Path.Combine(far, "Screen.Wiring.cs"),
                "public partial class Screen { void Wire(Source s) { s.Shown += OnShown; } }");

            RuleResult result = Rule.InspectFile(
                CSharpSyntaxTree.ParseText(File.ReadAllText(handlerFile), path: handlerFile),
                "Views/Screen.cs");

            Assert.Contains(result.Findings, found => found.MethodName == "OnShown");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PlainAssignment_DoesNotExempt()
    {
        // "_dropHandler = OnDropped" duz atama. Action alani olay degil, hata yine kaybolur.
        Assert.Contains(Run(SampleFile("EventSubscription.cs")).Findings, found => found.MethodName == "OnDropped");
    }

    [Fact]
    public void SubscriptionInAComment_DoesNotExempt()
    {
        // "+= OnProgress" sadece yorum satirinda geciyor; yorumlar sozdizimi dugumu degil.
        Assert.Contains(Run(SampleFile("EventSubscription.cs")).Findings, found => found.MethodName == "OnProgress");
    }

    private static IReadOnlyList<Finding> Inspect(string filePath) =>
        Run(filePath).Findings;

    private static RuleResult Run(string filePath) =>
        Rule.InspectFile(
            CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath),
            Path.Combine("Patients", Path.GetFileName(filePath)).Replace('\\', '/'));

    private static string SampleFile() =>
        SampleFile("AsyncVoid.cs");

    private static string SampleFile(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Patients", fileName);
}
