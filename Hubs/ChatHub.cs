﻿using Konnect_4New.Models;
using Konnect_4New.Models.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Konnect_4New.Hubs
{
    public class ChatHub : Hub
    {
        private readonly Konnect4Context _context;

        // Track online users: UserId -> List of ConnectionIds
        private static readonly ConcurrentDictionary<int, HashSet<string>> _onlineUsers = new();

        // Track user connections: ConnectionId -> UserId
        private static readonly ConcurrentDictionary<string, int> _connections = new();

        public ChatHub(Konnect4Context context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            // Get userId from query string (sent from client)
            var httpContext = Context.GetHttpContext();
            if (httpContext != null && httpContext.Request.Query.TryGetValue("userId", out var userIdStr))
            {
                if (int.TryParse(userIdStr, out int userId))
                {
                    // Add connection tracking
                    _connections[Context.ConnectionId] = userId;

                    // Add to online users
                    _onlineUsers.AddOrUpdate(userId,
                        new HashSet<string> { Context.ConnectionId },
                        (key, existing) =>
                        {
                            existing.Add(Context.ConnectionId);
                            return existing;
                        });

                    // Notify all users that this user is online
                    await Clients.All.SendAsync("UserOnline", new OnlineStatusDto
                    {
                        UserId = userId,
                        IsOnline = true
                    });

                    // Join personal notification group
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

                    // Join all conversation groups
                    var conversations = await _context.ConversationParticipants
                        .Where(cp => cp.UserId == userId)
                        .Select(cp => cp.ConversationId)
                        .ToListAsync();

                    foreach (var conversationId in conversations)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
                    }
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connections.TryRemove(Context.ConnectionId, out int userId))
            {
                // Remove from online users
                if (_onlineUsers.TryGetValue(userId, out var connections))
                {
                    connections.Remove(Context.ConnectionId);

                    // If no more connections for this user, mark as offline
                    if (connections.Count == 0)
                    {
                        _onlineUsers.TryRemove(userId, out _);

                        // Notify all users that this user is offline
                        await Clients.All.SendAsync("UserOffline", new OnlineStatusDto
                        {
                            UserId = userId,
                            IsOnline = false
                        });
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Send a message
        public async Task SendMessage(SendMessageDto dto, int senderId)
        {
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, 
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            try
            {
                // Find or create conversation
                var conversation = await GetOrCreateConversation(senderId, dto.ReceiverId);

                // Create message
                var message = new Message
                {
                    ConversationId = conversation.ConversationId,
                    SenderId = senderId,
                    Content = dto.Content,
                    MessageType = dto.MessageType,
                    FileUrl = dto.FileUrl,
                    SentAt = istTime,
                    IsEdited = false,
                    IsDeleted = false
                };

                _context.Messages.Add(message);

                // Update conversation last message time
                conversation.LastMessageAt = istTime;

                await _context.SaveChangesAsync();

                // Load sender info
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

                // Send to conversation group
                await Clients.Group($"conversation_{conversation.ConversationId}")
                    .SendAsync("ReceiveMessage", messageDto);

                // Send notification to receiver if they're online
                await Clients.Group($"user_{dto.ReceiverId}")
                    .SendAsync("NewMessageNotification", messageDto);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", $"Failed to send message: {ex.Message}");
            }
        }

        // Typing indicator
        public async Task SendTypingIndicator(int conversationId, int userId, bool isTyping)
        {
            var user = await _context.Users.FindAsync(userId);

            var typingDto = new TypingIndicatorDto
            {
                ConversationId = conversationId,
                UserId = userId,
                Username = user?.Username ?? "Unknown",
                IsTyping = isTyping
            };

            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserTyping", typingDto);
        }

        // Mark messages as read
        public async Task MarkAsRead(int conversationId, int userId)
        {
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, 
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            try
            {
                // Get all unread messages in this conversation
                var unreadMessages = await _context.Messages
                    .Where(m => m.ConversationId == conversationId
                        && m.SenderId != userId
                        && !m.MessageReadStatuses.Any(mrs => mrs.UserId == userId))
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    _context.MessageReadStatus.Add(new MessageReadStatus
                    {
                        MessageId = message.MessageId,
                        UserId = userId,
                        ReadAt = istTime
                    });
                }

                // Update last read time
                var participant = await _context.ConversationParticipants
                    .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

                if (participant != null)
                {
                    participant.LastReadAt = istTime;
                }

                await _context.SaveChangesAsync();

                // Notify other participants
                await Clients.OthersInGroup($"conversation_{conversationId}")
                    .SendAsync("MessagesRead", new { ConversationId = conversationId, UserId = userId, MessageIds = unreadMessages.Select(m => m.MessageId) });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", $"Failed to mark as read: {ex.Message}");
            }
        }

        // Delete message
        public async Task DeleteMessage(int messageId, int userId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);

                if (message == null || message.SenderId != userId)
                {
                    await Clients.Caller.SendAsync("Error", "Message not found or unauthorized");
                    return;
                }

                message.IsDeleted = true;
                message.Content = "This message has been deleted";
                await _context.SaveChangesAsync();

                await Clients.Group($"conversation_{message.ConversationId}")
                    .SendAsync("MessageDeleted", new { MessageId = messageId, ConversationId = message.ConversationId });
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("Error", $"Failed to delete message: {ex.Message}");
            }
        }

        // Join conversation (for newly created conversations)
        public async Task JoinConversation(int conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }

        // Helper method to get or create conversation
        private async Task<Conversation> GetOrCreateConversation(int user1Id, int user2Id)
        {
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, 
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

            // Check if conversation exists between these two users
            var existingConversation = await _context.ConversationParticipants
                .Where(cp => cp.UserId == user1Id)
                .Select(cp => cp.Conversation)
                .Where(c => c.ConversationParticipants.Any(cp => cp.UserId == user2Id))
                .FirstOrDefaultAsync();

            if (existingConversation != null)
            {
                return existingConversation;
            }

            // Create new conversation
            var conversation = new Conversation
            {
                CreatedAt = istTime,
                LastMessageAt = istTime
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Add participants
            _context.ConversationParticipants.AddRange(
                new ConversationParticipant
                {
                    ConversationId = conversation.ConversationId,
                    UserId = user1Id,
                    JoinedAt = istTime
                },
                new ConversationParticipant
                {
                    ConversationId = conversation.ConversationId,
                    UserId = user2Id,
                    JoinedAt = istTime
                }
            );

            await _context.SaveChangesAsync();

            // Add both users to conversation group if they're online
            if (_onlineUsers.TryGetValue(user1Id, out var user1Connections))
            {
                foreach (var connectionId in user1Connections)
                {
                    await Groups.AddToGroupAsync(connectionId, $"conversation_{conversation.ConversationId}");
                }
            }

            if (_onlineUsers.TryGetValue(user2Id, out var user2Connections))
            {
                foreach (var connectionId in user2Connections)
                {
                    await Groups.AddToGroupAsync(connectionId, $"conversation_{conversation.ConversationId}");
                }
            }

            return conversation;
        }

        // Get online status for multiple users
        public async Task<List<OnlineStatusDto>> GetOnlineStatus(List<int> userIds)
        {
            return userIds.Select(userId => new OnlineStatusDto
            {
                UserId = userId,
                IsOnline = _onlineUsers.ContainsKey(userId)
            }).ToList();
        }
    }
}