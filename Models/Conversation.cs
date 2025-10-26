using System;
using System.Collections.Generic;

namespace Konnect_4New.Models
{
    public partial class Conversation
    {
        public int ConversationId { get; set; }
        public DateTime LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<ConversationParticipant> ConversationParticipants { get; set; } = new List<ConversationParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}