using dupi.Dtos;
using dupi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dupi.Api;

[ApiController]
[Route("api/chat")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ChatApiController : ControllerBase
{
    private readonly ChatService _chatService;

    public ChatApiController(ChatService chatService)
    {
        _chatService = chatService;
    }

    private string UserId => User.FindFirstValue("dupi:uid")!;

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var convos = await _chatService.GetConversationsAsync(UserId);
        return Ok(convos.Select(c => new ConversationDto
        {
            FriendId = c.FriendId,
            DisplayName = c.Profile?.DisplayName ?? "Unknown",
            Username = c.Profile?.Username ?? "",
            LastMessage = c.Last.Content,
            LastMessageAt = c.Last.SentAt,
            UnreadCount = c.Unread
        }));
    }

    [HttpGet("{friendId}")]
    public async Task<IActionResult> GetMessages(string friendId, [FromQuery] int skip = 0, [FromQuery] int take = 60)
    {
        var messages = await _chatService.GetMessagesAsync(UserId, friendId, skip, take);
        return Ok(messages.Select(m => new MessageDto
        {
            Id = m.Id,
            SenderId = m.SenderId,
            Content = m.Content,
            SentAt = m.SentAt,
            IsRead = m.IsRead
        }));
    }
}
