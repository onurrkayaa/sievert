using System.Globalization;

namespace Sievert.Modeling;

/// <summary>
/// Dondurulmus veri kumesini okur. Sessizce toparlamiyor: baslik beklenenden farkliysa,
/// bir alan bossa, sayisal bir alan NaN ya da sonsuzsa okuma duruyor ve hangi satirin
/// hangi alani oldugunu soyluyor.
///
/// Sebebi Asama 4'ten geliyor: sessizce duzeltilen bir veri hatasi, sonradan modelin
/// hatasi gibi gorunuyor. Veri bozuksa model kurulmamali.
/// </summary>
public static class SnapshotReader
{
    /// <summary>Beklenen baslik. Sirasi da dahil sabit; anlik goruntu donduruldu.</summary>
    public static readonly string[] Columns =
    [
        "Repository", "RepositoryIdentity", "Sha", "AuthorDateUtc",
        "LinesAdded", "LinesDeleted", "FilesChanged", "CsFilesChanged", "Entropy",
        "DirectoryCount", "SubsystemCount", "MaxFileAgeDays", "MinFileAgeDays",
        "PriorChanges", "PriorFixes", "DistinctAuthorsOnFiles",
        "AuthorCommitCount", "AuthorFileExperience", "IsFix",
        "IsBugIntroducing", "LabelSource", "BotMu",
    ];

    /// <summary>
    /// Once dosyanin SHA-256'sini kayitli degerle karsilastirir, sonra okur. Model
    /// besleyen her yol bunu kullanmali: anlik goruntunun degismedigini gostermeden
    /// okunan bir dosya, dondurulmus veri kumesi sayilmaz.
    /// </summary>
    public static IReadOnlyList<SnapshotRow> ReadVerified(string path, string checksumPath)
    {
        FileChecksum.Verify(path, checksumPath);

        return Read(path);
    }

    public static IReadOnlyList<SnapshotRow> Read(string path)
    {
        using StreamReader reader = new(path);

        string? header = reader.ReadLine()
            ?? throw new InvalidDataException($"Anlik goruntu bos: {path}");

        CheckHeader(Split(header, 1, path), path);

        List<SnapshotRow> rows = [];
        int number = 1;

        while (reader.ReadLine() is string line)
        {
            number++;

            if (line.Length == 0)
            {
                continue;
            }

            rows.Add(ToRow(Split(line, number, path), number, path));
        }

        return rows;
    }

    private static void CheckHeader(string[] fields, string path)
    {
        if (fields.SequenceEqual(Columns, StringComparer.Ordinal))
        {
            return;
        }

        throw new InvalidDataException(
            $"Anlik goruntunun basligi beklenenden farkli: {path}{Environment.NewLine}"
            + $"  beklenen: {string.Join(',', Columns)}{Environment.NewLine}"
            + $"  okunan  : {string.Join(',', fields)}");
    }

    private static SnapshotRow ToRow(string[] fields, int number, string path)
    {
        if (fields.Length != Columns.Length)
        {
            throw new InvalidDataException(
                $"{path}:{number} satirinda {Columns.Length} alan olmali, {fields.Length} alan var.");
        }

        return new SnapshotRow(
            Text(fields, 0, number, path),
            Text(fields, 1, number, path),
            Text(fields, 2, number, path),
            Date(fields, 3, number, path),
            Whole(fields, 4, number, path),
            Whole(fields, 5, number, path),
            Whole(fields, 6, number, path),
            Whole(fields, 7, number, path),
            Real(fields, 8, number, path),
            Whole(fields, 9, number, path),
            Whole(fields, 10, number, path),
            Whole(fields, 11, number, path),
            Whole(fields, 12, number, path),
            Whole(fields, 13, number, path),
            Whole(fields, 14, number, path),
            Whole(fields, 15, number, path),
            Whole(fields, 16, number, path),
            Whole(fields, 17, number, path),
            Flag(fields, 18, number, path),
            Flag(fields, 19, number, path),
            fields[20].Length == 0 ? null : fields[20],
            Flag(fields, 21, number, path));
    }

    private static string Text(string[] fields, int index, int number, string path) =>
        fields[index].Length > 0
            ? fields[index]
            : throw Bad(index, number, path, "bos");

    private static DateTimeOffset Date(string[] fields, int index, int number, string path)
    {
        if (!fields[index].EndsWith('Z'))
        {
            throw Bad(index, number, path, $"UTC degil: '{fields[index]}'");
        }

        return DateTimeOffset.TryParse(
            fields[index],
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out DateTimeOffset value)
            ? value
            : throw Bad(index, number, path, $"tarih okunamadi: '{fields[index]}'");
    }

    private static int Whole(string[] fields, int index, int number, string path) =>
        int.TryParse(fields[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : throw Bad(index, number, path, Reason(fields[index]));

    private static double Real(string[] fields, int index, int number, string path)
    {
        if (!double.TryParse(fields[index], NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            || !double.IsFinite(value))
        {
            throw Bad(index, number, path, Reason(fields[index]));
        }

        return value;
    }

    private static bool Flag(string[] fields, int index, int number, string path) => fields[index] switch
    {
        "0" => false,
        "1" => true,
        _ => throw Bad(index, number, path, $"0 ya da 1 olmali, '{fields[index]}' var"),
    };

    private static string Reason(string field) => field.Length == 0
        ? "bos (NULL)"
        : $"sayi olarak okunamadi ya da sonlu degil: '{field}'";

    private static InvalidDataException Bad(int index, int number, string path, string reason) =>
        new($"{path}:{number} satirinda {Columns[index]} alani {reason}.");

    /// <summary>
    /// RFC 4180 tirnaklama: alan cift tirnakla sariliysa icindeki virgul ayrac degil,
    /// ikilenmis tirnak tek tirnak demek. Anlik goruntude su an oyle bir alan yok ama
    /// okuyucu bunu bilmek zorunda, cunku dosya elle degil kodla uretildi ve bir gun
    /// icinde virgul gecen bir depo adi cikabilir.
    /// </summary>
    private static string[] Split(string line, int number, string path)
    {
        List<string> fields = [];
        int index = 0;

        while (true)
        {
            if (index < line.Length && line[index] == '"')
            {
                index++;
                System.Text.StringBuilder field = new();

                while (true)
                {
                    if (index >= line.Length)
                    {
                        throw new InvalidDataException($"{path}:{number} satirinda kapanmamis tirnak var.");
                    }

                    if (line[index] == '"')
                    {
                        if (index + 1 < line.Length && line[index + 1] == '"')
                        {
                            field.Append('"');
                            index += 2;
                            continue;
                        }

                        index++;
                        break;
                    }

                    field.Append(line[index]);
                    index++;
                }

                fields.Add(field.ToString());
            }
            else
            {
                int comma = line.IndexOf(',', index);
                fields.Add(comma < 0 ? line[index..] : line[index..comma]);
                index = comma < 0 ? line.Length : comma;
            }

            if (index >= line.Length)
            {
                return [.. fields];
            }

            if (line[index] != ',')
            {
                throw new InvalidDataException($"{path}:{number} satirinda tirnaktan sonra ayrac beklendi.");
            }

            index++;

            if (index == line.Length)
            {
                fields.Add(string.Empty);

                return [.. fields];
            }
        }
    }
}
