using Sievert.Core.Analysis;
using Sievert.Core.Rules;

namespace Sievert.Tests;

/// <summary>Testlerde elle analiz nesnesi kurmak icin kisa yollar.</summary>
internal static class Samples
{
    public static SievertMethod Method(string name, int lineCount = 1, bool isAsync = false, int startLine = 1) =>
        new(name, startLine, lineCount, isAsync, ParameterCount: 0, ReturnType: "void");

    public static SievertType Type(string name, params SievertMethod[] methods) =>
        new(name, SievertTypeKind.Class, StartLine: 1, methods);

    public static FileAnalysis Sample(string path, params SievertType[] types) =>
        new(path, types, TotalLineCount: 100, ParseErrors: []);

    public static FileAnalysis BrokenSample(string path, params string[] errors) =>
        new(path, [], TotalLineCount: 10, errors);

    public static FileAnalysis TypelessSample(string path) =>
        new(path, [], TotalLineCount: 5, ParseErrors: []);

    public static Finding Found(
        string ruleCode = "SV001",
        string methodName = "Save",
        Severity severity = Severity.Error,
        string filePath = "a.cs",
        int line = 1) =>
        new(
            ruleCode,
            "async void metot",
            $"{methodName} metodu async void. Donus tipini Task yaparsan hatalar cagirana ulasir.",
            "async void bir metotta olusan hata cagirana ulasmaz.",
            filePath,
            line,
            methodName,
            severity);
}
