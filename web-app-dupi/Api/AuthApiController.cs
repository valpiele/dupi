using dupi.Data;
using dupi.Dtos;
using dupi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Google.Apis.Auth;

namespace dupi.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IConfiguration _config;

    public AuthApiController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(new { error = "Invalid email or password." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized(new { error = "Invalid email or password." });

        var claims = await _userManager.GetClaimsAsync(user);
        var dupiUid = claims.FirstOrDefault(c => c.Type == "dupi:uid")?.Value ?? user.Id;

        var token = _jwtTokenService.GenerateToken(user.Id, dupiUid, user.Email!, user.UserName);
        return Ok(new AuthResponse
        {
            Token = token,
            UserId = dupiUid,
            Email = user.Email!,
            DisplayName = user.UserName
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _userManager.AddClaimAsync(user, new Claim("dupi:uid", user.Id));

        var token = _jwtTokenService.GenerateToken(user.Id, user.Id, user.Email!, user.UserName);
        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email!,
            DisplayName = user.UserName
        });
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _config["Authentication:Google:ClientId"]! }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch
        {
            return Unauthorized(new { error = "Invalid Google token." });
        }

        var user = await _userManager.FindByLoginAsync("Google", payload.Subject);
        if (user != null)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var dupiUid = claims.FirstOrDefault(c => c.Type == "dupi:uid")?.Value ?? user.Id;
            var token = _jwtTokenService.GenerateToken(user.Id, dupiUid, user.Email!, payload.Name);
            return Ok(new AuthResponse { Token = token, UserId = dupiUid, Email = user.Email!, DisplayName = payload.Name });
        }

        // Create new user
        user = new ApplicationUser { UserName = payload.Email, Email = payload.Email };
        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
            return BadRequest(new { errors = createResult.Errors.Select(e => e.Description) });

        await _userManager.AddClaimAsync(user, new Claim("dupi:uid", payload.Subject));
        await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));

        var jwt = _jwtTokenService.GenerateToken(user.Id, payload.Subject, user.Email!, payload.Name);
        return Ok(new AuthResponse { Token = jwt, UserId = payload.Subject, Email = user.Email!, DisplayName = payload.Name });
    }
}
