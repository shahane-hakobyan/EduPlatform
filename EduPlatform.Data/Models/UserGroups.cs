namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Խումբ (սովորողների, ծնողների կամ այլ)
    /// </summary>
    public class UserGroup
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public GroupType Type { get; set; }

        /// <summary>Խմբի նկարի եզակի անունը ֆայլային պահոցում (պարտադիր չէ)</summary>
        public string? ImageStoredFileName { get; set; }

        /// <summary>Նկարի MIME տեսակը</summary>
        public string? ImageContentType { get; set; }

        /// <summary>Խմբի ստեղծողի (ադմինի) UserId</summary>
        public string CreatedByUserId { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<UserGroupMember> Members { get; set; } = new();
    }
}
