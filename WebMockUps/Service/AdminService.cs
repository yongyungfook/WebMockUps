using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data;
using System.Security.Claims;
using System.Text;
using WebMockUps.Data;
using WebMockUps.Models;
using WebMockUps.Controllers;

namespace WebMockUps.Service
{
    public class AdminService
    {
        private readonly ILogger<AdminService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;

        public AdminService(HttpClient httpClient, ApplicationDbContext context, ILogger<AdminService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
        }

        public async Task<List<User>> GetAllUsers()
        {
            return await _context.Users
            .Where(u => u.Role == "User")
            .OrderBy(u => u.Email)
            .ToListAsync();
        }

        public async Task<List<User>> GetAllAdmin()
        {
            return await _context.Users
            .Where(u => u.Role == "Supervisor" || u.Role == "Staff")
            .OrderByDescending(u => u.Status)
            .ToListAsync();
        }

        public async Task<User> GetUserDetails(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                return user;
            }
            else
            {
                throw new Exception("Session timeout, please log in!");
            }
        }

        public async Task<List<Transaction>> GetTransactionsByUser(string email)
        {
            var emailParam = new SqlParameter("@Email", email);

            return await _context.Transactions.FromSqlRaw("EXEC sp_GetTransactionsByUser @Email", emailParam).ToListAsync();
        }

        public async Task<int> UpdateUserStatus(string email, string status)
        {
            var emailParam = new SqlParameter("@Email", email);
            var statusParam = new SqlParameter("@Status", status);

            return await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateUserStatus @Email, @Status", emailParam, statusParam);
        }

        public bool DeleteAccount(int accountNumber)
        {
            var transactionsToDelete = _context.Transactions
            .Where(t => t.AccountNumber == accountNumber);

            if (transactionsToDelete.Any())
            {
                _context.Transactions.RemoveRange(transactionsToDelete);
                _context.SaveChanges();
            }
                
            var account = _context.Accounts.FirstOrDefault(a => a.AccountNumber == accountNumber);
            if (account == null)
            {
                throw new Exception("Account not found.");
            }

            _context.Accounts.Remove(account);
            _context.SaveChanges();

            return true;
        }

        public async Task<bool> RegisterStaff(RegisterUser registerUser)
        {
            if (registerUser.Fullname.Length > 50 || registerUser.Username.Length > 50 || registerUser.Email.Length > 50 || registerUser.Password.Length > 50 || registerUser.PhoneNumber.Length > 50)
            {
                throw new Exception("Please enter real value...");
            }

            var passwordHasher = new PasswordHasher<object>();
            string hashedPassword = passwordHasher.HashPassword(null, registerUser.Password);

            var fullNameParam = new SqlParameter("@Fullname", registerUser.Fullname);
            var usernameParam = new SqlParameter("@Username", registerUser.Username);
            var emailParam = new SqlParameter("@Email", registerUser.Email);
            var passwordParam = new SqlParameter("@Password", hashedPassword);
            var phoneNumberParam = new SqlParameter("@PhoneNumber", registerUser.PhoneNumber);
            var roleParam = new SqlParameter("@Role", "Staff");

            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_InsertUser @Fullname, @Username, @Email, @Password, @PhoneNumber, @Role",
                fullNameParam, usernameParam, emailParam, passwordParam, phoneNumberParam, roleParam);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public async Task<int> UpdateAdminRole(string email, string role)
        {
            var emailParam = new SqlParameter("@Email", email);
            var roleParam = new SqlParameter("@Role", role);

            return await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateAdminRole @Email, @Role", emailParam, roleParam);
        }

        public async Task<List<Log>> GetLog()
        {
            return await _context.Logs.OrderByDescending(log => log.LogTime).ToListAsync();
        }

        public async Task<List<Log>> GetLogByEmail(string email)
        {
            return await _context.Logs.Where(l => l.Email == email).OrderByDescending(log => log.LogTime).ToListAsync();
        }

        public async Task<Log> GetLogById(int id)
        {
            var log = await _context.Logs.FindAsync(id);

            return log;
        }

        public bool DeleteLog(Log log)
        {
            _context.Logs.Remove(log);

            _context.SaveChanges();

            return true;
        }

        public async Task<bool> InsertLog(string type, string email, string message)
        {
            var typeParam = new SqlParameter("@EventType", type);
            var emailParam = new SqlParameter("@Email", email);
            var messageParam = new SqlParameter("@Message", message);

            await _context.Database.ExecuteSqlRawAsync("EXEC sp_InsertLog @EventType, @Email, @Message", typeParam, emailParam, messageParam);

            return true;
        }

        public async Task<int> GetUserCount()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> GetAccountCount()
        {
            return await _context.Accounts.CountAsync();
        }

        public async Task<decimal> GetTotalBalance()
        {
            var userBalance = await _context.Users.SumAsync(u => u.Balance);
            var accountBalance = await _context.Accounts.SumAsync(a => a.Balance);

            return userBalance + accountBalance;
        }
    }
}
