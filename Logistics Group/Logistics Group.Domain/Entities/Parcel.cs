namespace LogisticsGroup.Domain.Entities
{
    public class Parcel
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = string.Empty; 
        public decimal Weight { get; set; }
        public string Status { get; set; } = string.Empty; 

        public int CategoryId { get; set; }
        public CargoCategory Category { get; set; } = null!;

        public int SenderBranchId { get; set; }
        public Branch SenderBranch { get; set; } = null!;

        public int ReceiverBranchId { get; set; }
        public Branch ReceiverBranch { get; set; } = null!;
    }
}