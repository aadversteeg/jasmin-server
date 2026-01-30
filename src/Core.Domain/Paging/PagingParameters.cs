using Ave.Extensions.Functional;
using Core.Domain.Models;

namespace Core.Domain.Paging;

public record PagingParameters
{
    public int Page { get; }
    public int PageSize { get; }

    private PagingParameters(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public static Result<PagingParameters, Error> Create(int page, int pageSize)
    {
        if (page < 1)
            return Result<PagingParameters, Error>.Failure(Errors.InvalidPage());
        if (pageSize < 1 || pageSize > 100)
            return Result<PagingParameters, Error>.Failure(Errors.InvalidPageSize());
        return Result<PagingParameters, Error>.Success(new PagingParameters(page, pageSize));
    }

    public int Skip => (Page - 1) * PageSize;
}
