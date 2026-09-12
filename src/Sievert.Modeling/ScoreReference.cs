using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Sievert.Modeling;

/// <summary>
/// Bir model profilinin EGITIM bolumundeki skor dagilimi. Goreli risk endeksi buradan
/// hesaplaniyor.
///
/// Endeks, skorun kendisinin yuz katı DEGIL: dagilimdaki yuzdelik sirasi. Esit skorlarda
/// orta sira (mid-rank) kullaniliyor, yani esit grubun yarisi altta sayiliyor. Boylece
/// endeks skor buyudukce hic dusmuyor ve esit skorlar ayni degeri aliyor.
///
/// Sozlesme: docs/urun/risk-sozlesmesi.md surum 1.0.
/// </summary>
public sealed class ScoreDistribution
{
    private readonly double[] sorted;

    private ScoreDistribution(string profileCode, string repositoryIdentity, double[] sorted)
    {
        ProfileCode = profileCode;
        RepositoryIdentity = repositoryIdentity;
        this.sorted = sorted;
    }

    public string ProfileCode { get; }

    public string RepositoryIdentity { get; }

    public IReadOnlyList<double> SortedScores => sorted;

    public int TrainCount => sorted.Length;

    public double Minimum => sorted[0];

    public double Maximum => sorted[^1];

    public static ScoreDistribution FromScores(
        string profileCode,
        string repositoryIdentity,
        IReadOnlyList<double> scores)
    {
        if (scores.Count == 0)
        {
            // Bos dagilimla yuzdelik hesaplanamaz. Sifira bolmek yerine burada duruyoruz.
            throw new ArgumentException(
                $"{profileCode}: egitim skor dagilimi bos, goreli endeks hesaplanamaz.",
                nameof(scores));
        }

        double[] sorted = [.. scores];
        Array.Sort(sorted);

        return new ScoreDistribution(profileCode, repositoryIdentity, sorted);
    }

    /// <summary>
    /// Skorun egitim dagilimindaki yuzdeligi, 0 ile 100 arasinda, bir ondalikli.
    /// </summary>
    public double RiskIndex(double score)
    {
        int less = LowerBound(score);
        int equal = UpperBound(score) - less;

        double percentile = (less + (0.5 * equal)) / sorted.Length;

        return Math.Round(percentile * 100.0, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>Skordan kucuk olan eleman sayisi.</summary>
    private int LowerBound(double score)
    {
        int low = 0;
        int high = sorted.Length;

        while (low < high)
        {
            int middle = low + ((high - low) / 2);

            if (sorted[middle] < score)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    /// <summary>Skordan kucuk ya da ona esit olan eleman sayisi.</summary>
    private int UpperBound(double score)
    {
        int low = 0;
        int high = sorted.Length;

        while (low < high)
        {
            int middle = low + ((high - low) / 2);

            if (sorted[middle] <= score)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }
}

/// <summary>
/// Uc profilin egitim skor dagilimi, dondurulmus bir dosyadan.
///
/// Dosya API tarafindan URETILMIYOR, okunuyor: dagilimi istek aninda hesaplamak, her
/// aciliste egitim verisini yeniden skorlamak demek olurdu ve hangi surumle uretildigi
/// izlenemezdi.
/// </summary>
public sealed class ScoreReference
{
    private readonly Dictionary<string, ScoreDistribution> distributions =
        new(StringComparer.OrdinalIgnoreCase);

    private ScoreReference()
    {
    }

    /// <summary>Dosyanin kendi ozeti; hangi referansin kullanildigi cevapta gorunsun diye.</summary>
    public string Checksum { get; private set; } = string.Empty;

    /// <summary>Dosyayi ureten kodun commit'i.</summary>
    public string CodeCommit { get; private set; } = string.Empty;

    public IReadOnlyList<ScoreDistribution> Distributions =>
        [.. distributions.Values.OrderBy(item => item.ProfileCode, StringComparer.Ordinal)];

    public static ScoreReference Load(string path)
    {
        ScoreReference reference = new()
        {
            Checksum = FileChecksum.Sha256(path),
        };

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;

        reference.CodeCommit = root.GetProperty("codeCommit").GetString()!;

        foreach (JsonElement entry in root.GetProperty("profiles").EnumerateArray())
        {
            string code = entry.GetProperty("profile").GetString()!;
            List<double> scores = [];

            foreach (JsonElement score in entry.GetProperty("trainScores").EnumerateArray())
            {
                scores.Add(score.GetDouble());
            }

            int declared = entry.GetProperty("trainCount").GetInt32();

            if (declared != scores.Count)
            {
                throw new InvalidDataException(
                    $"{code}: dosyada trainCount {declared} yaziyor ama {scores.Count} skor var.");
            }

            reference.distributions[code] = ScoreDistribution.FromScores(
                code,
                entry.GetProperty("repository").GetString()!,
                scores);
        }

        return reference;
    }

    public ScoreDistribution? For(string profileCode) => distributions.GetValueOrDefault(profileCode);

    /// <summary>
    /// Dosyanin metni. Yazan ve okuyan ayni yerde dursun diye burada; iki kosuda ayni
    /// baytlari uretmesi gerekiyor, o yuzden ondalik bicimi kulturden bagimsiz ve tam
    /// donus ("R") formatinda.
    /// </summary>
    public static string Render(
        string codeCommit,
        IReadOnlyDictionary<string, string> sources,
        IReadOnlyList<ScoreDistribution> distributions)
    {
        StringBuilder text = new();
        text.Append("{\n");
        text.Append("  \"codeCommit\": \"").Append(codeCommit).Append("\",\n");
        text.Append("  \"tiePolicy\": \"mid-rank\",\n");
        text.Append("  \"scoreDefinition\": \"egitim bolumunun ham model skorlari, artan sirada\",\n");
        text.Append("  \"sources\": {\n");

        int written = 0;

        foreach ((string key, string value) in sources.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            text.Append("    \"").Append(key).Append("\": \"").Append(value).Append('"');
            text.Append(++written == sources.Count ? "\n" : ",\n");
        }

        text.Append("  },\n");
        text.Append("  \"profiles\": [\n");

        for (int index = 0; index < distributions.Count; index++)
        {
            ScoreDistribution distribution = distributions[index];

            text.Append("    {\n");
            text.Append("      \"profile\": \"").Append(distribution.ProfileCode).Append("\",\n");
            text.Append("      \"repository\": \"").Append(distribution.RepositoryIdentity).Append("\",\n");
            text.Append("      \"trainCount\": ").Append(distribution.TrainCount).Append(",\n");
            text.Append("      \"minimum\": ").Append(Number(distribution.Minimum)).Append(",\n");
            text.Append("      \"maximum\": ").Append(Number(distribution.Maximum)).Append(",\n");
            text.Append("      \"trainScores\": [");

            for (int score = 0; score < distribution.TrainCount; score++)
            {
                text.Append(score == 0 ? "\n        " : ",\n        ");
                text.Append(Number(distribution.SortedScores[score]));
            }

            text.Append("\n      ]\n");
            text.Append(index == distributions.Count - 1 ? "    }\n" : "    },\n");
        }

        text.Append("  ]\n");
        text.Append("}\n");

        return text.ToString();
    }

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
