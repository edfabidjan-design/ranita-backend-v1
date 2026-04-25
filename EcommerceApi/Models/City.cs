namespace EcommerceApi.Models
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public ICollection<District> Districts { get; set; } = new List<District>();
    }
}