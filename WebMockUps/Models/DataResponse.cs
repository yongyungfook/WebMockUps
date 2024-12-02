namespace WebMockUps.Models
{
    public class DataResponse
    {
        public List<Post> PostList { get; set; }
        public List<Category> CategoryList { get; set; }
        public List<Account> LiveAccountList { get; set; }
        public List<Account> DemoAccountList { get; set; }
        public List<Transaction> UserTransactionList { get; set; } 
        public Dictionary<int, List<Transaction>> TransactionsByAccount { get; set; }
        public User User { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
