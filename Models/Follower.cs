using System;
using System.Collections.Generic;

namespace Konnect_4New.Models;

public partial class Follower
{
    public int FollowerId { get; set; }

    public int UserId { get; set; }

    public int FollowerUserId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User FollowerUser { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
