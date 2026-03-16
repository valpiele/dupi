using dupi.Data;
using dupi.Models;
using dupi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ProfileService _profileService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ProfileService profileService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _profileService = profileService;
    }

    // GET /Account/Login
    [HttpGet]
    public IActionResult Login(string returnUrl = "/")
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST /Account/Login
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password,
            isPersistent: model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
            return LocalRedirect(model.ReturnUrl);

        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Account locked. Try again later.");
        else
            ModelState.AddModelError(string.Empty, "Invalid email or password.");

        return View(model);
    }

    // GET /Account/Register
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    // POST /Account/Register
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddClaimAsync(user, new Claim("dupi:uid", user.Id));

            // Auto-create a default profile so other users can see this account's info immediately
            var defaultProfile = _profileService.GetProfile(user.Id, model.Email, model.Email);
            _profileService.SaveProfile(defaultProfile);

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Discover");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // GET /Account/LoginWithGoogle
    [HttpGet]
    public IActionResult LoginWithGoogle(string returnUrl = "/")
    {
        var redirectUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl });
        var properties = _signInManager
            .ConfigureExternalAuthenticationProperties("Google", redirectUrl);
        return Challenge(properties, "Google");
    }

    // GET /Account/GoogleCallback
    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string returnUrl = "/", string? remoteError = null)
    {
        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Error from Google: {remoteError}");
            return View("Login", new LoginViewModel { ReturnUrl = returnUrl });
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return RedirectToAction(nameof(Login));

        // Try sign in with existing linked account
        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey,
            isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded)
            return LocalRedirect(returnUrl);

        // First-time Google user — create ApplicationUser and link Google login
        var email = info.Principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var user = new ApplicationUser { UserName = email, Email = email };

        var createResult = await _userManager.CreateAsync(user);
        if (createResult.Succeeded)
        {
            // Store Google sub ID as the canonical blob key for this user
            await _userManager.AddClaimAsync(user, new Claim("dupi:uid", info.ProviderKey));
            await _userManager.AddLoginAsync(user, info);

            var displayName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email;
            var defaultProfile = _profileService.GetProfile(info.ProviderKey, email, displayName);
            _profileService.SaveProfile(defaultProfile);

            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in createResult.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View("Login", new LoginViewModel { ReturnUrl = returnUrl });
    }

    // POST /Account/Logout
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
