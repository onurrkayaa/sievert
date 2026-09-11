using Sievert.Analysis;
using Sievert.Core.Analysis;

namespace Sievert.Tests;

public class EnumAndDelegateTests
{
    [Fact]
    public void Enum_IsCountedAsType()
    {
        SievertType type = Analyze("public enum Durum { Acik, Kapali }").Types.Single();

        Assert.Equal("Durum", type.Name);
        Assert.Equal(SievertTypeKind.Enum, type.Kind);
        Assert.Equal(1, type.StartLine);
    }

    [Fact]
    public void Enum_MethodListStaysEmpty()
    {
        Assert.Empty(Analyze("public enum Durum { Acik, Kapali }").Types.Single().Methods);
    }

    [Fact]
    public void Delegate_IsCountedAsType()
    {
        SievertType type = Analyze("public delegate int Secici(string deger);").Types.Single();

        Assert.Equal("Secici", type.Name);
        Assert.Equal(SievertTypeKind.Delegate, type.Kind);
        Assert.Empty(type.Methods);
    }

    [Fact]
    public void NestedEnumAndDelegate_AreQualifiedWithOuterTypeName()
    {
        FileAnalysis analysis = Analyze("""
            public class Dis
            {
                public enum Durum { Acik }

                public delegate void Haberci();

                public void Calis() { }
            }
            """);

        Assert.Equal(
            ["Dis", "Dis.Durum", "Dis.Haberci"],
            analysis.Types.Select(type => type.Name).Order(StringComparer.Ordinal).ToArray());

        // Enum ve delegate araya girse de metot hala dogru tipe yaziliyor.
        Assert.Equal(["Calis"], analysis.Types.Single(type => type.Name == "Dis").Methods.Select(method => method.Name).ToArray());
    }

    [Fact]
    public void FileWithOnlyEnum_IsNotCountedAsTypeless()
    {
        Assert.False(Analyze("public enum Durum { Acik }").NoTypesFound);
    }

    [Theory]
    [InlineData(SievertTypeKind.Class, "class")]
    [InlineData(SievertTypeKind.Record, "record")]
    [InlineData(SievertTypeKind.Struct, "struct")]
    [InlineData(SievertTypeKind.Interface, "interface")]
    [InlineData(SievertTypeKind.Enum, "enum")]
    [InlineData(SievertTypeKind.Delegate, "delegate")]
    public void Keyword_GivesCSharpNameForEveryKind(SievertTypeKind kind, string expected)
    {
        Assert.Equal(expected, kind.Keyword());
        Assert.Equal(kind, TypeKindNames.Parse(expected));
    }

    private static FileAnalysis Analyze(string source) => FileAnalyzer.AnalyzeText(source, "test.cs");
}
