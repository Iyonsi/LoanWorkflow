using Microsoft.EntityFrameworkCore;

namespace LoanWorkflow.Api.Features.Common;

public static class PaginationExtensions
{
    public static async Task<(List<T> Items, PaginationMeta Meta)> ToPagedAsync<T>(this IQueryable<T> query, int page, int size, CancellationToken ct = default)
    {
        if(page < 1) page = 1;
        if(size < 1) size = 20;
        var count = await query.LongCountAsync(ct);
        var totalPages = (int)Math.Ceiling(count / (double)size);
        if(page > totalPages && totalPages > 0) page = totalPages;
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(ct);
        var meta = new PaginationMeta
        {
            PageNumber = page,
            PageSize = size,
            TotalCount = count,
            TotalPages = totalPages,
            HasNext = page < totalPages,
            HasPrevious = page > 1
        };
        return (items, meta);
    }
}
