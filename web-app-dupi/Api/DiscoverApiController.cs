using dupi.Dtos;
using dupi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Api;

[ApiController]
[Route("api/discover")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DiscoverApiController : ControllerBase
{
    private readonly ProfileService _profileService;
    private readonly SocialService _socialService;

    public DiscoverApiController(ProfileService profileService, SocialService socialService)
    {
        _profileService = profileService;
        _socialService = socialService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    [HttpGet]
    public async Task<IActionResult> GetProfiles()
    {
        var profiles = _profileService.GetAllPublicProfiles()
            .Where(p => p.UserId != UserId)
            .ToList();

        var statuses = await _socialService.GetStatusesAsync(UserId, profiles.Select(p => p.UserId));

        return Ok(profiles.Select(p => new DiscoverProfileDto
        {
            UserId = p.UserId,
            Username = p.Username,
            DisplayName = p.DisplayName,
            Bio = p.Bio,
            FriendshipStatus = statuses.GetValueOrDefault(p.UserId, "none")
        }));
    }
}
