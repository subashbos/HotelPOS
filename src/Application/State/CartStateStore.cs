using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using HotelPOS.Domain.Entities;
using HotelPOS.Application.Interfaces;

namespace HotelPOS.Application.State
{
    public interface ICartStateStore
    {
        ConcurrentDictionary<int, List<OrderItem>> TableCarts { get; }
        List<HeldOrder> HeldOrders { get; }
        object LockObj { get; }
    }

    public class CartStateStore : ICartStateStore
    {
        public ConcurrentDictionary<int, List<OrderItem>> TableCarts { get; } = new();
        public List<HeldOrder> HeldOrders { get; } = new();
        public object LockObj { get; } = new();
    }
}
