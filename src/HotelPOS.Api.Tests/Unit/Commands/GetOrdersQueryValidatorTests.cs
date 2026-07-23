using FluentAssertions;
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

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetOrdersQuery.PageSize));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    public void Validate_RejectsInvalidPageNumber(int pageNumber, int pageSize)
    {
        var query = new GetOrdersQuery(pageNumber, pageSize);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetOrdersQuery.PageNumber));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 10)]
    [InlineData(1, 100)]
    public void Validate_AcceptsInRangePageSize(int pageNumber, int pageSize)
    {
        var query = new GetOrdersQuery(pageNumber, pageSize);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
