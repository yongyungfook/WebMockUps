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

namespace WebMockUps.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;

        public HomeController(ILogger<HomeController> logger, HttpClient httpClient, AuthService authService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> IndexAsync(string? redirected, string? returnUrl)
        {
            var url = "";

            try
            {
                if (!string.IsNullOrEmpty(redirected) && redirected == "true")
                {
                    TempData["ErrorAlert"] = "Please log in to access the requested page.";
                }

                var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (role != null && role != "User")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }

                var response = await _httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();

                var responseData = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(responseData))
                {
                    throw new Exception("No data retrieved");
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseData);

                if (apiResponse?.Data == null || apiResponse.Data.PostList == null || apiResponse.Data.CategoryList == null)
                {
                    throw new Exception("Invalid response structure or empty lists");
                }

                var postsWithCategories = apiResponse.Data.PostList.Select(post =>
                {
                    post.Categories = post.PostCategoryIds
                        .Select(catId => apiResponse.Data.CategoryList
                        .FirstOrDefault(c => c.CategoryId == catId)?.CategoryName)
                        .Where(name => name != null)
                        .ToList();

                    return post;
                }).Take(3).ToList();

                return View(postsWithCategories);
            }


            catch (Exception ex)
            {
                await _authService.InsertLog("Error at Index", "", $"Exception detected: {ex.Message}");

                return BadRequest($"An error occurred: {ex.Message}");
            }
        }

        public IActionResult Register()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                var user = await _authService.Authenticate(email, password);

                if (user != null)
                {
                    if (user.Status == "BANNED")
                    {
                        TempData["ErrorMessage"] = "Your account has been banned! Please contact support for assistance.";

                        TempData["ShowLoginModal"] = true;

                        return RedirectToAction("Index", "Home");
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim("Status", user.Status)
                    };


                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                    await _authService.InsertLog("Log In", email, user.Role + "(" + email + ") has logged into the system.");

                    TempData["SuccessAlert"] = "Welcome back, " + user.Username + "!";

                    if (user.Role == "User")
                    {
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }
                }
                else
                {
                    await _authService.InsertLog("Log In", email, "An unsuccessful login attempt has been detected for the email(" + email + ").");

                    TempData["ErrorMessage"] = "Invalid email or password!";

                    TempData["ShowLoginModal"] = true;

                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _authService.InsertLog("Error at Login", email, $"Exception detected: {ex.Message}");

                TempData["ShowLoginModal"] = true;

                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize]
        public async Task<IActionResult> LogOut()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            await _authService.InsertLog("Log Out", email, "User(" + email + ") has logged out from the system.");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["SuccessAlert"] = "You are now logged out.";

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Register(string Fullname, string Username, string Email, string Password, string PhoneNumber)
        {
            if (Fullname.Length > 50 || Username.Length > 50 || Email.Length > 50 || Password.Length > 50 || PhoneNumber.Length > 50)
            {
                TempData["RegMessage"] = "Please enter legit values!";

                return View();
            }

            var registerUser = new RegisterUser { Fullname = Fullname, Username = Username, Email = Email, Password = Password, PhoneNumber = PhoneNumber };

            try
            {
                var success = await _authService.RegisterAsync(registerUser);

                if (success)
                {
                    await _authService.InsertLog("Register", Email, "A new user with the email(" + Email + ") has been registered.");

                    TempData["SuccessMessage"] = "Account created successfully!";

                    TempData["ShowLoginModal"] = true;

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["RegMessage"] = "Email is already linked with an account, please use another email!";

                    return View();
                }
            }
            catch (Exception ex)
            {
                TempData["RegMessage"] = ex.Message;

                await _authService.InsertLog("Error at Register", "", $"Exception detected: {ex.Message}");

                return View();
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdatePassword(string email, string oldPassword, string newPassword)
        {
            try
            {
                var user = await _authService.Authenticate(email, oldPassword);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "The old password you entered is incorrect, please try again!";

                    TempData["ShowPasswordModal"] = true;

                    return RedirectToAction("UserDetails", "Account");
                }
                else
                {
                    var success = await _authService.UpdatePassword(email, newPassword);

                    if (success)
                    {
                        await _authService.InsertLog("Update Password", email, "User(" + email + ") has updated their password.");

                        TempData["SuccessAlert"] = "Password updated!";

                        return RedirectToAction("UserDetails", "Account");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Something went wrong, please try again!";

                        TempData["ShowPasswordModal"] = true;

                        return RedirectToAction("UserDetails", "Account");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _authService.InsertLog("Error at UpdatePassword", "", $"Exception detected: {ex.Message}");

                TempData["ShowPasswordModal"] = true;

                return RedirectToAction("UserDetails", "Account");
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateName(string username, string name)
        {
            try
            {
                var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var user = await _authService.GetUserDetails(email);

                if(username.IsNullOrEmpty() || name.IsNullOrEmpty() || username.Length > 50 || name.Length > 50)
                {
                    TempData["ErrorMessage"] = "Please enter legit name/username!";

                    TempData["ShowUpdateModal"] = true;

                    return RedirectToAction("UserDetails", "Account");
                } 
                else
                {
                    var success = await _authService.UpdateName(email, username, name); 

                    if(success)
                    {
                        await _authService.InsertLog("Update Name", email, $"User({email})has updated their username from {user.Username} to {username} and full name from {user.Fullname} to {name}.");

                        TempData["SuccessAlert"] = "Profile updated!";

                        return RedirectToAction("UserDetails", "Account");
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Something went wrong, please try again!";

                        TempData["ShowUpdateModal"] = true;

                        return RedirectToAction("UserDetails", "Account");
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                await _authService.InsertLog("Error at UpdateName", "", $"Exception detected: {ex.Message}");

                TempData["ShowUpdateModal"] = true;

                return RedirectToAction("UserDetails", "Account");
            }
        }

        public IActionResult AccessDenied()
        {
            TempData["ErrorAlert"] = "Access is denied!";

            return RedirectToAction("Index", "Home");

        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
