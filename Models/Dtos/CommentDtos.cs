namespace Konnect_4New.Models.Dtos
{
    public class CreateCommentDto
    {
        public int PostId { get; set; }
        public int UserId { get; set; }   // (later can come from JWT)
        public int? ParentCommentId { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class CommentResponseDto
    {
        public int CommentId { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public int? ParentCommentId { get; set; }
    }
    public class CommentDto
    {
        public int CommentId { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public int? ParentCommentId { get; set; }
        public string Content { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
