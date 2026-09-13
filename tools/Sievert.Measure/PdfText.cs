using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Sievert.Measure;

/// <summary>
/// PDF'ten metin cikarir.
///
/// Neden kendi cikaricim var: rapordaki sayilarin dogrulanmasi, raporu ureten koddan
/// **bagimsiz** olmali. Uretici kutuphanenin kendi API'siyle okumak, ayni hatayi iki kez
/// yapmayi yakalamaz. Disaridan bir program (pdftotext gibi) cagirmak ise araci o
/// programin kuruluyor olmasina baglardi.
///
/// Kapsam sinirli ve bilerek oyle: bu cikarici yalniz bu projenin urettigi PDF'leri
/// okuyabiliyor - Flate ile sikistirilmis icerik akislari ve gomulu fontlarin ToUnicode
/// eslemesi. Genel bir PDF ayristiricisi degil.
/// </summary>
public static partial class PdfText
{
    /// <summary>Belgedeki butun gorunur metin, sayfa sirasinda.</summary>
    public static string Extract(byte[] document)
    {
        string raw = Encoding.Latin1.GetString(document);

        Dictionary<int, Dictionary<int, string>> maps = ToUnicodeMaps(document, raw);
        Dictionary<string, int> fonts = FontNames(raw);

        StringBuilder text = new();

        foreach (string content in Streams(document, raw))
        {
            if (content.Contains("beginbfchar", StringComparison.Ordinal)
                || content.Contains("/Type /Font", StringComparison.Ordinal))
            {
                continue;
            }

            Append(text, content, maps, fonts);
        }

        return text.ToString();
    }

    /// <summary>Belgedeki sayfa sayisi.</summary>
    public static int PageCount(byte[] document) =>
        PageObject().Matches(Encoding.Latin1.GetString(document)).Count;

    /// <summary>Bir icerik akisindaki metni, font degisimlerini izleyerek cozer.</summary>
    private static void Append(
        StringBuilder text,
        string content,
        Dictionary<int, Dictionary<int, string>> maps,
        Dictionary<string, int> fonts)
    {
        Dictionary<int, string>? current = null;

        foreach (Match match in Token().Matches(content))
        {
            if (match.Groups["font"].Success)
            {
                current = fonts.TryGetValue(match.Groups["font"].Value, out int objectNumber)
                    && maps.TryGetValue(objectNumber, out Dictionary<int, string>? map)
                        ? map
                        : null;

                continue;
            }

            // Metin cogunlukla TJ dizisi icinde geliyor: aralarinda konum duzeltmesi
            // olan birden fazla onaltilik parca. Parcalar birlestiriliyor.
            if (match.Groups["array"].Success)
            {
                foreach (Match piece in HexString().Matches(match.Groups["array"].Value))
                {
                    text.Append(Decode(piece.Groups["hex"].Value, current));
                }

                continue;
            }

            if (match.Groups["hex"].Success)
            {
                text.Append(Decode(match.Groups["hex"].Value, current));

                continue;
            }

            if (match.Groups["newline"].Success)
            {
                text.Append(' ');
            }
        }

        text.Append('\n');
    }

