using HotelPOS.Domain.Entities;
namespace HotelPOS.Application.Interfaces
{
    public interface IUserContext
    {
        string? CurrentUsername { get; }
    }
}
