namespace HotelPOS.Api.Configuration
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string? Key { get; set; }
        public string Issuer { get; set; } = "HotelPOS";
        public string Audience { get; set; } = "HotelPOSClient";
    }
}
