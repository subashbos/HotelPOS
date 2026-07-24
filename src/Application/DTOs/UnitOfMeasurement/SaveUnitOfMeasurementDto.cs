namespace HotelPOS.Application.DTOs.UnitOfMeasurement
{
    /// <summary>DTO for creating or updating a UnitOfMeasurement.</summary>
    public class SaveUnitOfMeasurementDto
    {
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
