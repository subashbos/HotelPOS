using HotelPOS.Domain.Common.Constants;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotelPOS.Infrastructure.Persistence
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly HotelDbContext _context;

        public ReservationRepository(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<Reservation>> GetReservationsAsync(DateTime? date = null)
        {
            var query = _context.Reservations
                .Include(r => r.Table)
                .Include(r => r.Customer)
                .AsQueryable();

            if (date.HasValue)
                query = query.Where(r => r.ReservationDate.Date == date.Value.Date);

            return await query
                .OrderBy(r => r.ReservationDate)
                .ThenBy(r => r.StartTime)
                .ToListAsync();
        }

        public async Task<List<Reservation>> GetActiveReservationsForTableAsync(int tableId, DateTime date, int? excludeReservationId = null)
        {
            var query = _context.Reservations
                .Where(r => r.TableId == tableId
                    && r.ReservationDate.Date == date.Date
                    && r.Status != ReservationStatuses.Cancelled
                    && r.Status != ReservationStatuses.NoShow);

            if (excludeReservationId.HasValue)
                query = query.Where(r => r.Id != excludeReservationId.Value);

            return await query.ToListAsync();
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.Table)
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task AddAsync(Reservation reservation)
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Reservation reservation)
        {
            var existing = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == reservation.Id);
            if (existing == null) throw new KeyNotFoundException($"Reservation #{reservation.Id} not found.");

            existing.TableId = reservation.TableId;
            existing.CustomerId = reservation.CustomerId;
            existing.CustomerName = reservation.CustomerName;
            existing.CustomerPhone = reservation.CustomerPhone;
            existing.ReservationDate = reservation.ReservationDate;
            existing.StartTime = reservation.StartTime;
            existing.EndTime = reservation.EndTime;
            existing.PartySize = reservation.PartySize;
            existing.Notes = reservation.Notes;
            // Status is intentionally not touched here - see IReservationRepository.UpdateAsync.

            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int id, string status)
        {
            var existing = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id);
            if (existing == null) throw new KeyNotFoundException($"Reservation #{id} not found.");

            existing.Status = status;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _context.Reservations.FirstOrDefaultAsync(r => r.Id == id);
            if (existing == null) return;

            _context.Reservations.Remove(existing);
            await _context.SaveChangesAsync();
        }

        private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _currentTransaction;

        public async Task BeginTransactionAsync()
        {
            _currentTransaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task AcquireTableDateLockAsync(int tableId, DateTime date)
        {
            if (!_context.Database.IsSqlServer())
                return;

            if (_context.Database.CurrentTransaction == null)
            {
                throw new InvalidOperationException("AcquireTableDateLockAsync must be called within an active transaction for concurrency safety.");
            }

            var resourceParam = new Microsoft.Data.SqlClient.SqlParameter("@Resource", $"Reservation_{tableId}_{date:yyyyMMdd}");
            var modeParam = new Microsoft.Data.SqlClient.SqlParameter("@LockMode", "Exclusive");
            var ownerParam = new Microsoft.Data.SqlClient.SqlParameter("@LockOwner", "Transaction");
            var timeoutParam = new Microsoft.Data.SqlClient.SqlParameter("@LockTimeout", 15000); // 15 seconds

            var resultParam = new Microsoft.Data.SqlClient.SqlParameter
            {
                ParameterName = "@Result",
                SqlDbType = System.Data.SqlDbType.Int,
                Direction = System.Data.ParameterDirection.Output
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC @Result = sp_getapplock @Resource, @LockMode, @LockOwner, @LockTimeout",
                resultParam, resourceParam, modeParam, ownerParam, timeoutParam);

            var lockResult = (int)resultParam.Value;
            if (lockResult < 0)
            {
                throw new InvalidOperationException($"Could not acquire lock for reservation booking. Result code: {lockResult}");
            }
        }
    }
}
