using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using Sievert.Contracts;

namespace Sievert.Api.Reports;

/// <summary>
/// Rapor modelini PDF'e ceviren belge.
///
/// Yalniz <see cref="ReportModel"/> okuyor: burada hicbir sorgu, hicbir hesap yok. Sebep
/// dogrulanabilirlik - PDF'te gorunen her sayi, once ara modelde duran bir sayi olmak
/// zorunda, yoksa bagimsiz dogrulama karsilastiracak bir sey bulamaz.
/// </summary>
public sealed class ReportDocument(ReportModel model, ReportText text) : IDocument
{
    private const string Ink = "#1a1a1a";

    private const string Muted = "#5a5a5a";

    private const string Rule = "#c8c8c8";

    private const string Warning = "#8a4b00";

    private const string WarningBackground = "#fff3e0";

    public DocumentMetadata GetMetadata() => new()
    {
        Title = model.Cover.Title ?? $"{text.Product} - {text.DocumentTitle}",
        Author = text.Product,
        Subject = $"{text.DocumentTitle}: {model.Cover.RepositoryDisplayName}",
        Creator = ReportManifest.GeneratorVersion,
        Producer = ReportManifest.GeneratorVersion,
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(18, Unit.Millimetre);
            page.DefaultTextStyle(style => style.FontSize(9).FontColor(Ink).FontFamily("Lato"));

            page.Header().Element(Header);
            page.Content().Element(Body);
            page.Footer().Element(Footer);
        });
    }

    private void Header(IContainer container) =>
        container.PaddingBottom(8).BorderBottom(1).BorderColor(Rule).Row(row =>
        {
            row.RelativeItem().Text($"{text.Product} · {text.DocumentTitle}").SemiBold().FontSize(9);

            row.ConstantItem(220).AlignRight().Text(model.Cover.RepositoryDisplayName)
                .FontColor(Muted).FontSize(9);
        });

    private void Footer(IContainer container) =>
        container.PaddingTop(6).BorderTop(1).BorderColor(Rule).Row(row =>
        {
            row.RelativeItem().Text(text.NotAVerdict).FontSize(8).FontColor(Muted);

            row.ConstantItem(120).AlignRight().Text(span =>
            {
                span.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
                span.Span(text.Page + " ");
                span.CurrentPageNumber();
                span.Span(" " + text.Of + " ");
                span.TotalPages();
            });
        });

    private void Body(IContainer container) =>
        container.PaddingVertical(10).Column(column =>
        {
            column.Spacing(14);

            column.Item().Element(Cover);
            column.Item().Element(Summary);
            column.Item().Element(Contract);
            column.Item().Element(Timeline);
            column.Item().Element(Files);
            column.Item().Element(Commits);
            column.Item().Element(Static);
            column.Item().Element(Explanations);
            column.Item().Element(Limitations);
            column.Item().Element(Provenance);
        });

    // 1 - Kapak

    private void Cover(IContainer container) =>
        container.Column(column =>
        {
            column.Spacing(8);

            column.Item().Text(text.DocumentTitle).FontSize(20).SemiBold();
            column.Item().Text(model.Cover.RepositoryDisplayName).FontSize(13).FontColor(Muted);

            if (model.Cover.Title is string title)
            {
                column.Item().Text(title).FontSize(11).SemiBold();
            }

            if (model.Cover.IsPartial)
            {
                column.Item().Element(PartialBanner);
            }

            column.Item().Text(text.CoverIntro).FontSize(9).FontColor(Muted);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(150);
                    columns.RelativeColumn();
                });

                Pair(table, text.RepositoryIdentity, model.Cover.RepositoryIdentity);
                Pair(table, text.ReportId, model.Cover.ReportId.ToString());
                Pair(table, text.GeneratedAt, text.Moment(model.Cover.GeneratedAtUtc));
                Pair(table, text.RiskJob, model.Cover.RiskJobId.ToString());
                Pair(table, text.StaticJob, model.Cover.StaticJobId?.ToString() ?? text.NotIncluded);
                Pair(table, text.ModelProfile, model.Cover.ModelProfile);
                Pair(table, text.ModelChecksum, model.Cover.ModelShortChecksum);
                Pair(table, text.Calibration, model.Cover.IsCalibrated ? "-" : text.NotCalibrated);
            });

            // Uc zorunlu cumle ilk sayfada. Arkaya atilirsa ilk sayfayi okuyup birakan
            // biri skoru olasilik saniyor.
            column.Item().PaddingTop(4).Column(lines =>
            {
                lines.Spacing(3);

                foreach (string line in text.CoverContract)
                {
                    lines.Item().Row(row =>
                    {
                        row.ConstantItem(10).Text("•").FontColor(Muted);
                        row.RelativeItem().Text(line).FontSize(9);
                    });
                }
            });

            column.Item().PaddingTop(4).Background("#f4f4f4").Padding(8)
                .Text(text.NotAVerdict).SemiBold().FontSize(10);

            if (model.Cover.Notes is string notes)
            {
                column.Item().PaddingTop(4).Column(note =>
                {
                    note.Item().Text(text.Notes).SemiBold().FontSize(9);
                    note.Item().Text(notes).FontSize(9).FontColor(Muted);
                });
            }
        });

    private void PartialBanner(IContainer container) =>
        container.Background(WarningBackground).BorderLeft(3).BorderColor(Warning).Padding(8)
            .Text(text.PartialBanner).SemiBold().FontSize(11).FontColor(Warning);

    // 2 - Yonetici ozeti

    private void Summary(IContainer container) =>
        container.Column(column =>
        {
            column.Spacing(6);

            Heading(column, text.SummaryHeading);

            if (model.Cover.IsPartial)
            {
                column.Item().Text(text.PartialShort).FontSize(9).FontColor(Warning).SemiBold();
            }

            column.Item().Text(text.ScopeNote).FontSize(8).FontColor(Muted);

            ReportSummary summary = model.Summary;

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(190);
                    columns.RelativeColumn();
                });

                Pair(table, text.CoveredCommits, text.Number(summary.CoveredCommitCount));
                Pair(table, text.SelectedWindow, text.Number(summary.CommitWindow));
                Pair(table, text.DateRange, summary.FirstCommitUtc is null || summary.LastCommitUtc is null
                    ? "-"
                    : $"{text.Day(summary.FirstCommitUtc.Value)} - {text.Day(summary.LastCommitUtc.Value)}");
                Pair(table, text.MeanIndex, text.Number(summary.MeanRiskIndex, 1));
                Pair(table, text.MedianIndex, text.Number(summary.MedianRiskIndex, 1));
                Pair(table, text.MaximumIndex, text.Number(summary.MaximumRiskIndex, 1));
                Pair(table, text.DecisionAt05, text.Number(summary.DecisionAt05Count));
                Pair(table, text.DecisionAtTrain,
                    $"{text.Number(summary.DecisionAtTrainCount)} ({text.Number(summary.TrainThreshold, 4)})");
                Pair(table, text.StaticFindingCount, summary.StaticFindingCount is int count
                    ? text.Number(count)
                    : text.NotIncluded);
            });

            column.Item().PaddingTop(4).Text(span =>
            {
                span.Span(text.MostTouched).SemiBold().FontSize(9);

                if (model.Cover.IsPartial)
                {
                    span.Span(" " + text.WithinWrittenResults).FontSize(8).FontColor(Warning);
                }
            });

            if (summary.MostTouchedFiles.Count == 0)
            {
                column.Item().Text(text.NoData).FontSize(9).FontColor(Muted);
            }
            else
            {
                foreach (ReportTouchedFile file in summary.MostTouchedFiles)
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text(file.RelativePath).FontSize(8);
                        row.ConstantItem(70).AlignRight().Text(
                            $"{text.Number(file.TouchCount)} {text.TouchCount.ToLowerInvariant()}").FontSize(8);
                        row.ConstantItem(60).AlignRight().Text(text.Number(file.MeanRiskIndex, 1)).FontSize(8);
                    });
                }
            }

            column.Item().PaddingTop(4).Text(span =>
            {
                span.Span(text.HighestCommits).SemiBold().FontSize(9);

                if (model.Cover.IsPartial)
                {
                    span.Span(" " + text.WithinWrittenResults).FontSize(8).FontColor(Warning);
                }
            });

            foreach (ReportCommitRow commit in summary.HighestCommits)
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(90).Text(commit.ShortSha).FontSize(8).FontFamily(Fonts.Consolas);
                    row.RelativeItem().Text(commit.MessageSubject).FontSize(8);
                    row.ConstantItem(50).AlignRight().Text(text.Number(commit.RiskIndex, 1)).FontSize(8);
                });
            }
        });

    // 3 - Risk sozlesmesi

    private void Contract(IContainer container) =>
        container.Column(column =>
        {
            column.Spacing(4);

            Heading(column, text.ContractHeading);

            foreach (string line in text.ContractLines)
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(10).Text("•").FontColor(Muted);
                    row.RelativeItem().Text(line).FontSize(9);
                });
            }
        });

    // 4 - Zaman cizelgesi

    private void Timeline(IContainer container) =>
        container.Column(column =>
        {
            column.Spacing(6);

            Heading(column, text.TimelineHeading);

            if (model.Cover.IsPartial)
            {
                column.Item().Text(text.PartialShort).FontSize(8).FontColor(Warning).SemiBold();
            }

            column.Item().Text(text.TimelineNote).FontSize(8).FontColor(Muted);

            if (model.Timeline.Count == 0)
            {
                column.Item().Text(text.NoData).FontSize(9).FontColor(Muted);

                return;
            }

            column.Item().Svg(ReportChart.Timeline(
                model.Timeline, model.ThresholdIndexAt05, model.ThresholdIndexAtTrain, text));

            column.Item().Text(
                $"{text.Number(model.Timeline.Count)} commit · "
                + $"{text.ThresholdAt05} {text.Number(model.ThresholdIndexAt05, 1)} · "
                + $"{text.ThresholdAtTrain} {text.Number(model.ThresholdIndexAtTrain, 1)}")
                .FontSize(8).FontColor(Muted);
        });

    // 5 - Dosya etkinligi

    private void Files(IContainer container) =>
        container.Column(column =>
        {
            column.Spacing(6);

            Heading(column, text.FilesHeading);

            if (model.Cover.IsPartial)
            {
                column.Item().Text(text.PartialShort).FontSize(8).FontColor(Warning).SemiBold();
            }

            column.Item().Text(text.FilesNote).FontSize(8).FontColor(Muted);

            column.Item().Text(
                $"{text.SortedBy}: {model.FileSort} · "
                + $"{text.Number(model.Files.Count)} / {text.Number(model.FileCountBeforeLimit)} {text.ShownOf}")
                .FontSize(8).FontColor(Muted);

            if (model.Files.Count == 0)
            {
                column.Item().Text(text.NoData).FontSize(9).FontColor(Muted);

                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.ConstantColumn(42);
                    columns.ConstantColumn(52);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(42);
                });

                table.Header(header =>
                {
                    HeaderCell(header, text.Path);
                    HeaderCell(header, text.TouchCount, right: true);
                    HeaderCell(header, text.Churn, right: true);
                    HeaderCell(header, text.Mean, right: true);
                    HeaderCell(header, text.Maximum, right: true);
                    HeaderCell(header, text.Latest, right: true);
                    HeaderCell(header, text.Findings, right: true);
                });

                foreach (FileActivityItem file in model.Files)
                {
                    Cell(table, file.RelativePath, mono: true);
                    Cell(table, text.Number(file.TouchCount), right: true);
                    Cell(table, text.Number(file.TotalChurn), right: true);
                    Cell(table, text.Number(file.MeanRiskIndex, 1), right: true);
                    Cell(table, text.Number(file.MaxRiskIndex, 1), right: true);
                    Cell(table, text.Number(file.LatestRiskIndex, 1), right: true);
                    Cell(table, file.StaticFindingCount is int count ? text.Number(count) : "-", right: true);
                }
            });

            column.Item().Text(text.StaticSeparate).FontSize(8).FontColor(Muted);
        });

    // 6 - Commit listesi

    private void Commits(IContainer container) =>
        container.Column(column =>
        {
            column.Spacing(6);

            Heading(column, text.CommitsHeading);

            if (model.Cover.IsPartial)
            {
                column.Item().Text(text.PartialShort).FontSize(8).FontColor(Warning).SemiBold();
            }

            column.Item().Text(text.CommitsNote).FontSize(8).FontColor(Muted);

            if (model.TopCommits.Count == 0)
            {
                column.Item().Text(text.NoData).FontSize(9).FontColor(Muted);

                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(64);
                    columns.ConstantColumn(58);
                    columns.RelativeColumn(4);
                    columns.ConstantColumn(40);
                    columns.ConstantColumn(48);
                    columns.ConstantColumn(30);
                    columns.ConstantColumn(30);
                    columns.ConstantColumn(44);
                    columns.ConstantColumn(34);
                });

                table.Header(header =>
                {
                    HeaderCell(header, text.ShortSha);
                    HeaderCell(header, text.Date);
                    HeaderCell(header, text.Subject);
                    HeaderCell(header, text.Index, right: true);
                    HeaderCell(header, text.RawScore, right: true);
                    HeaderCell(header, text.Decision05, right: true);
                    HeaderCell(header, text.DecisionTrain, right: true);
                    HeaderCell(header, text.Churn, right: true);
                    HeaderCell(header, text.CsFiles, right: true);
                });

                foreach (ReportCommitRow commit in model.TopCommits)
                {
                    Cell(table, commit.ShortSha, mono: true);
                    Cell(table, commit.AuthorDateUtc.UtcDateTime.ToString("yyyy-MM-dd"));
                    Cell(table, commit.MessageSubject);
                    Cell(table, text.Number(commit.RiskIndex, 1), right: true);
                    Cell(table, text.Number(commit.RawModelScore, 4), right: true);
                    Cell(table, commit.DecisionAt05 ? text.Yes : text.No, right: true);
                    Cell(table, commit.DecisionAtTrainThreshold ? text.Yes : text.No, right: true);
                    Cell(table, text.Number(commit.Churn), right: true);
                    Cell(table, text.Number(commit.CsFilesChanged), right: true);
                }
            });

            // Uyari kodlari tabloda bir sutun kaplamiyor: cogu commit'te ayni uc kod var
            // ve tablo okunmaz hale geliyordu. Farkli olanlar altta toplaniyor.
            string[] extra = [.. model.TopCommits
                .SelectMany(commit => commit.WarningCodes)
                .Where(code => code != RiskWarning.UncalibratedScore
                    && code != RiskWarning.SzzTarget
                    && code != RiskWarning.StaticAnalysisNotIncluded)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(code => code, StringComparer.Ordinal)];

            if (extra.Length > 0)
            {
                column.Item().Text($"{text.Warnings}: {string.Join(", ", extra)}")
                    .FontSize(8).FontColor(Muted);
            }
        });

    // 7 - Statik bulgular

    private void Static(IContainer container) =>
        container.Column(column =>
        {
            column.Spacing(6);

            Heading(column, text.StaticHeading);

            if (!model.Static.Included)
            {
                column.Item().Text(text.StaticNotRun).FontSize(9);

                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(160);
                    columns.RelativeColumn();
                });

                Pair(table, text.StaticJob, model.Static.JobId?.ToString() ?? "-");
                Pair(table, text.SourceHead, model.Static.SourceHeadSha ?? "-");
                Pair(table, text.TreeState, model.Static.SourceTreeState ?? "-");
                Pair(table, text.Verified, model.Static.SourceVerified ? text.Yes : text.No);
                Pair(table, text.Findings, text.Number(model.Static.FindingCount));
                Pair(table, text.Suppressed, text.Number(model.Static.SuppressedCount));
                Pair(table, text.Exemptions, text.Number(model.Static.ExemptionCount));
            });

            if (model.Static.RuleCounts.Count > 0)
            {
                column.Item().Text(text.RuleDistribution).SemiBold().FontSize(9);

                column.Item().Text(string.Join(" · ", model.Static.RuleCounts
                    .Select(rule => $"{rule.RuleCode} {text.Number(rule.Count)}")))
                    .FontSize(8);

                column.Item().Text(string.Join(" · ", model.Static.SeverityCounts
                    .Select(severity => $"{severity.Severity} {text.Number(severity.Count)}")))
                    .FontSize(8).FontColor(Muted);
            }

            if (model.Static.Findings.Count > 0)
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(44);
                        columns.ConstantColumn(52);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(34);
                        columns.RelativeColumn(4);
                    });

                    table.Header(header =>
                    {
                        HeaderCell(header, text.Rule);
                        HeaderCell(header, text.Severity);
                        HeaderCell(header, text.Path);
                        HeaderCell(header, text.Line, right: true);
                        HeaderCell(header, text.Message);
                    });

                    foreach (StaticFindingResponse finding in model.Static.Findings)
                    {
                        Cell(table, finding.RuleCode, mono: true);
                        Cell(table, finding.Severity);
                        Cell(table, finding.RelativePath, mono: true);
                        Cell(table, text.Number(finding.Line), right: true);
                        Cell(table, finding.Message);
                    }
                });
            }

            column.Item().Background("#f4f4f4").Padding(6)
                .Text(text.StaticSeparate).SemiBold().FontSize(9);
        });

    // 8 - Model aciklamasi

    private void Explanations(IContainer container) =>
        container.Column(column =>
        {
            column.Spacing(6);

            Heading(column, text.ExplanationHeading);

            column.Item().Text(text.ExplanationNote).FontSize(8).FontColor(Muted);

            if (model.Explanations.Count == 0)
            {
                column.Item().Text(text.NoData).FontSize(9).FontColor(Muted);

                return;
            }

            foreach (ReportExplanation explanation in model.Explanations)
            {
                column.Item().PaddingTop(4).Column(block =>
                {
                    block.Spacing(3);

                    block.Item().Text(span =>
                    {
                        span.Span(explanation.ShortSha).SemiBold().FontSize(9).FontFamily(Fonts.Consolas);
                        span.Span($"  {text.Index} {text.Number(explanation.RiskIndex, 1)}").FontSize(9);
                        span.Span($"  {text.RawScore} {text.Number(explanation.RawModelScore, 4)}").FontSize(9);
                        span.Span($"  {text.ExplanationVerified}: "
                            + (explanation.ExplanationVerified ? text.Yes : text.No)).FontSize(8).FontColor(Muted);
                    });

                    block.Item().Row(row =>
                    {
                        row.RelativeItem().Element(side => Contributions(
                            side, text.PositiveContributions, explanation.Positive));

                        row.ConstantItem(10);

                        row.RelativeItem().Element(side => Contributions(
                            side, text.NegativeContributions, explanation.Negative));
                    });
                });
            }
        });

    private void Contributions(
        IContainer container, string heading, IReadOnlyList<ReportContribution> contributions) =>
        container.Column(column =>
        {
            column.Item().Text(heading).SemiBold().FontSize(8);

            if (contributions.Count == 0)
            {
                column.Item().Text("-").FontSize(8).FontColor(Muted);

                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(46);
                    columns.ConstantColumn(46);
                    columns.ConstantColumn(46);
                });

                table.Header(header =>
                {
                    HeaderCell(header, text.Feature);
                    HeaderCell(header, text.Value, right: true);
                    HeaderCell(header, text.Coefficient, right: true);
                    HeaderCell(header, text.Contribution, right: true);
                });

                foreach (ReportContribution contribution in contributions)
                {
                    Cell(table, contribution.Feature);
                    Cell(table, text.Number(contribution.Value, 2), right: true);
                    Cell(table, text.Number(contribution.Coefficient, 4), right: true);
                    Cell(table, text.Number(contribution.Contribution, 4), right: true);
                }
            });
        });

    // 9 - Sinirliliklar

    private void Limitations(IContainer container) =>
        container.Column(column =>
        {
            column.Spacing(4);

            Heading(column, text.LimitationsHeading);

            foreach (string code in model.Limitations)
            {
                column.Item().Row(row =>
                {
                    row.ConstantItem(10).Text("•").FontColor(Muted);
                    row.RelativeItem().Text(text.Limitation(code)).FontSize(9);
                });
            }
        });

    // 10 - Kaynak ve butunluk

    private void Provenance(IContainer container) =>
        container.Column(column =>
        {
            column.Spacing(6);

            Heading(column, text.ProvenanceHeading);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(170);
                    columns.RelativeColumn();
                });

                Pair(table, text.ManifestChecksum, model.Provenance.ManifestSha256, mono: true);
                Pair(table, text.ModelChecksum, model.Provenance.ModelChecksum, mono: true);
                Pair(table, text.ScoreReferenceChecksum, model.Provenance.ScoreReferenceChecksum, mono: true);
                Pair(table, text.ModelResultsChecksum, model.Provenance.ModelResultsChecksum, mono: true);
                Pair(table, text.RiskJob, model.Provenance.RiskJobId.ToString());
                Pair(table, text.StaticJob, model.Provenance.StaticJobId?.ToString() ?? text.NotIncluded);
                Pair(table, text.SchemaVersion, model.Provenance.SchemaVersion);
                Pair(table, text.GeneratorVersion, model.Provenance.GeneratorVersion);
            });

            column.Item().Text(text.PdfChecksumNote).FontSize(8).FontColor(Muted);
        });

    // Ortak parcalar

    private static void Heading(ColumnDescriptor column, string title) =>
        column.Item().BorderBottom(1).BorderColor(Ink).PaddingBottom(3)
            .Text(title).FontSize(12).SemiBold();

    private static void Pair(TableDescriptor table, string label, string value, bool mono = false)
    {
        table.Cell().PaddingVertical(1).Text(label).FontSize(9).FontColor(Muted);

        TextSpanDescriptor cell = table.Cell().PaddingVertical(1).Text(value).FontSize(9);

        if (mono)
        {
            cell.FontFamily(Fonts.Consolas).FontSize(8);
        }
    }

    /// <summary>
    /// Tablo basligi. <c>table.Header</c> kullaniliyor cunku tablo sayfa asinca baslik
    /// **her sayfada** tekrar etmeli; ilk surumde etmiyordu ve ikinci sayfadaki satirlar
    /// hangi sutunun ne oldugu belli olmadan duruyordu.
    /// </summary>
    private static void HeaderCell(TableCellDescriptor header, string title, bool right = false)
    {
        IContainer cell = header.Cell().BorderBottom(1).BorderColor(Ink).PaddingVertical(2).PaddingRight(3);

        (right ? cell.AlignRight() : cell).Text(title).FontSize(8).SemiBold();
    }

    /// <summary>
    /// Icerik hucresi. <c>ShowEntire</c> ile bir satirin icerigi sayfa sonunda ikiye
    /// bolunmuyor: uzun bir dosya yolu ilk surumde sayfa sinirinda ortasindan kesilmisti.
    /// </summary>
    private static void Cell(TableDescriptor table, string value, bool right = false, bool mono = false)
    {
        IContainer cell = table.Cell().ShowEntire()
            .BorderBottom(1).BorderColor(Rule).PaddingVertical(2).PaddingRight(3);

        TextSpanDescriptor span = (right ? cell.AlignRight() : cell).Text(value).FontSize(8);

        if (mono)
        {
            span.FontFamily(Fonts.Consolas).FontSize(7.5f);
        }
    }
}
