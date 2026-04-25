namespace EcommerceApi.Models
{
    public class Neighborhood
    {
        public int Id { get; set; }

        public int DistrictId { get; set; }
        public District District { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}