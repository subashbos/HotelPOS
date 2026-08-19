using System.Collections.Concurrent;
using HotelPOS.Application.Interfaces;
using HotelPOS.Domain.Entities;

namespace HotelPOS.Tests.TestHelpers
{
    /// <summary>In-memory stand-in for the DB-backed lockout store, used to unit test AuthService.</summary>
    public class InMemoryLoginLockoutRepository : ILoginLockoutRepository
    {
        private readonly ConcurrentDictionary<string, LoginLockout> _store = new(StringComparer.OrdinalIgnoreCase);

        public Task<LoginLockout?> GetAsync(string normalizedUsername)
        {
            _store.TryGetValue(normalizedUsername, out var state);
            return Task.FromResult(state);
        }

        public Task SaveAsync(LoginLockout lockout)
        {
            _store[lockout.NormalizedUsername] = lockout;
            return Task.CompletedTask;
        }

        public Task ClearAsync(string normalizedUsername)
        {
            _store.TryRemove(normalizedUsername, out _);
            return Task.CompletedTask;
        }
    }
}
