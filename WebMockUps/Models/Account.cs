using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebMockUps.Models
{
    [Table("Account")]
    public class Account
    {
        [Key]
        public int AccountNumber { get; set; }
        public string? Email { get; set; }
        public string? Platform { get; set; }
        public string? Leverage { get; set; }
        public decimal Balance { get; set; }
        public decimal Equity { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
    }

}
