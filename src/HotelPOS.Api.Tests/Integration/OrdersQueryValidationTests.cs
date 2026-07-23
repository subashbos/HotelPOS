using FluentAssertions;
using FluentValidation;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Orders.Queries;
using HotelPOS.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace HotelPOS.Tests
{
    public class OrdersQueryValidationTests
    {
        private static IMediator BuildMediator(string dbName)
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddDbContext<HotelDbContext>(options => options.UseInMemoryDatabase(dbName));
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IItemService>(_ => new Mock<IItemService>().Object);
            services.AddScoped<IOrderService, OrderService>();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(GetOrdersQuery).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(HotelPOS.Application.Common.Behaviors.ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(typeof(GetOrdersQueryValidator).Assembly);

            return services.BuildServiceProvider().GetRequiredService<IMediator>();
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(1, -1)]
        [InlineData(1, 100000)]
        [InlineData(0, 10)]
        public async Task GetPagedOrders_WithInvalidPaging_ThrowsValidationException(int pageNumber, int pageSize)
        {
            var mediator = BuildMediator($"OrdersQueryValidationDb_{pageNumber}_{pageSize}");
            var query = new GetOrdersQuery(pageNumber, pageSize);

            Func<Task> act = async () => await mediator.Send(query);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task GetPagedOrders_WithValidPaging_ReturnsEmptyResult()
        {
            var mediator = BuildMediator("OrdersQueryValidationDb_valid");
            var query = new GetOrdersQuery(1, 10);

            var (items, totalCount) = await mediator.Send(query);

            items.Should().BeEmpty();
            totalCount.Should().Be(0);
        }
    }
}