    /// <summary>Onaltilik dizeyi fontun ToUnicode eslemesiyle metne cevirir.</summary>
    private static string Decode(string hex, Dictionary<int, string>? map)
    {
        StringBuilder decoded = new();

        for (int index = 0; index + 3 < hex.Length + 1 && index + 4 <= hex.Length; index += 4)
        {
            int code = int.Parse(hex.AsSpan(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            decoded.Append(map is not null && map.TryGetValue(code, out string? value)
                ? value
                : '�');
        }

        return decoded.ToString();
    }

    /// <summary>Icerik akislarindaki font adlarini nesne numarasina baglar.</summary>
    private static Dictionary<string, int> FontNames(string raw)
    {
        Dictionary<string, int> fonts = new(StringComparer.Ordinal);

        foreach (Match match in FontResource().Matches(raw))
        {
            fonts[match.Groups["name"].Value] = int.Parse(
                match.Groups["object"].Value, CultureInfo.InvariantCulture);
        }

        return fonts;
    }

    /// <summary>
    /// Font nesnesi -> ToUnicode eslemesi.
    ///
    /// Esleme font basina tutuluyor: gomulu altkume fontlarinda ayni glif numarasi iki
    /// fontta iki farkli harf olabiliyor ve tek bir genel tablo sessizce yanlis metin
    /// uretirdi.
    /// </summary>
    private static Dictionary<int, Dictionary<int, string>> ToUnicodeMaps(byte[] document, string raw)
    {
        Dictionary<int, int> toUnicodeOf = [];

        // Nesne bloklari tek tek geziliyor: tek bir buyuk desen, bir nesnenin sonuyla
        // digerinin basini birlestirip yanlis eslesme uretiyordu.
        foreach (Match block in ObjectBlock().Matches(raw))
        {
            string body = block.Groups["body"].Value;

            if (!body.Contains("/Type /Font", StringComparison.Ordinal))
            {
                continue;
            }

            if (ToUnicodeReference().Match(body) is { Success: true } reference)
            {
                toUnicodeOf[int.Parse(block.Groups["object"].Value, CultureInfo.InvariantCulture)] =
                    int.Parse(reference.Groups["map"].Value, CultureInfo.InvariantCulture);
            }
        }

        Dictionary<int, string> streams = ObjectStreams(document, raw);
        Dictionary<int, Dictionary<int, string>> maps = [];

        foreach ((int font, int map) in toUnicodeOf)
        {
            if (streams.TryGetValue(map, out string? content))
            {
                maps[font] = ParseCMap(content);
            }
        }

        return maps;
    }

    /// <summary>ToUnicode CMap'inin bfchar ve bfrange bloklarini okur.</summary>
    private static Dictionary<int, string> ParseCMap(string cmap)
    {
        Dictionary<int, string> map = [];

        foreach (Match block in BfChar().Matches(cmap))
        {
            foreach (Match pair in HexPair().Matches(block.Groups["body"].Value))
            {
                map[Code(pair.Groups["from"].Value)] = Characters(pair.Groups["to"].Value);
            }
        }

        foreach (Match block in BfRange().Matches(cmap))
        {
            foreach (Match triple in HexTriple().Matches(block.Groups["body"].Value))
            {
                int start = Code(triple.Groups["from"].Value);
                int end = Code(triple.Groups["to"].Value);
                int first = Code(triple.Groups["value"].Value);

                for (int offset = 0; offset <= end - start && offset < 65536; offset++)
                {
                    map[start + offset] = char.ConvertFromUtf32(first + offset);
                }
            }
        }

        return map;
    }

    private static int Code(string hex) =>
        int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    /// <summary>Onaltilik degeri UTF-16 karakterlere cevirir; vekil ciftleri korunuyor.</summary>
    private static string Characters(string hex)
    {
        StringBuilder value = new();

        for (int index = 0; index + 4 <= hex.Length; index += 4)
        {
            value.Append((char)int.Parse(
                hex.AsSpan(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        }

        return value.ToString();
    }

    /// <summary>Nesne numarasina gore acilmis akislar.</summary>
    private static Dictionary<int, string> ObjectStreams(byte[] document, string raw)
    {
        Dictionary<int, string> streams = [];

        foreach (Match match in ObjectStream().Matches(raw))
        {
            int number = int.Parse(match.Groups["object"].Value, CultureInfo.InvariantCulture);

            if (Inflate(document, match.Groups["data"].Index, match.Groups["data"].Length) is string content)
            {
                streams[number] = content;
            }
        }

        return streams;
    }

    /// <summary>Butun acilmis icerik akislari.</summary>
    private static IEnumerable<string> Streams(byte[] document, string raw)
    {
        foreach (Match match in ObjectStream().Matches(raw))
        {
            if (Inflate(document, match.Groups["data"].Index, match.Groups["data"].Length) is string content)
            {
                yield return content;
            }
        }
    }

    /// <summary>Flate ile sikistirilmis bolumu acar; acilamazsa null.</summary>
    private static string? Inflate(byte[] document, int offset, int length)
    {
        try
        {
            using MemoryStream input = new(document, offset, length);
            using ZLibStream inflate = new(input, CompressionMode.Decompress);
            using MemoryStream output = new();

            inflate.CopyTo(output);

            return Encoding.Latin1.GetString(output.ToArray());
        }
        catch (InvalidDataException)
        {
            // Sikistirilmamis ya da baska bir filtreyle kodlanmis akis; atlaniyor.
            return null;
        }
    }

    [GeneratedRegex(@"/Type\s*/Page(?![s])")]
    private static partial Regex PageObject();

    // "(?!endobj)" onemli: bu olmadan desen bir nesnenin basiyla bir sonrakinin akisini
    // birlestiriyor ve akislar yanlis nesne numarasina baglaniyordu. Belirtisi de sessizdi:
    // metin cikiyordu ama her harf yerine bilinmeyen karakter yaziliyordu.
    [GeneratedRegex(@"(?<object>\d+) 0 obj(?:(?!endobj).)*?stream\r?\n(?<data>.*?)\r?\nendstream", RegexOptions.Singleline)]
    private static partial Regex ObjectStream();

    [GeneratedRegex(@"/ToUnicode\s+(?<map>\d+) 0 R")]
    private static partial Regex ToUnicodeReferenceInternal();

    [GeneratedRegex(@"(?<object>\d+) 0 obj(?<body>.*?)endobj", RegexOptions.Singleline)]
    private static partial Regex ObjectBlock();

    [GeneratedRegex(@"\[(?<array>[^\]]*)\]\s*TJ")]
    private static partial Regex ShowArray();

    [GeneratedRegex(@"<(?<hex>[0-9A-Fa-f]+)>")]
    private static partial Regex HexString();

    [GeneratedRegex(@"/(?<name>F\d+)\s+(?<object>\d+) 0 R")]
    private static partial Regex FontResource();

    [GeneratedRegex(@"beginbfchar(?<body>.*?)endbfchar", RegexOptions.Singleline)]
    private static partial Regex BfChar();

    [GeneratedRegex(@"beginbfrange(?<body>.*?)endbfrange", RegexOptions.Singleline)]
    private static partial Regex BfRange();

    [GeneratedRegex(@"<(?<from>[0-9A-Fa-f]+)>\s*<(?<to>[0-9A-Fa-f]+)>")]
    private static partial Regex HexPair();

    [GeneratedRegex(@"<(?<from>[0-9A-Fa-f]+)>\s*<(?<to>[0-9A-Fa-f]+)>\s*<(?<value>[0-9A-Fa-f]+)>")]
    private static partial Regex HexTriple();

    [GeneratedRegex(@"/(?<font>F\d+)\s+[\d.]+\s+Tf|\[(?<array>[^\]]*)\]\s*TJ|<(?<hex>[0-9A-Fa-f]+)>\s*Tj|(?<newline>T\*|Td|TD|ET)")]
    private static partial Regex Token();

    private static Regex ToUnicodeReference() => ToUnicodeReferenceInternal();
}
