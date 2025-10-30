// Models/Dtos/AdminDtos.cs - NEW FILE
public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalPosts { get; set; }
    public int TotalComments { get; set; }
    public int ActiveUsersToday { get; set; }
    public List<UserStatsDto> RecentUsers { get; set; } = new();
    public List<PostStatsDto> RecentPosts { get; set; } = new();
}



public class UserStatsDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public int PostsCount { get; set; }
    public int FollowersCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Role { get; set; } = "User";
}

public class PostStatsDto
{
    public int PostId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
    public int UserId { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}