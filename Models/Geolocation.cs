namespace RFPlayerLibrary.Models
{
    public class Geolocation
    {
        public string As { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string Isp { get; set; } = string.Empty;
        public decimal Lat { get; set; }
        public decimal Lon { get; set; }
        public string Org { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Timezone { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
    }
}