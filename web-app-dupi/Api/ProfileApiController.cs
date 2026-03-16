using dupi.Dtos;
using dupi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Api;

[ApiController]
[Route("api/profile")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProfileApiController : ControllerBase
{
    private readonly ProfileService _profileService;

    public ProfileApiController(ProfileService profileService)
    {
        _profileService = profileService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    [HttpGet]
    public IActionResult GetProfile()
    {
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var profile = _profileService.GetProfile(UserId, email, displayName);

        return Ok(new ProfileDto
        {
            UserId = profile.UserId,
            Username = profile.Username,
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            IsPublic = profile.IsPublic
        });
    }

    [HttpPut]
    public IActionResult UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ProfileService.IsValidUsername(request.Username))
            return BadRequest(new { error = "Username must be 3-30 characters, alphanumeric, hyphens, or underscores." });

        if (_profileService.IsUsernameTaken(request.Username, UserId))
            return BadRequest(new { error = "Username is already taken." });

        var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var profile = _profileService.GetProfile(UserId, email, displayName);

        profile.Username = request.Username;
        profile.DisplayName = request.DisplayName;
        profile.Bio = request.Bio;
        profile.IsPublic = request.IsPublic;

        _profileService.SaveProfile(profile);

        return Ok(new ProfileDto
        {
            UserId = profile.UserId,
            Username = profile.Username,
            Email = profile.Email,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            IsPublic = profile.IsPublic
        });
    }
}
