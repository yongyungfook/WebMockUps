using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using WebMockUps.Data;
using WebMockUps.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebMockUps.Service
{
    public class AccountService
    {
        private readonly ApplicationDbContext _context;

        public AccountService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Account>> GetLiveAccountsAsync(string email)
        {
            var emailParam = new SqlParameter("@Email", email);

            var typeParam = new SqlParameter("@Type", "live");

            return await _context.Accounts.FromSqlRaw("EXEC sp_GetAccountsByEmail @Email, @Type", emailParam, typeParam).ToListAsync();
        }

        public async Task<List<Account>> GetDemoAccountsAsync(string email)
        {
            var emailParam = new SqlParameter("@Email", email);

            var typeParam = new SqlParameter("@Type", "demo");

            return await _context.Accounts.FromSqlRaw("EXEC sp_GetAccountsByEmail @Email, @Type", emailParam, typeParam).ToListAsync();             
        }

        public async Task<List<Account>> GetAllAccountsAsync(string email)
        {
            return await _context.Accounts.Where(a => a.Email == email).ToListAsync();
        } 

        public async Task<User> GetUserDetailsAsync(string email)
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

        public async Task<bool> DepositToAccount(int accountNumber, decimal amount)
        {
            var accountNumberParam = new SqlParameter("@AccountNumber", accountNumber);
            var amountParam = new SqlParameter("@Amount", amount);

            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_DepositToAccount @AccountNumber, @Amount", accountNumberParam, amountParam);

            return result == 1;
        }

        public async Task<bool> DepositFromUser(string email, decimal amount)
        {
            var user = await GetUserDetailsAsync(email);

            if(amount > user.Balance)
            {
                throw new Exception("Insufficient balance to perform the transaction!");
            }

            var emailParam = new SqlParameter("@Email", email);
            var amountParam = new SqlParameter("@Amount", amount);

            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_DepositFromUser @Email, @Amount", emailParam, amountParam);

            return result == 1;

        }

        public async Task<bool> WithdrawFromAccount(int accountNumber, decimal amount)
        {
            var accountNumberParam = new SqlParameter("@AccountNumber", accountNumber);
            var amountParam = new SqlParameter("@Amount", amount);

            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_WithdrawFromAccount @AccountNumber, @Amount", accountNumberParam, amountParam);

            return result == 1;           
        }

        public async Task<bool> WithdrawToUser(string email, decimal amount)
        {        
            var user = await GetUserDetailsAsync(email);              

            var emailParam = new SqlParameter("@Email", email);
            var amountParam = new SqlParameter("@Amount", amount);

            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_WithdrawToUser @Email, @Amount", emailParam, amountParam);

            return result == 1;
        }
        
        public async Task<bool> RecordTransaction(int accountNumber, string type, decimal amount)
        {
            var accountNumberParam = new SqlParameter("@AccountNumber", accountNumber);
            var typeParam = new SqlParameter("@TransactionType", type);
            var amountParam = new SqlParameter("@Amount", amount);

            var result = await _context.Database.ExecuteSqlRawAsync("EXEC sp_InsertTransaction @AccountNumber, @TransactionType, @Amount", accountNumberParam, typeParam, amountParam);

            return true;
        }

        public async Task<bool> UpdateAccountStatus(int accountNumber, string status) 
        {
            var accountNumberParam = new SqlParameter("@AccountNumber", accountNumber);
            var statusParam = new SqlParameter("@Status", status);

            var result = await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateAccountStatus @AccountNumber, @Status", accountNumberParam, statusParam);

            return result == 1;
        }

        public async Task<bool> UpdateAccountType(int accountNumber, string type)
        {
            var accountNumberParam = new SqlParameter("@AccountNumber", accountNumber);
            var typeParam = new SqlParameter("@Type", type);

            var result = await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateAccountType @AccountNumber, @Type", accountNumberParam, typeParam);

            return result == 1;
        }

        public async Task<List<Transaction>> GetTransactionsByAccountNumberAsync(int accountNumber)
        {
            var accountNumberParam = new SqlParameter("@AccountNumber", accountNumber);

            var result = await _context.Transactions.FromSqlRaw("EXEC sp_GetTransactionsByAccountNumber @AccountNumber", accountNumberParam).ToListAsync();

            return result;
        }

        public async Task<int> CreateNewAccount(string email, string type)
        {
            var emailParam = new SqlParameter("@Email", email);
            var typeParam = new SqlParameter("@Type", type);

            var outputParam = new SqlParameter
            {
                ParameterName = "@NewAccountNumber",
                SqlDbType = System.Data.SqlDbType.Int,
                Direction = System.Data.ParameterDirection.Output
            };

            await _context.Database.ExecuteSqlRawAsync("EXEC sp_InsertAccount @Email, @Type, @NewAccountNumber OUTPUT", emailParam, typeParam, outputParam);

            int newAccountNumber = (int)outputParam.Value;
            return newAccountNumber;
        }

        public async Task<List<Transaction>> GetTransactionsByUser(string email)
        {
            var emailParam = new SqlParameter("@Email", email);

            return await _context.Transactions.FromSqlRaw("EXEC sp_GetTransactionsByUser @Email", emailParam).ToListAsync();
        }

        public async Task<bool> InsertLog(string type, string email, string message)
        {
            var typeParam = new SqlParameter("@EventType", type);
            var emailParam = new SqlParameter("@Email", email);
            var messageParam = new SqlParameter("@Message", message);

            await _context.Database.ExecuteSqlRawAsync("EXEC sp_InsertLog @EventType, @Email, @Message", typeParam, emailParam, messageParam);

            return true;
        }
    }
}
