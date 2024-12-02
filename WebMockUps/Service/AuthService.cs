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
    public class AuthService
    {
        private readonly ILogger<AuthService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;

        public AuthService(HttpClient httpClient, ApplicationDbContext context, ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
        }

        public async Task<User> Authenticate(string email, string password)
        {
            var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return null;
            }

            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(user, user.HashedPassword, password);

            if (result == PasswordVerificationResult.Success)
            {
                return user;
            }    
            
            return null;
        }

        public async Task<bool> RegisterAsync(RegisterUser registerUser)
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
            var roleParam = new SqlParameter("@Role", "User");

            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_InsertUser @Fullname, @Username, @Email, @Password, @PhoneNumber, @Role", 
                fullNameParam, usernameParam, emailParam, passwordParam, phoneNumberParam, roleParam);
      
            if(result == 1)
            {
                return true;
            } 
            else
            {
                return false;
            }
        }

        public async Task<bool> UpdatePassword(string email, string newPassword)
        {
            var passwordHasher = new PasswordHasher<object>();
            string hashedPassword = passwordHasher.HashPassword(null, newPassword);

            var emailParam = new SqlParameter("@Email", email);
            var passwordParam = new SqlParameter("@NewPassword", hashedPassword);

            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_UpdatePassword @Email, @NewPassword", emailParam, passwordParam);

            if (result == 1) 
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> UpdateName(string email, string username, string name)
        {
            var emailParam = new SqlParameter("@Email", email);
            var usernameParam = new SqlParameter("@Username", username);
            var fullnameParam = new SqlParameter("@Fullname", name);

            var result = await _context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateName @Email, @Username, @Fullname", emailParam, usernameParam, fullnameParam);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
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
