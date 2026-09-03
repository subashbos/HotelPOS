using HotelPOS.Application.UseCases.Orders.Queries;
using Xunit;

namespace HotelPOS.Tests;

public class GetOrdersQueryValidatorTests
{
    private readonly GetOrdersQueryValidator _validator = new();

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 100000)]
    [InlineData(1, 101)]
    public void Validate_RejectsOutOfRangePageSize(int pageNumber, int pageSize)
    {
        var query = new GetOrdersQuery(pageNumber, pageSize);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetOrdersQuery.PageSize));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    public void Validate_RejectsInvalidPageNumber(int pageNumber, int pageSize)
    {
        var query = new GetOrdersQuery(pageNumber, pageSize);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetOrdersQuery.PageNumber));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 10)]
    [InlineData(1, 100)]
    public void Validate_AcceptsInRangePageSize(int pageNumber, int pageSize)
    {
        var query = new GetOrdersQuery(pageNumber, pageSize);

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }
}
