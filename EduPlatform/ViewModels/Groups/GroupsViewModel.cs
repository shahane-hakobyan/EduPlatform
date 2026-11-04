using EduPlatform.ViewModels.Library;

namespace EduPlatform.ViewModels
{
    public class GroupsPageViewModel
    {
        public List<GroupCardViewModel> Groups { get; set; } = new();

        /// <summary>Ֆիլտրեր՝ Խմբի տեսակ, Իմ դերը խմբում</summary>
        public List<MaterialFilterOption> Filters { get; set; } = new();

        public string CreateUrl { get; set; } = "#";
    }

    public class GroupCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        /// <summary>Խմբի նկարը (եթե չկա՝ ցուցադրվում է պատկերակ)</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Pupils | Parents | Other (CSS-ի և ֆիլտրի համար)</summary>
        public string TypeKey { get; set; } = "";
        public string TypeLabel { get; set; } = "";

        public bool IsAdmin { get; set; }
        /// <summary>Լրիվ անվանումը (ֆիլտրի համար)</summary>
        public string RoleLabel { get; set; } = "";

        /// <summary>Կարճ անվանումը քարտի վրա՝ «Ստեղծող» / «Անդամ»</summary>
        public string RoleShortLabel { get; set; } = "";

        public int MemberCount { get; set; }

        public string DetailUrl { get; set; } = "#";
    }

    public class GroupDetailViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string TypeKey { get; set; } = "";
        public string TypeLabel { get; set; } = "";
        public int MemberCount { get; set; }

        /// <summary>Միայն ադմինը կարող է խմբագրել և ջնջել</summary>
        public bool IsAdmin { get; set; }

        public List<GroupMemberViewModel> Members { get; set; } = new();

        public string BackUrl { get; set; } = "#";
        public string EditUrl { get; set; } = "#";
    }

    public class GroupMemberViewModel
    {
        public string Name { get; set; } = "";
        public bool IsAdmin { get; set; }
        public bool IsMe { get; set; }
        public string RoleLabel { get; set; } = "";

        /// <summary>Անդամի պրոֆիլի հղումը (առայժմ տեղապահ՝ պրոֆիլները AuthServer-ից են)</summary>
        public string ProfileUrl { get; set; } = "#";
    }
}
