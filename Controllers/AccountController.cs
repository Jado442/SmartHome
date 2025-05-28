using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Models;
using SmartHome.ViewModels;
using Microsoft.AspNetCore.Authorization;
using QRCoder;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using SmartHome.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging; 

namespace SmartHome.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<Users> _signInManager;
        private readonly UserManager<Users> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly QrLoginService _qrLoginService;
        private readonly IMemoryCache _cache;

        public AccountController(
            SignInManager<Users> signInManager,
            UserManager<Users> userManager,
            IEmailSender emailSender,
            AppDbContext context,
            QrLoginService qrLoginService,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor)

        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _qrLoginService = qrLoginService;
            _cache = cache;
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            var (qrImage, qrToken) = _qrLoginService.GenerateLoginQr();

            ViewBag.QrCodeUrl = qrImage;
            ViewBag.QrToken = qrToken;

            // Setup polling
            HttpContext.Response.Headers.Add("Polling-Interval", "3000");

            return View();
        }



        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> QrLogin(string token)
        {
            if (_qrLoginService.ValidateToken(token, out var state))
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    _qrLoginService.CompleteAuthentication(token, User.Identity.Name);
                    return View("QrLoginSuccess");
                }

                return View("QrConfirm", new QrConfirmModel { Token = token });
            }

            return View("QrLoginFailed");
        }




        [HttpPost]
        [Authorize]
        {
            if (ModelState.IsValid &&
                 _qrLoginService.CompleteAuthentication(model.Token, User.Identity.Name))
            {
                return Json(new { success = true });
            }
            return Json(new { success = false, error = "Invalid token" });
        }

        [HttpGet]
        public IActionResult CheckQrStatus(string token)
        {
            if (_cache.TryGetValue(token, out QrLoginState state) && state.IsAuthenticated)
            {
                _cache.Remove(token);
                return Json(new
                {
                    valid = true,
                    email = state.Email,
                    redirect = Url.Action("Index", "Home")
                });
            }
            return Json(new { valid = false });
        }

        [HttpGet]
        [Route("Account/Login/RefreshQr")]
        public IActionResult RefreshQr()
        {
            var (newImage, newToken) = _qrLoginService.GenerateLoginQr();

            return Json(new
            {
                newQrCode = newImage,
                newToken = newToken
            });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

                    if (!result.Succeeded)
                    {
                        await RecordFailedAttempt(model.Email, user.Id);
                        ModelState.AddModelError("", "Invalid login attempt.");
                        return View(model);
                    }

                    // Proceed with 2FA
                    var token = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
                    HttpContext.Session.SetString("2FA-Email", model.Email);
                    HttpContext.Session.SetString("2FA-RememberMe", model.RememberMe.ToString());

                    await _emailSender.SendEmailAsync(user.Email, "Your verification code", $"<p>Your code: <strong>{token}</strong></p>");
                    TempData["Message"] = $"We sent a verification code to {model.Email}.";

                    return RedirectToAction("LoginWith2fa");
                }
                else
                {
                    await RecordFailedAttempt(model.Email);
                    ModelState.AddModelError("", "Invalid login attempt.");
                }
            }
            return View(model);
        }

       

        [HttpGet]
        public IActionResult LoginWith2fa()
        {
            return View(new TwoFactorViewModel
            {
                Email = HttpContext.Session.GetString("2FA-Email"),
                RememberMe = bool.Parse(HttpContext.Session.GetString("2FA-RememberMe") ?? "false")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(TwoFactorViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid attempt.");
                return View(model);
            }

            var valid = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, model.Code);
            if (valid)
            {
                await _signInManager.SignInAsync(user, model.RememberMe);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid verification code.");
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> SuspiciousActivity()
        {
            var user = await _userManager.GetUserAsync(User);
            var attempts = await _context.UnauthorizedAccessAttempts
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.AttemptTime)
                .Take(10)
                .ToListAsync();

            return View(attempts);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                Users user = new Users
                {
                    FullName = model.Name,
                    Email = model.Email,
                    UserName = model.Email
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    return RedirectToAction("Login", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        public IActionResult VerifyEmail()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(VerifyEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(model.Email);

                if (user != null)
                {
                    return RedirectToAction("ChangePassword", new { username = user.UserName });
                }

                ModelState.AddModelError("", "Invalid email.");
            }
            return View(model);
        }

        [Authorize]
        {
        }

            return View(new ChangePasswordViewModel { Email = username });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            {
            }

            {
            }
            {
                {
                }
                return View(model);
            }

            // Password changed successfully
            return RedirectToAction("ChangePasswordConfirmation");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private async Task RecordFailedAttempt(string attemptedEmail, string userId = null)
        {
            var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

            var attempt = new UnauthorizedAccessAttempt
            {
                UserId = userId,
                AttemptedEmail = attemptedEmail,
                IpAddress = ip,
                UserAgent = userAgent,
                AttemptTime = DateTime.UtcNow
            };

            _context.UnauthorizedAccessAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await SendSecurityAlertEmail(user.Email, ip);
                    attempt.NotifiedUser = true;
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task SendSecurityAlertEmail(string email, string ip)
        {
            var subject = "Suspicious Login Attempt Detected";
            var message = $@"
                <h3>Security Alert</h3>
                <p>We detected an unauthorized login attempt for your account:</p>
                <ul>
                    <li>Time: {DateTime.UtcNow.ToString("f")} (UTC)</li>
                    <li>IP Address: {ip}</li>
                </ul>
                <p>If this wasn't you, please change your password immediately.</p>
                <p><a href='{Url.Action("ChangePassword", "Account", null, Request.Scheme)}'>Change Password Now</a></p>";

            await _emailSender.SendEmailAsync(email, subject, message);
        }


        
        }
    }




