using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HotelPOS.Infrastructure.Persistence
{
    public class HeldOrderRepository : IHeldOrderRepository
    {
        private readonly HotelDbContext _context;

        public HeldOrderRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task SaveAsync(HeldOrder held)
        {
            var serializedItems = JsonSerializer.Serialize(held.Items);
            var isSqlite = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
            var exists = false;

            if (isSqlite)
            {
                var count = _context.Database.SqlQueryRaw<int>("SELECT COUNT(1) FROM HeldOrders WHERE Id = {0}", held.Id.ToString()).FirstOrDefault();
                exists = count > 0;
            }
            else
            {
                var count = _context.Database.SqlQueryRaw<int>("SELECT COUNT(1) FROM HeldOrders WHERE Id = {0}", held.Id).FirstOrDefault();
                exists = count > 0;
            }

            if (exists)
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE HeldOrders SET HoldName = {0}, HeldAt = {1}, TableNumber = {2}, SerializedItems = {3} WHERE Id = {4}",
                    held.HoldName, held.HeldAt, held.TableNumber, serializedItems, isSqlite ? held.Id.ToString() : held.Id);
            }
            else
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO HeldOrders (Id, HoldName, HeldAt, TableNumber, SerializedItems) VALUES ({0}, {1}, {2}, {3}, {4})",
                    isSqlite ? held.Id.ToString() : held.Id, held.HoldName, held.HeldAt, held.TableNumber, serializedItems);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var isSqlite = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM HeldOrders WHERE Id = {0}", isSqlite ? id.ToString() : id);
        }

        public async Task ClearAllAsync()
        {
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM HeldOrders");
        }

        public async Task<List<HeldOrder>> GetAllAsync()
        {
            var conn = _context.Database.GetDbConnection();
            var alreadyOpen = conn.State == System.Data.ConnectionState.Open;
            if (!alreadyOpen) await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, HoldName, HeldAt, TableNumber, SerializedItems FROM HeldOrders";

            var restored = new List<HeldOrder>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (reader.Read())
            {
                var idVal = reader.GetValue(0);
                var id = idVal is Guid g ? g : Guid.Parse(idVal.ToString()!);
                var holdName = reader.GetString(1);
                var heldAtVal = reader.GetValue(2);
                var heldAt = heldAtVal is DateTime dt ? dt : DateTime.Parse(heldAtVal.ToString()!);
                var tableNum = reader.GetInt32(3);
                var serializedItems = reader.GetString(4);

                var items = JsonSerializer.Deserialize<List<OrderItem>>(serializedItems) ?? new List<OrderItem>();

                restored.Add(new HeldOrder
                {
                    Id = id,
                    HoldName = holdName,
                    HeldAt = heldAt,
                    TableNumber = tableNum,
                    Items = items
                });
            }

            if (!alreadyOpen) await conn.CloseAsync();
            return restored;
        }
    }
}
