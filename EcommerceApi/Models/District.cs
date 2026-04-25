namespace EcommerceApi.Models
{
    public class District
    {
        public int Id { get; set; }

        public int CityId { get; set; }
        public City City { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public ICollection<Neighborhood> Neighborhoods { get; set; } = new List<Neighborhood>();
    }
}