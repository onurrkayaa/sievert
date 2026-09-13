namespace Sievert.Contracts;

/// <summary>Sayfali bir liste. Sayfa boyutunun ust siniri API tarafinda; istemci onu asamaz.</summary>
public sealed record PagedResponse<T>(int Page, int PageSize, int TotalCount, IReadOnlyList<T> Items);
