namespace Sievert.Cli;

/// <summary>Bir cikti parcasinin ne ise yaradigi. Renge ekranda karar veriliyor.</summary>
public enum OutputColor
{
    Normal,
    Dim,
    Heading,
    Tag,
    Warning,
}

/// <summary>Tek renkte basilacak bir metin parcasi.</summary>
public readonly record struct OutputSpan(string Text, OutputColor Color);

/// <summary>Parcalardan olusan tek bir cikti satiri.</summary>
public sealed record OutputLine(IReadOnlyList<OutputSpan> Spans)
{
    /// <summary>Satirin renksiz hali. Testler ve yonlendirilmis cikti bunu kullanir.</summary>
    public string PlainText => string.Concat(Spans.Select(span => span.Text));
}
