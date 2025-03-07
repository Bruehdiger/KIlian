using KIlian.Generated.Rpc.Pagination;

namespace KIlian.Features.Rpc.Pagination.Extensions;

public static class PaginationExtensions
{
    public static int GetOffset(this OffsetBasedPagination pagination) => Math.Max(0, pagination.Offset);

    public static int GetCount(this OffsetBasedPagination pagination, int min = 1, int max = 100) => Math.Max(min, Math.Min(max, pagination.Count));
}