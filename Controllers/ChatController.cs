using Konnect_4New.Models;
using Konnect_4New.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Konnect_4New.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly Konnect4Context _context;

        public ChatController(Konnect4Context context)
        {
            _context = context;
        }

        // GET: api/chat/conversations/{userId}
        [HttpGet("conversations/{userId}")]
        public async Task<ActionResult<IEnumerable<ConversationDto>>> GetUserConversations(int userId)
        {
            try
            {
                // First, get all conversation IDs for this user
                var conversationIds = await _context.ConversationParticipants
                    .Where(cp => cp.UserId == userId)
                    .Select(cp => cp.ConversationId)
                    .ToListAsync();

                // Then load conversations with participants
                var conversations = await _context.Conversations
                    .Where(c => conversationIds.Contains(c.ConversationId))
                    .Include(c => c.ConversationParticipants)
                        .ThenInclude(cp => cp.User)
                    .OrderByDescending(c => c.LastMessageAt)
                    .ToListAsync();

                var conversationDtos = new List<ConversationDto>();

                foreach (var conversation in conversations)
                {
                    var otherParticipant = conversation.ConversationParticipants
                        .FirstOrDefault(p => p.UserId != userId)?.User;

                    if (otherParticipant == null) continue;

                    // Get unread count
                    var unreadCount = await _context.Messages
                        .Where(m => m.ConversationId == conversation.ConversationId
                            && m.SenderId != userId
                            && !m.IsDeleted)
                        .CountAsync();

                    // Get last message separately
                    var lastMessage = await _context.Messages
                        .Where(m => m.ConversationId == conversation.ConversationId && !m.IsDeleted)
                        .OrderByDescending(m => m.SentAt)
                        .Include(m => m.Sender)
                        .FirstOrDefaultAsync();

                    MessageDto? lastMessageDto = null;

                    if (lastMessage != null)
                    {
                        var isRead = await _context.MessageReadStatus
                            .AnyAsync(mrs => mrs.MessageId == lastMessage.MessageId && mrs.UserId == userId);

                        lastMessageDto = new MessageDto
                        {
                            MessageId = lastMessage.MessageId,
                            ConversationId = lastMessage.ConversationId,
                            SenderId = lastMessage.SenderId,
                            SenderUsername = lastMessage.Sender.Username,
                            SenderFullName = lastMessage.Sender.FullName ?? lastMessage.Sender.Username,
                            SenderAvatar = lastMessage.Sender.ProfileImageUrl,
                            Content = lastMessage.Content,
                            MessageType = lastMessage.MessageType,
                            FileUrl = lastMessage.FileUrl,
                            SentAt = lastMessage.SentAt,
                            IsEdited = lastMessage.IsEdited,
                            IsDeleted = lastMessage.IsDeleted,
                            IsRead = isRead
                        };
                    }

                    conversationDtos.Add(new ConversationDto
                    {
                        ConversationId = conversation.ConversationId,
                        OtherUser = new ParticipantDto
                        {
                            UserId = otherParticipant.UserId,
                            Username = otherParticipant.Username,
                            FullName = otherParticipant.FullName ?? otherParticipant.Username,
                            ProfileImageUrl = otherParticipant.ProfileImageUrl,
                            IsOnline = false
                        },
                        LastMessage = lastMessageDto,
                        UnreadCount = unreadCount,
                        LastMessageAt = conversation.LastMessageAt,
                        CreatedAt = conversation.CreatedAt
                    });
                }

                return Ok(conversationDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}", innerException = ex.InnerException?.Message });
            }
        }

        // GET: api/chat/conversation/{conversationId}/messages
        [HttpGet("conversation/{conversationId}/messages")]
        public async Task<ActionResult<ConversationDetailDto>> GetConversationMessages(
            int conversationId,
            [FromQuery] int userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var isParticipant = await _context.ConversationParticipants
                    .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

                if (!isParticipant)
                    return Unauthorized("You are not a participant in this conversation");

                var conversation = await _context.Conversations
                    .Include(c => c.ConversationParticipants)
                        .ThenInclude(cp => cp.User)
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

                if (conversation == null)
                    return NotFound("Conversation not found");

                var messages = await _context.Messages
                    .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                    .Include(m => m.Sender)
                    .OrderByDescending(m => m.SentAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                messages.Reverse();

                var messageDtos = new List<MessageDto>();
                foreach (var m in messages)
                {
                    var isRead = await _context.MessageReadStatus
                        .AnyAsync(mrs => mrs.MessageId == m.MessageId && mrs.UserId == userId);

                    messageDtos.Add(new MessageDto
                    {
                        MessageId = m.MessageId,
                        ConversationId = m.ConversationId,
                        SenderId = m.SenderId,
                        SenderUsername = m.Sender.Username,
                        SenderFullName = m.Sender.FullName ?? m.Sender.Username,
                        SenderAvatar = m.Sender.ProfileImageUrl,
                        Content = m.Content,
                        MessageType = m.MessageType,
                        FileUrl = m.FileUrl,
                        SentAt = m.SentAt,
                        IsEdited = m.IsEdited,
                        IsDeleted = m.IsDeleted,
                        IsRead = isRead
                    });
                }

                var participantDtos = conversation.ConversationParticipants.Select(cp => new ParticipantDto
                {
                    UserId = cp.User.UserId,
                    Username = cp.User.Username,
                    FullName = cp.User.FullName ?? cp.User.Username,
                    ProfileImageUrl = cp.User.ProfileImageUrl,
                    IsOnline = false
                }).ToList();

                var conversationDetail = new ConversationDetailDto
                {
                    ConversationId = conversation.ConversationId,
                    Participants = participantDtos,
                    Messages = messageDtos,
                    CreatedAt = conversation.CreatedAt
                };

                return Ok(conversationDetail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}", innerException = ex.InnerException?.Message });
            }
        }

        // POST: api/chat/conversation
        [HttpPost("conversation")]
        public async Task<ActionResult<ConversationDto>> CreateOrGetConversation(
            [FromBody] CreateConversationDto dto,
            [FromQuery] int currentUserId)
        {
            try
            {
                var existingConversation = await _context.ConversationParticipants
                    .Where(cp => cp.UserId == currentUserId)
                    .Include(cp => cp.Conversation)
                        .ThenInclude(c => c.ConversationParticipants)
                            .ThenInclude(cp => cp.User)
                    .Select(cp => cp.Conversation)
                    .FirstOrDefaultAsync(c => c.ConversationParticipants.Any(cp => cp.UserId == dto.ParticipantUserId));

                if (existingConversation != null)
                {
                    var otherUser = existingConversation.ConversationParticipants
                        .FirstOrDefault(cp => cp.UserId != currentUserId)?.User;

                    if (otherUser == null)
                        return BadRequest("Invalid conversation state");

                    return Ok(new ConversationDto
                    {
                        ConversationId = existingConversation.ConversationId,
                        OtherUser = new ParticipantDto
                        {
                            UserId = otherUser.UserId,
                            Username = otherUser.Username,
                            FullName = otherUser.FullName ?? otherUser.Username,
                            ProfileImageUrl = otherUser.ProfileImageUrl,
                            IsOnline = false
                        },
                        LastMessage = null,
                        UnreadCount = 0,
                        LastMessageAt = existingConversation.LastMessageAt,
                        CreatedAt = existingConversation.CreatedAt
                    });
                }

                var participant = await _context.Users.FindAsync(dto.ParticipantUserId);
                if (participant == null)
                    return NotFound("User not found");

                var conversation = new Conversation
                {
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();

                _context.ConversationParticipants.AddRange(
                    new ConversationParticipant
                    {
                        ConversationId = conversation.ConversationId,
                        UserId = currentUserId,
                        JoinedAt = DateTime.UtcNow
                    },
                    new ConversationParticipant
                    {
                        ConversationId = conversation.ConversationId,
                        UserId = dto.ParticipantUserId,
                        JoinedAt = DateTime.UtcNow
                    }
                );

                await _context.SaveChangesAsync();

                return Ok(new ConversationDto
                {
                    ConversationId = conversation.ConversationId,
                    OtherUser = new ParticipantDto
                    {
                        UserId = participant.UserId,
                        Username = participant.Username,
                        FullName = participant.FullName ?? participant.Username,
                        ProfileImageUrl = participant.ProfileImageUrl,
                        IsOnline = false
                    },
                    LastMessage = null,
                    UnreadCount = 0,
                    LastMessageAt = conversation.LastMessageAt,
                    CreatedAt = conversation.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}", innerException = ex.InnerException?.Message });
            }
        }

        // POST: api/chat/message
        [HttpPost("message")]
        public async Task<ActionResult> SendMessage([FromBody] SendMessageDto dto, [FromQuery] int senderId)
        {
            try
            {
                var conversation = await GetOrCreateConversation(senderId, dto.ReceiverId);

                var message = new Message
                {
                    ConversationId = conversation.ConversationId,
                    SenderId = senderId,
                    Content = dto.Content,
                    MessageType = dto.MessageType,
                    FileUrl = dto.FileUrl,
                    SentAt = DateTime.UtcNow,
                    IsEdited = false,
                    IsDeleted = false
                };

                _context.Messages.Add(message);
                conversation.LastMessageAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var sender = await _context.Users.FindAsync(senderId);

                var messageDto = new MessageDto
                {
                    MessageId = message.MessageId,
                    ConversationId = message.ConversationId,
                    SenderId = senderId,
                    SenderUsername = sender.Username,
                    SenderFullName = sender.FullName ?? sender.Username,
                    SenderAvatar = sender.ProfileImageUrl,
                    Content = message.Content,
                    MessageType = message.MessageType,
                    FileUrl = message.FileUrl,
                    SentAt = message.SentAt,
                    IsEdited = message.IsEdited,
                    IsDeleted = message.IsDeleted,
                    IsRead = false
                };

                return Ok(messageDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}", innerException = ex.InnerException?.Message });
            }
        }

        // PUT: api/chat/message/{messageId}
        [HttpPut("message/{messageId}")]
        public async Task<ActionResult> EditMessage(int messageId, [FromBody] string newContent, [FromQuery] int userId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);

                if (message == null)
                    return NotFound("Message not found");

                if (message.SenderId != userId)
                    return Unauthorized("You can only edit your own messages");

                message.Content = newContent;
                message.IsEdited = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Message updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // DELETE: api/chat/message/{messageId}
        [HttpDelete("message/{messageId}")]
        public async Task<ActionResult> DeleteMessage(int messageId, [FromQuery] int userId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);

                if (message == null)
                    return NotFound("Message not found");

                if (message.SenderId != userId)
                    return Unauthorized("You can only delete your own messages");

                message.IsDeleted = true;
                message.Content = "This message has been deleted";
                await _context.SaveChangesAsync();

                return Ok(new { message = "Message deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // POST: api/chat/conversation/{conversationId}/read
        [HttpPost("conversation/{conversationId}/read")]
        public async Task<ActionResult> MarkConversationAsRead(int conversationId, [FromQuery] int userId)
        {
            try
            {
                var isParticipant = await _context.ConversationParticipants
                    .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

                if (!isParticipant)
                    return Unauthorized("You are not a participant in this conversation");

                var unreadMessages = await _context.Messages
                    .Where(m => m.ConversationId == conversationId
                        && m.SenderId != userId
                        && !m.IsDeleted)
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    var alreadyRead = await _context.MessageReadStatus
                        .AnyAsync(mrs => mrs.MessageId == message.MessageId && mrs.UserId == userId);

                    if (!alreadyRead)
                    {
                        _context.MessageReadStatus.Add(new MessageReadStatus
                        {
                            MessageId = message.MessageId,
                            UserId = userId,
                            ReadAt = DateTime.UtcNow
                        });
                    }
                }

                var participant = await _context.ConversationParticipants
                    .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

                if (participant != null)
                {
                    participant.LastReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Messages marked as read", count = unreadMessages.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // GET: api/chat/unread-count/{userId}
        [HttpGet("unread-count/{userId}")]
        public async Task<ActionResult<int>> GetUnreadCount(int userId)
        {
            try
            {
                var unreadCount = await _context.Messages
                    .Where(m => m.Conversation.ConversationParticipants.Any(cp => cp.UserId == userId)
                        && m.SenderId != userId
                        && !m.IsDeleted)
                    .CountAsync();

                return Ok(unreadCount);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}", innerException = ex.InnerException?.Message });
            }
        }

        // DELETE: api/chat/conversation/{conversationId}
        [HttpDelete("conversation/{conversationId}")]
        public async Task<ActionResult> DeleteConversation(int conversationId, [FromQuery] int userId)
        {
            try
            {
                var participant = await _context.ConversationParticipants
                    .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

                if (participant == null)
                    return NotFound("You are not a participant in this conversation");

                _context.ConversationParticipants.Remove(participant);
                await _context.SaveChangesAsync();

                var remainingParticipants = await _context.ConversationParticipants
                    .CountAsync(cp => cp.ConversationId == conversationId);

                if (remainingParticipants == 0)
                {
                    var conversation = await _context.Conversations.FindAsync(conversationId);
                    if (conversation != null)
                    {
                        _context.Conversations.Remove(conversation);
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new { message = "Conversation deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        // GET: api/chat/search
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserListDto>>> SearchUsers(
            [FromQuery] string query,
            [FromQuery] int currentUserId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return Ok(new List<UserListDto>());

                var users = await _context.Users
                    .Where(u => u.UserId != currentUserId
                        && (u.Username.Contains(query) || u.FullName.Contains(query)))
                    .Take(20)
                    .Select(u => new UserListDto
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName,
                        ProfileImageUrl = u.ProfileImageUrl
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        private async Task<Conversation> GetOrCreateConversation(int user1Id, int user2Id)
        {
            var existingConversation = await _context.ConversationParticipants
                .Where(cp => cp.UserId == user1Id)
                .Select(cp => cp.Conversation)
                .Where(c => c.ConversationParticipants.Any(cp => cp.UserId == user2Id))
                .FirstOrDefaultAsync();

            if (existingConversation != null)
                return existingConversation;

            var conversation = new Conversation
            {
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            _context.ConversationParticipants.AddRange(
                new ConversationParticipant
                {
                    ConversationId = conversation.ConversationId,
                    UserId = user1Id,
                    JoinedAt = DateTime.UtcNow
                },
                new ConversationParticipant
                {
                    ConversationId = conversation.ConversationId,
                    UserId = user2Id,
                    JoinedAt = DateTime.UtcNow
                }
            );

            await _context.SaveChangesAsync();
            return conversation;
        }
    }
}