using System;

namespace Konnect_4New.Models.Dtos
{
    public class LikeDto
    {
        public int PostId { get; set; }
        public int UserId { get; set; }   // (later should come from JWT)
    }

    public class LikeResponseDto
    {
        public int LikeId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}
