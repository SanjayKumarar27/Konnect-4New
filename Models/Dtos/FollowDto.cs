namespace Konnect_4New.Models.Dtos
{
        public class FollowDto
        {
            public int TargetUserId { get; set; }  // Who the current user wants to follow/unfollow
        }

        public class FollowActionDto
        {
            public int RequestId { get; set; }  // For accepting/rejecting requests
        }
}
