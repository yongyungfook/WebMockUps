using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Security.Claims;
using WebMockUps.Models;
using WebMockUps.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Azure.Core;
using System.Web;

namespace WebMockUps.Controllers
{
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly HttpClient _httpClient;
        private readonly AdminService _adminService;
        private readonly AccountService _accountService;

        public AdminController(ILogger<AdminController> logger, HttpClient httpClient, AdminService adminService, AccountService accountService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _adminService = adminService;
            _accountService = accountService;
        }

        [Authorize(Roles = "Admin, Supervisor, Staff")]
        public async Task<IActionResult> Dashboard()
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {               
                TempData["userCount"] = await _adminService.GetUserCount();
                TempData["accountCount"] = await _adminService.GetAccountCount();
                TempData["totalBalance"] = await _adminService.GetTotalBalance();

                var logs = await _adminService.GetLog();
                return View(logs.Take(8).ToList());             
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at Dashboard", adminEmail, $"Exception detected: {ex.Message}");

                return View();
            }
        }

        [Authorize(Roles = "Admin, Staff, Supervisor")]
        public async Task<IActionResult> ManageUser()
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                var users = await _adminService.GetAllUsers();

                return View(users);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at ManageUser", adminEmail, $"Exception detected: {ex.Message}");

                return View();
            }
        }

        [Authorize(Roles = "Admin, Staff, Supervisor")]
        public async Task<IActionResult> ManageUserDetails(string email, string? reload, int page = 1)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                if (reload != null && reload == "true")
                {
                    TempData["ShowTransactionModal"] = true;
                }

                var user = await _adminService.GetUserDetails(email);

                var liveAccounts = await _accountService.GetLiveAccountsAsync(email);
                var demoAccounts = await _accountService.GetDemoAccountsAsync(email);

                var allAccounts = liveAccounts.Concat(demoAccounts);

                int pageSize = 10;
                var userTransactions = await _adminService.GetTransactionsByUser(email);

                int totalTransactions = userTransactions.Count();
                var paginatedTransactions = userTransactions
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var transactionsByAccount = new Dictionary<int, List<Transaction>>();

                foreach (var account in allAccounts)
                {
                    transactionsByAccount[account.AccountNumber] = await _accountService.GetTransactionsByAccountNumberAsync(account.AccountNumber);
                }

                var mainBalance = await _accountService.GetUserDetailsAsync(email);

                TempData["balance"] = mainBalance.Balance.ToString();

                var viewModel = new DataResponse
                {
                    LiveAccountList = liveAccounts,
                    DemoAccountList = demoAccounts,
                    TransactionsByAccount = transactionsByAccount,
                    User = user,
                    UserTransactionList = paginatedTransactions,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling((double)totalTransactions / pageSize)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at ManageUserDetails", adminEmail, $"Exception detected: {ex.Message}");

                return View();
            }
        }

        [Authorize(Roles = "Admin, Supervisor")]
        [HttpPost]
        public async Task<IActionResult> RestrictUser(string email, string status)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {                
                var result = 0;

                if (status.ToUpper() != "RESTRICTED")
                {
                    result = await _adminService.UpdateUserStatus(email, "RESTRICTED");
                    TempData["SuccessMessage"] = "The user has been restricted!";
                }
                else
                {
                    result = await _adminService.UpdateUserStatus(email, "ACTIVE");
                    TempData["SuccessMessage"] = "The user has been unrestricted!";
                }

                if(result == 1)
                {
                    await _adminService.InsertLog("Ban", adminEmail,
                        $"The user({email}) has been {(status == "RESTRICTED" ? "UNRESTRICTED" : "RESTRICTED")} by admin({adminEmail}).");

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                }
                else
                {
                    TempData.Remove("SuccessMessage");

                    TempData["ErrorMessage"] = "Something went wrong, please try again later...";

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                }
            }
            catch (Exception ex)    
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at RestrictUser", adminEmail, $"Exception detected: {ex.Message}");

                return RedirectToAction("ManageUserDetails", "Admin");
            }
        }

        [Authorize(Roles = "Admin, Supervisor")]
        public async Task<IActionResult> BanUser(string email, string status)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                var result = 0;

                if (status != "BANNED")
                {
                    result = await _adminService.UpdateUserStatus(email, "BANNED");
                    TempData["SuccessMessage"] = "The user has been banned!";
                }
                else
                {
                    result = await _adminService.UpdateUserStatus(email, "ACTIVE");
                    TempData["SuccessMessage"] = "The user has been unbanned!";
                }

                if (result == 1)
                {
                    await _adminService.InsertLog("Ban", adminEmail,
                        $"The user({email}) has been {(status == "BANNED" ? "UNBANNED" : "BANNED")} by admin({adminEmail}).");

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                }
                else
                {
                    TempData.Remove("SuccessMessage");

                    TempData["ErrorMessage"] = "Something went wrong, please try again later...";

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at BanUser", adminEmail, $"Exception detected: {ex.Message}");

                return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageAdmin()
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                var users = await _adminService.GetAllAdmin();

                return View(users);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at ManageAdmin", adminEmail, $"Exception detected: {ex.Message}");

                return View();
            }
        }

        [Authorize(Roles = "Admin, Supervisor")]
        public async Task<IActionResult> CreateNewAccount(string type, string email)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                if (type.IsNullOrEmpty())
                {
                    TempData["ErrorMessage"] = "Action failed, please try again later...";

                    return RedirectToAction("Index");
                }

                int result = await _accountService.CreateNewAccount(email, type);

                await _accountService.InsertLog("Create Account", adminEmail, "Admin(" + email + ") has created a new " + type.ToUpper() + " account(ID:" + result + ") for the user(" + email + ").");

                TempData["SuccessMessage"] = "New " + type.ToUpper() + " Account (ID:" + result + ") has been created!";

                return RedirectToAction("ManageUserDetails", "Admin", new { email = email }); 
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at CreateNewAccount", adminEmail, $"Exception detected: {ex.Message}");

                return RedirectToAction("ManageUserDetails", "Admin", new { email = email});
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Supervisor")]
        public async Task<IActionResult> UpdateAccountStatus(string accountType, string accountStatus, int accountNumber, string email, string oldStatus)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                if(accountStatus.IsNullOrEmpty() || accountStatus.IsNullOrEmpty())
                {
                    TempData["ErrorMessage"] = "Action failed, please try again later...";

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                }

                bool result = await _accountService.UpdateAccountStatus(accountNumber, accountType + " - " + accountStatus);

                if(result == true)
                {
                    await _adminService.InsertLog("Update", email, "Admin(" + adminEmail + ") has updated the account(ID:" + accountNumber + ") status of the user(" + email + ") from " + oldStatus + " to " + accountType + " - " + accountStatus + ".");

                    TempData["SuccessMessage"] = "Account status updated!";

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                } 
                else
                {
                    TempData["ErrorMessage"] = "Action failed, please try again later...";

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at UpdateAccountStatus", adminEmail, $"Exception detected: {ex.Message}");

                return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
            }
        }

        [Authorize(Roles = "Admin, Supervisor")]
        public async Task<IActionResult> SwitchAccountType(int accountNumber, string type, string email)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                if (accountNumber == 0 || type.IsNullOrEmpty())
                {
                    TempData["ErrorMessage"] = "Something went wrong, please try again later...";

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                }

                var updatedType = type;

                if (type == "demo")
                {
                    updatedType = "live";
                }
                else
                {
                    updatedType = "demo";
                }

                var result = await _accountService.UpdateAccountType(accountNumber, updatedType);

                if (result == true)
                {                    
                    await _accountService.InsertLog("Update Type", adminEmail, "Admin(" + adminEmail + ") has updated the account(ID:" + accountNumber + ") type of the user(" + email + ") from " + type.ToUpper() + " to " + updatedType.ToUpper() +  ".");

                    TempData["SuccessMessage"] = "Account type updated!";

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                }
                else
                {
                    TempData["ErrorMessage"] = "Action failed, please try again later...";

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                
                await _accountService.InsertLog("Error at SwitchAccountType", adminEmail, $"Exception detected: {ex.Message}");

                return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
            }

        }

        [Authorize(Roles = "Admin, Supervisor")]
        public async Task<IActionResult> DeleteAccount(int accountNumber, string email)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                bool result = _adminService.DeleteAccount(accountNumber);

                if(result == true)
                {
                    await _accountService.InsertLog("Delete Account", adminEmail, "Admin(" + adminEmail + ") has deleted the account(ID:" + accountNumber + ") from the user(" + email + ").");

                    TempData["SuccessMessage"] = "Account deleted!";

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                } 
                else
                {
                    TempData["ErrorMessage"] = "Action failed, please try again later...";

                    return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at DeleteAccount", adminEmail, $"Exception detected: {ex.Message}");

                return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAdminRole(string email, string updateAction)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                var result = 0;
                var oldRole = "";
                var newRole = "";
                var user = await _adminService.GetUserDetails(email);

                if(user.Role.ToUpper() == "STAFF" && updateAction.ToUpper() == "PROMOTE")
                {
                    result = await _adminService.UpdateAdminRole(email, "Supervisor");
                    oldRole = "STAFF";
                    newRole = "SUPERVISOR";
                }
                else if(user.Role.ToUpper() == "STAFF" && updateAction.ToUpper() == "DEMOTE")
                {
                    result = await _adminService.UpdateAdminRole(email, "User");
                    oldRole = "STAFF";
                    newRole = "USER";
                }
                else
                {
                    result = await _adminService.UpdateAdminRole(email, "Staff");
                    oldRole = "SUPERVISOR";
                    newRole = "STAFF";
                }
                
                if(result == 1)
                {
                    TempData["SuccessMessage"] = "Role updated!";

                    await _adminService.InsertLog("Update Role", adminEmail, $"Admin({adminEmail} has {updateAction.ToUpper()}D the role of {email} from {oldRole} to {newRole}. ");

                    return RedirectToAction("ManageAdmin", "Admin");
                } 
                else
                {
                    TempData["ErrorMessage"] = "Action failed, please try again later...";

                    return RedirectToAction("ManageAdmin", "Admin");
                }               
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at UpdateAdminRole", adminEmail, $"Exception detected: {ex.Message}");

                return RedirectToAction("ManageAdmin", "Admin");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BanAdmin(string email, string status)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                var user = await _adminService.GetUserDetails(email);

                var result = 0;

                if (status != "BANNED")
                {
                    result = await _adminService.UpdateUserStatus(email, "BANNED");
                    TempData["SuccessMessage"] = $"The {user.Role} has been banned!";
                }
                else
                {
                    result = await _adminService.UpdateUserStatus(email, "ACTIVE");
                    TempData["SuccessMessage"] = $"The {user.Role} has been unbanned!";
                }

                if (result == 1)
                {
                    await _adminService.InsertLog("Ban", adminEmail,
                        $"The {user.Role}({email}) has been {(status == "BANNED" ? "UNBANNED" : "BANNED")} by admin({adminEmail}).");

                    return RedirectToAction("ManageAdmin", "Admin");
                }
                else
                {
                    TempData.Remove("SuccessMessage");

                    TempData["ErrorMessage"] = "Something went wrong, please try again later...";

                    return RedirectToAction("ManageUserDetails", "Admin");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at BanAdmin", adminEmail, $"Exception detected: {ex.Message}");

                return RedirectToAction("ManageUserDetails", "Admin", new { email = email });
            }
        }

        [Authorize(Roles = "Admin, Supervisor, Staff")]
        public async Task<IActionResult> ManageActivity(string email)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                var logs = await _adminService.GetLogByEmail(email);

                var user = await _adminService.GetUserDetails(email);

                TempData["username"] = user.Username;

                string returnUrl = Request.Path + Request.QueryString;
                TempData["returnUrl"] = HttpUtility.UrlEncode(returnUrl);

                return View(logs);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at ManageActivity", adminEmail, $"Exception detected: {ex.Message}");

                return RedirectToAction("Dashboard", "Admin");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RegisterStaff(string Fullname, string Username, string Email, string Password, string PhoneNumber)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (Fullname.Length > 50 || Username.Length > 50 || Email.Length > 50 || Password.Length > 50 || PhoneNumber.Length > 50)
            {
                TempData["RegMessage"] = "Please enter legit values!";

                TempData["ShowRegModal"] = true;

                return View("ManageAdmin", "Admin");
            }

            var registerUser = new RegisterUser { Fullname = Fullname, Username = Username, Email = Email, Password = Password, PhoneNumber = PhoneNumber };

            try
            {
                var success = await _adminService.RegisterStaff(registerUser);

                if (success)
                {
                    await _adminService.InsertLog("Register", adminEmail, "A new staff with the email(" + Email + ") has been registered.");

                    TempData["SuccessMessage"] = "Account created successfully!";

                    return RedirectToAction("ManageAdmin", "Admin");
                }
                else
                {
                    TempData["RegMessage"] = "Email is already linked with an account, please use another email!";

                    TempData["ShowRegModal"] = true;

                    return View("ManageAdmin", "Admin");
                }
            }
            catch (Exception ex)
            {
                TempData["RegMessage"] = ex.Message;

                await _accountService.InsertLog("Error at RegisterStaff", adminEmail, $"Exception detected: {ex.Message}");

                return View();
            }
        }

        [Authorize(Roles = "Admin, Supervisor, Staff")]
        public async Task<IActionResult> Log()
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                var logs = await _adminService.GetLog();

                return View(logs);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at Log", adminEmail, $"Exception detected: {ex.Message}");

                return View();
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> DeleteSelectedLog(List<int> selectedIds, string returnUrl)
        {
            var adminEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                string decodedReturnUrl = HttpUtility.UrlDecode(returnUrl as string);

                foreach (var id in selectedIds)
                {
                    var result = await _adminService.GetLogById(id);

                    if (result != null)
                    {
                        _adminService.DeleteLog(result);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Something went wrong, please try again later...";

                        return Redirect(decodedReturnUrl ?? Url.Action("Log", "Admin"));
                    }
                }

                TempData["SuccessMessage"] = "Selected log(s) has been deleted!";

                return Redirect(decodedReturnUrl ?? Url.Action("Log", "Admin"));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at DeleteSelectedLog", adminEmail, $"Exception detected: {ex.Message}");

                return Redirect(HttpUtility.UrlDecode(returnUrl as string) ?? Url.Action("Log", "Admin"));
            }
        }

        [Authorize]
        public async Task<IActionResult> LogOut()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

            await _adminService.InsertLog("Log Out", email, role + "(" + email + ") has logged out from the system.");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["SuccessAlert"] = "You are now logged out.";

            return RedirectToAction("Index", "Home");
        }
    }
}
