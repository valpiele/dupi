using dupi.Dtos;
using dupi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Api;

[ApiController]
[Route("api/friends")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SocialApiController : ControllerBase
{
    private readonly SocialService _socialService;

    public SocialApiController(SocialService socialService)
    {
        _socialService = socialService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    [HttpGet]
    public async Task<IActionResult> GetFriends()
    {
        var friends = await _socialService.GetFriendsAsync(UserId);
        var pending = await _socialService.GetPendingReceivedAsync(UserId);

        return Ok(new FriendsListDto
        {
            Friends = friends.Select(f =>
            {
                var friendId = f.F.SenderId == UserId ? f.F.ReceiverId : f.F.SenderId;
                return new FriendDto
                {
                    UserId = friendId,
                    Username = f.Profile?.Username ?? "",
                    DisplayName = f.Profile?.DisplayName ?? "Unknown",
                    Bio = f.Profile?.Bio ?? "",
                    Status = "friends"
                };
            }).ToList(),
            PendingReceived = pending.Select(f => new FriendDto
            {
                UserId = f.F.SenderId,
                Username = f.Profile?.Username ?? "",
                DisplayName = f.Profile?.DisplayName ?? "Unknown",
                Bio = f.Profile?.Bio ?? "",
                Status = "pending_received"
            }).ToList()
        });
    }

    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] FriendRequestDto request)
    {
        var sent = await _socialService.SendRequestAsync(UserId, request.UserId);
        return sent ? Ok(new { sent = true }) : BadRequest(new { error = "Request already exists or invalid." });
    }

    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] FriendRequestDto request)
    {
        await _socialService.AcceptAsync(UserId, request.UserId);
        return Ok(new { accepted = true });
    }

    [HttpPost("decline")]
    public async Task<IActionResult> Decline([FromBody] FriendRequestDto request)
    {
        await _socialService.DeclineAsync(UserId, request.UserId);
        return Ok(new { declined = true });
    }

    [HttpPost("unfriend")]
    public async Task<IActionResult> Unfriend([FromBody] FriendRequestDto request)
    {
        await _socialService.UnfriendAsync(UserId, request.UserId);
        return Ok(new { unfriended = true });
    }
}

public class FriendRequestDto
{
    public string UserId { get; set; } = string.Empty;
}
