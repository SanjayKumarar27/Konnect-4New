using System;

namespace Konnect_4New.Models
{
    public partial class MessageReadStatus
    {
        public int ReadStatusId { get; set; }
        public int MessageId { get; set; }
        public int UserId { get; set; }
        public DateTime ReadAt { get; set; }

        public virtual Message Message { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}