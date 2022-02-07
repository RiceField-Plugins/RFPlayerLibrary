namespace RFPlayerLibrary.Models
{
    public class PlayerGeolocation
    {
        public long Id { get; set; }
        public ulong SteamId { get; set; }
        public string IP { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Timezone { get; set; } = string.Empty;
        public string ISP { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Ass { get; set; } = string.Empty;

        public PlayerGeolocation()
        {
            
        }
        
        public PlayerGeolocation Copy()
        {
            return (PlayerGeolocation) MemberwiseClone();
        }
    }
}