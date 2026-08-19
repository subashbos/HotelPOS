using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Infrastructure.Persistence;

namespace HotelPOS.TestCommon
{
    /// <summary>Composable seed helpers for tests built on SharedSqliteDatabaseFixture.</summary>
    public static class TestDataSeeder
    {
        // Nothing beyond HotelDbContext's own HasData baseline is needed yet; grow this as
        // fixtures shared across many tests are actually required.
        public static Task SeedBaselineAsync(HotelDbContext context) => Task.CompletedTask;

        public static async Task<Role> SeedRoleWithModuleAsync(HotelDbContext context, string roleName, string module, bool canAccess = true)
        {
            var role = new Role { Name = roleName, Description = "Test role" };
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            context.RolePermissions.Add(new RolePermission { RoleId = role.Id, ModuleName = module, CanAccess = canAccess });
            await context.SaveChangesAsync();
            return role;
        }

        public static async Task<User> SeedUserAsync(HotelDbContext context, string username, int? roleId = null, string role = RoleNames.Cashier)
        {
            var user = new User
            {
                Username = username,
                PasswordHash = "test-hash",
                Salt = "test-salt",
                Role = role,
                RoleId = roleId
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        public static async Task<Order> SeedOrderAsync(HotelDbContext context, string status, decimal totalAmount = 100m)
        {
            var order = new Order
            {
                TableNumber = 1,
                Status = status,
                PaymentMode = PaymentModes.Cash,
                TotalAmount = totalAmount,
                AmountPaid = totalAmount,
                CashPaid = totalAmount
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            return order;
        }
    }
}
