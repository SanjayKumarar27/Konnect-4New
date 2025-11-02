namespace Konnect_4New.Models.Dtos
{
    public class CreatePostDto
    {
        public int UserId { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public string? Category {  get; set; }
    }

    public class UpdatePostDto
    {
        public string? Content { get; set; }
        public string? ImageUrl { get; set; } // Changed from IFormFile
        public string? Category { get; set; }
    }

    public class PostResponseDto
    {
        public int PostUserId { get; set; }
        public int PostId { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; } // Changed from ImageBase64
        public DateTime? CreatedAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public string ? ProfileImageUrl { get; set; }
    }
}