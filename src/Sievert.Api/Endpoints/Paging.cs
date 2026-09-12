using Microsoft.AspNetCore.Http;

namespace Sievert.Api.Endpoints;

/// <summary>Sayfalama parametreleri. Gecersiz deger sessizce duzeltilmiyor, hata donuyor.</summary>
public readonly record struct Paging(int Page, int PageSize)
{
    public int Skip => (Page - 1) * PageSize;

    public static bool TryParse(HttpContext context, ApiOptions options, out Paging paging, out string? error)
    {
        paging = default;
        error = null;

        int page = 1;
        int size = options.DefaultPageSize;

        if (context.Request.Query["page"].FirstOrDefault() is string rawPage && rawPage.Length > 0
            && (!int.TryParse(rawPage, out page) || page < 1))
        {
            error = "page 1 ya da daha buyuk bir tam sayi olmali.";

            return false;
        }

        if (context.Request.Query["pageSize"].FirstOrDefault() is string rawSize && rawSize.Length > 0
            && (!int.TryParse(rawSize, out size) || size < 1 || size > options.MaximumPageSize))
        {
            error = $"pageSize 1 ile {options.MaximumPageSize} arasinda olmali.";

            return false;
        }

        paging = new Paging(page, size);

        return true;
    }
}
