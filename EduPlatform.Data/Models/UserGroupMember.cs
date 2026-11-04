namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Խմբի անդամ (ստեղծողը նույնպես անդամ է՝ Admin դերով)
    /// </summary>
    public class UserGroupMember
    {
        public int GroupId { get; set; }
        public UserGroup Group { get; set; } = null!;

        public string UserId { get; set; } = "";

        public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;

        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}
