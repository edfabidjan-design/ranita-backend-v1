using System;

namespace EcommerceApi.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = default!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
