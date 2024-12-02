using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http.Headers;
using System.Net.Http;
using WebMockUps.Models;
using WebMockUps.Service;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace WebMockUps.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public AccountController(HttpClient httpClient, AccountService accountService, ILogger<AccountController> logger)
        {
            _httpClient = httpClient;
            _accountService = accountService;
            _logger = logger;
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Index()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Session expired, please log in!";

                TempData["ShowLoginModal"] = true;

                return RedirectToAction("Index", "Home");
            }

            try
            {
                var liveAccounts = await _accountService.GetLiveAccountsAsync(email);
                var demoAccounts = await _accountService.GetDemoAccountsAsync(email);

                var allAccounts = liveAccounts.Concat(demoAccounts);

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
                    TransactionsByAccount = transactionsByAccount
                };
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at AccountIndex", email, $"Exception detected: {ex.Message}");

                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> UserDetails(string ?reload, int page = 1)
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Session expired, please log in!";

                TempData["ShowLoginModal"] = true;

                return RedirectToAction("Index", "Home");
            }

            try
            {
                if(reload != null && reload == "true")
                {
                    TempData["ShowTransactionModal"] = true;
                }

                int pageSize = 10;
                var userDetails = await _accountService.GetUserDetailsAsync(email);
                var transactions = await _accountService.GetTransactionsByUser(email);

                int totalTransactions = transactions.Count();
                var paginatedTransactions = transactions
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                if (userDetails != null)
                {
                    var viewmodel = new DataResponse
                    {
                        User = userDetails,
                        UserTransactionList = paginatedTransactions,
                        CurrentPage = page,
                        TotalPages = (int)Math.Ceiling((double)totalTransactions / pageSize)
                    };

                    return View(viewmodel);
                }
                else
                {
                    throw new Exception("Something went wrong, please try again later...");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at UserIndex", email, $"Exception detected: {ex.Message}");

                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> DepositAccount(decimal depositAmount, int accountNumber, string type)
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {              
                if (string.IsNullOrEmpty(email))
                {
                    TempData["ErrorMessage"] = "Session expired, please log in!";

                    TempData["ShowLoginModal"] = true;

                    return RedirectToAction("Index", "Home");
                } 
                else if(depositAmount <= 0)
                {
                    TempData["ErrorMessage"] = "Please do not enter negative value!";

                    return RedirectToAction("Index");
                }

                var success1 = await _accountService.DepositFromUser(email, depositAmount);
                var success2 = await _accountService.DepositToAccount(accountNumber, depositAmount);                

                if (success1 && success2)
                {
                    await RecordTransaction(accountNumber, "deposit", depositAmount);
                    await _accountService.InsertLog("Deposit", email, "User(" + email + ") has deposited IDR " + depositAmount.ToString("#,0.00") + " into his/her " + type.ToUpper() + " account(ID: " + accountNumber + ").");

                    TempData["SuccessMessage"] = "Transaction successful!";

                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Transaction failed, please try again later...";

                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at DepositAccount", email, $"Exception detected: {ex.Message}");

                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> WithdrawAccount(decimal withdrawAmount, int accountNumber, decimal balance, string type)
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {          
                if (string.IsNullOrEmpty(email))
                {
                    TempData["ErrorMessage"] = "Session expired, please log in!";

                    TempData["ShowLoginModal"] = true;

                    return RedirectToAction("Index", "Home");
                }

                if(withdrawAmount > balance)
                {
                    TempData["ErrorMessage"] = "Insufficient balance to perform the transaction!";

                    return RedirectToAction("Index");
                }
                else if (withdrawAmount <= 0)
                {
                    TempData["ErrorMessage"] = "Please do not enter negative value!";

                    return RedirectToAction("Index");
                }

                var success1 = await _accountService.WithdrawToUser(email, withdrawAmount);
                var success2 = await _accountService.WithdrawFromAccount(accountNumber, withdrawAmount);

                if (success1 && success2)
                {
                    await RecordTransaction(accountNumber, "withdraw", withdrawAmount);
                    await _accountService.InsertLog("Withdraw", email, "User(" + email + ") has withdrew IDR " + withdrawAmount.ToString("#,0.00") + " from his/her " + type.ToUpper() + " account(ID: " + accountNumber + ").");

                    TempData["SuccessMessage"] = "Transaction successful!";

                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Transaction failed, please try again later...";

                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at WithdrawAccount", email, $"Exception detected: {ex.Message}");

                return RedirectToAction("Index");
            }
        }

        [NonAction]
        public async Task<bool> RecordTransaction(int accountNumber, string type, decimal amount)
        {
            try
            {
                var result = await _accountService.RecordTransaction(accountNumber, type, amount);

                return result == true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> UpdateAccountStatus(int id, string status)
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                if(id == 0 || status.IsNullOrEmpty())
                {
                    TempData["ErrorMessage"] = "Action failed, please try again later...";

                    return RedirectToAction("Index");
                }

                var updatedStatus = status; 
                var oldStatus = status;

                if (status.Contains("INACTIVE", StringComparison.OrdinalIgnoreCase))
                {
                    updatedStatus = status.Replace("INACTIVE", "ACTIVE");
                } 
                else
                {
                    updatedStatus = status.Replace("ACTIVE", "INACTIVE");
                }

                var result = await _accountService.UpdateAccountStatus(id, updatedStatus);

                if(result == true) 
                {                 
                    await _accountService.InsertLog("Update Status", email, "User(" + email + ") has updated the status of his/her account(ID: " + id + ") from " + status + " to " + updatedStatus + ".");

                    TempData["SuccessMessage"] = "Status updated!";

                    return RedirectToAction("Index");
                } 
                else
                {
                    TempData["ErrorMessage"] = "Action failed, please try again later...";

                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at RecordTransaction", email, $"Exception detected: {ex.Message}");

                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> SwitchAccountType(int accountNumber, string type)
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {           
                if (accountNumber == 0 || type.IsNullOrEmpty())
                {
                    TempData["ErrorMessage"] = "Action failed, please try again later...";

                    return RedirectToAction("Index");
                }

                var updatedType = type;

                if(type == "demo")
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
                    await _accountService.InsertLog("Update Type", email, "User(" + email + ") has updated the type of the account(ID: " + accountNumber + ") from " + type.ToUpper() + " to " + updatedType.ToUpper() + ".");

                    TempData["SuccessMessage"] = "Account type updated!";

                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Action failed, please try again later...";



                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at SwitchAccountType", email, $"Exception detected: {ex.Message}");

                return RedirectToAction("Index");
            }

        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> CreateNewAccount(string type)
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            try
            {
                if (type.IsNullOrEmpty())
                {
                    TempData["ErrorMessage"] = "Action failed, please try again later...";

                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(email))
                {
                    if (!TempData.ContainsKey("ErrorMessage"))
                    {
                        TempData["ErrorMessage"] = "Session expired, please log in!";
                        TempData["ShowLoginModal"] = true;
                    }
                    return RedirectToAction("Index", "Home");
                } 
                else
                {
                    int result = await _accountService.CreateNewAccount(email, type);

                    await _accountService.InsertLog("Update Type", email, "User(" + email + ") has created a new " + type.ToUpper() + " account(ID: " + result + ")");

                    TempData["SuccessMessage"] = "New Live Account (ID:" + result + ") has been created!";

                    return RedirectToAction("Index");
                }              
            } 
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _accountService.InsertLog("Error at CreateNewAccount", email, $"Exception detected: {ex.Message}");

                return RedirectToAction("Index");
            }
        }
    }
}
