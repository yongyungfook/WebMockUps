using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebMockUps.Models
{
    [Table("User")]
    public class User
    {
        [Key]
        public string Email { get; set; }
        public string HashedPassword { get; set; }
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string PhoneNumber { get; set; }
        public decimal Balance { get; set; }
        public decimal Equity { get; set; }
        public decimal FreeMargin { get; set; }
        public string AccountType { get; set; }
        public string Status { get; set; }
        public string LiveAccountType { get; set; }
        public string Platform { get; set; }
        public decimal Profit { get; set; }
        public decimal Storage { get; set; }
        public decimal Commission { get; set; }
        public decimal Floating { get; set; }
        public decimal Margin { get; set; }
        public decimal MarginLevel { get; set; }
        public decimal MarginLeverage { get; set; }
        public decimal MarginInitial { get; set; }
        public decimal MarginMaintenance { get; set; }
        public decimal SoTime { get; set; }
        public decimal SoLevel { get; set; }
        public decimal SoEquity { get; set; }
        public decimal SoMargin { get; set; }
        public decimal Assets { get; set; }
        public decimal Liabilities { get; set; }
        public decimal BlockedCommission { get; set; }
        public decimal BlockedProfit { get; set; }
        public string Role { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
