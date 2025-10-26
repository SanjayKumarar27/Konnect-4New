using System;
using System.Collections.Generic;

namespace Konnect_4New.Models.Dtos
{
    // Request DTOs
    public class SendMessageDto
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "Text";
        public string? FileUrl { get; set; }
    }

    public class CreateConversationDto
    {
        public int ParticipantUserId { get; set; }
    }

    public class MarkAsReadDto
    {
        public int ConversationId { get; set; }
    }

    // Response DTOs
    public class MessageDto
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public string SenderUsername { get; set; } = string.Empty;
        public string SenderFullName { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "Text";
        public string? FileUrl { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsRead { get; set; }
    }

    public class ConversationDto
    {
        public int ConversationId { get; set; }
        public ParticipantDto OtherUser { get; set; } = null!;
        public MessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public DateTime LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ParticipantDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public bool IsOnline { get; set; }
    }

    public class ConversationDetailDto
    {
        public int ConversationId { get; set; }
        public List<ParticipantDto> Participants { get; set; } = new List<ParticipantDto>();
        public List<MessageDto> Messages { get; set; } = new List<MessageDto>();
        public DateTime CreatedAt { get; set; }
    }

    public class TypingIndicatorDto
    {
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsTyping { get; set; }
    }

    public class OnlineStatusDto
    {
        public int UserId { get; set; }
        public bool IsOnline { get; set; }
    }
}