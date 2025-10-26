

namespace Konnect_4New.Models.Dtos
{
    public class FollowUserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
    }
}