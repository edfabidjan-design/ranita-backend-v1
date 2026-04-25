namespace EcommerceApi.Models
{
    public class VendorWalletTransaction
    {
        public int Id { get; set; }
        public int VendorId { get; set; }
        public decimal Amount { get; set; } // + crédit, - débit
        public string Type { get; set; } = "";
        public string Reference { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? OrderId { get; set; }
        public int? OrderItemId { get; set; }
        public int? ReturnRequestId { get; set; }

        public int? PayoutBatchId { get; set; }
        public DateTime? SettledAt { get; set; }

        public Vendor Vendor { get; set; } = null!;
    }
}
