using Sievert.Cli;
using Sievert.Core.Rules;

using static Sievert.Tests.Samples;

namespace Sievert.Tests;

public class CheckCommandTests
{
    [Fact]
    public void NoFindings_ExitCodeIsZero()
    {
        Assert.Equal(0, CheckCommand.ExitCode([], ArgumentParser.DefaultFailOn));
    }

    [Fact]
    public void FindingAtDefaultLevel_ExitCodeIsOne()
    {
        Assert.Equal(1, CheckCommand.ExitCode([Found(severity: Severity.Error)], ArgumentParser.DefaultFailOn));
    }

    [Fact]
    public void FindingBelowTheThreshold_ExitCodeIsZero()
    {
        // Varsayilan esik warning, bilgi seviyesindeki bulgu build'i kirmiyor.
        Assert.Equal(0, CheckCommand.ExitCode([Found(severity: Severity.Info)], Severity.Warning));
    }

    [Fact]
    public void FindingExactlyAtTheThreshold_ExitCodeIsOne()
    {
        Assert.Equal(1, CheckCommand.ExitCode([Found(severity: Severity.Warning)], Severity.Warning));
    }

    [Fact]
    public void ThresholdCanBeRaised()
    {
        Finding[] findings = [Found(severity: Severity.Warning)];

        Assert.Equal(0, CheckCommand.ExitCode(findings, Severity.Error));
        Assert.Equal(1, CheckCommand.ExitCode(findings, Severity.Warning));
    }
}
