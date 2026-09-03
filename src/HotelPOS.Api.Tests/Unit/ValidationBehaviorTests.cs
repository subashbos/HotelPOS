using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentValidation;
using HotelPOS.Application.Interfaces;
using HotelPOS.Application.UseCases;
using HotelPOS.Application.UseCases.Purchases.Commands;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure;
using HotelPOS.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelPOS.Tests
{
    /// <summary>
    /// Regression tests for the root cause behind docs/QA_REVIEW_AND_TEST_GAPS.md item 8:
    /// ValidationBehavior declared `where TRequest : IRequest&lt;TResponse&gt;`, but MediatR 14's
    /// void `IRequest` commands do not implement `IRequest&lt;Unit&gt;` (only the non-generic
    /// `IRequest`/`IBaseRequest` markers). MediatR's own `IPipelineBehavior&lt;TRequest,TResponse&gt;`
    /// has no such constraint. That mismatch meant the .NET DI container could never construct
    /// `ValidationBehavior&lt;TRequest, Unit&gt;` for any void command - it silently resolved to zero
    /// pipeline behaviors instead of throwing, so FluentValidation validators never ran for any void
    /// command through the real mediator pipeline, in either the API or the desktop app.
    ///
    /// These tests exercise the mediator pipeline directly (bypassing every service-layer hand
    /// patch added as a workaround before the real root cause was found), configured exactly like
    /// API/Program.cs, so they fail if the constraint regresses.
    /// </summary>
    public class ValidationBehaviorTests
    {
        private static IMediator BuildMediator(IServiceScope scope) =>
            scope.ServiceProvider.GetRequiredService<IMediator>();

        private static async Task<IServiceScope> BuildScopeAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<HotelDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddScoped<IUserContext, FakeUserContext>();
            services.AddScoped<IAuthorizationService, AuthorizationService>();
            services.AddInfrastructure();

            // Mirrors API/Program.cs's MediatR configuration exactly.
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(SavePurchaseCommand).Assembly);
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(HotelPOS.Application.Common.Behaviors.ValidationBehavior<,>));
            });
            services.AddValidatorsFromAssembly(typeof(SavePurchaseCommandValidator).Assembly);

            var provider = services.BuildServiceProvider();
            var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            await db.Database.EnsureCreatedAsync();
            return scope;
        }

        [Fact]
        public void IPipelineBehavior_ClosesForVoidCommandAndUnit()
        {
            // The mechanism itself: MediatR's void-command dispatch path asks the container for
            // IPipelineBehavior<TRequest, Unit>. If ValidationBehavior can't close for that pair,
            // this returns zero results with no exception - the exact silent failure mode that hid
            // this bug for months.
            var services = new ServiceCollection();
            services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(HotelPOS.Application.Common.Behaviors.ValidationBehavior<,>));
            services.AddSingleton<IValidator<SavePurchaseCommand>>(new SavePurchaseCommandValidator());
            var provider = services.BuildServiceProvider();

            var behaviors = provider.GetServices<IPipelineBehavior<SavePurchaseCommand, MediatR.Unit>>();

            Assert.NotEmpty(behaviors);
        }

        [Fact]
        public async Task Mediator_Send_VoidCommand_InvalidData_ThrowsValidationException()
        {
            using var scope = await BuildScopeAsync();
            var mediator = BuildMediator(scope);

            var invalidPurchase = new Purchase
            {
                SupplierId = 0, // invalid: SavePurchaseCommandValidator requires > 0
                InvoiceNumber = "INV-1",
                PurchaseDate = DateTime.UtcNow,
                GrandTotal = 100m,
                PurchaseItems = new List<PurchaseItem>
                {
                    new PurchaseItem { ItemId = 1, Quantity = 1, UnitPrice = 100m }
                }
            };

            // Before the fix this reached SavePurchaseCommandHandler unvalidated instead of
            // throwing ValidationException.
            await Assert.ThrowsAsync<ValidationException>(
                () => mediator.Send(new SavePurchaseCommand(invalidPurchase)));
        }

        private class FakeUserContext : IUserContext
        {
            public bool IsAuthenticated => true;
            public int? CurrentUserId => 1;
            public string? CurrentUsername => "tester";
            public string? CurrentRole => "Admin";
            public IReadOnlyList<RolePermission>? Permissions => new List<RolePermission>
            {
                new RolePermission { ModuleName = "Purchase", CanAccess = true, CanEdit = true, CanDelete = true }
            };
        }
    }
}
