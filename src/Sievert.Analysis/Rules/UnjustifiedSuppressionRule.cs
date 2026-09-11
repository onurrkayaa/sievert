using Sievert.Core.Rules;

namespace Sievert.Analysis.Rules;

/// <summary>SV007 - gerekcesi yazilmamis susturma yorumlarini bulur.</summary>
public sealed class UnjustifiedSuppressionRule : IRule
{
    public string Code => "SV007";

    public string Name => "gerekcesiz susturma";

    public string Description =>
        "Bir bulguyu susturmak bir karardir ve kararin sebebi kodda yazili olmali; "
        + "gerekcesiz susturma, bulgunun neden gormezden gelindigini sonradan "
        + "kimsenin bilememesi demek.";

    public RuleResult Inspect(RuleContext context)
    {
        List<Finding> findings = [];

        foreach (SuppressionComment comment in SuppressionReader.Read(context.File.Tree))
        {
            if (comment.HasReason)
            {
                continue;
            }

            findings.Add(new Finding(
                Code,
                Name,
                $"// sievert:disable {comment.RuleCode} yazilmis ama gerekce yok. "
                + "Gerekce yazilmadigi icin bu susturma gecersiz, bulgu elenmiyor.",
                Description,
                context.File.RelativePath,
                comment.CommentLine,
                comment.MethodName,
                Severity.Warning));
        }

        return findings.Count == 0 ? RuleResult.Empty : new RuleResult(findings, []);
    }
}
