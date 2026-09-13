using System.Text.Json;

using Sievert.Contracts;

namespace Sievert.Api.Visualizations;

/// <summary>
/// Pencere satirlarindan risk zaman cizelgesini kurar.
///
/// Secim ve gosterim sirasi ayni degil: secim "en yeni N", gosterim kronolojik. Ikisini
/// karistirmak, "son 100 commit" isteyip en eski 100'u gostermek olurdu.
/// </summary>
public static class TimelineBuilder
{
    public static List<RiskTimelinePoint> Build(IReadOnlyList<WindowCommit> commits)
    {
        List<RiskTimelinePoint> points = [];

        for (int index = 0; index < commits.Count; index++)
        {
            WindowCommit commit = commits[index];

            points.Add(new RiskTimelinePoint(
                index,
                commit.Sha,
                commit.ShortSha,
                commit.AuthorDateUtc,
                commit.MessageSubject,
                commit.RawModelScore,
                commit.RiskIndex,
                commit.DecisionAt05,
                commit.DecisionAtTrainThreshold,
                commit.TrainThreshold,
                commit.IsFix,
                commit.IsBugIntroducing,
                commit.LinesAdded,
                commit.LinesDeleted,
                commit.FilesChanged,
                commit.CsFilesChanged,
                commit.IsBot,
                Warnings(commit.WarningCodes)));
        }

        return points;
    }

    /// <summary>
    /// Uyari kodlari veritabaninda JSON metin olarak duruyor. Bozuk bir metin butun
    /// cizelgeyi dusurmemeli; o satir uyarisiz gorunur.
    /// </summary>
    private static IReadOnlyList<string> Warnings(string raw)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
