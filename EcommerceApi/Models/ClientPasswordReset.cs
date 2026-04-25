using System;

namespace EcommerceApi.Models
{
    public class CustomerPasswordReset
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string LoginValue { get; set; } = "";
        public string ResetCode { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }

        public Customer? Customer { get; set; }
    }
}