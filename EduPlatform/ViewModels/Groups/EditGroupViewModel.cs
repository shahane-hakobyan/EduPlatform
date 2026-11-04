namespace EduPlatform.ViewModels
{
    public class EditGroupViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }

        /// <summary>GroupType-ի արժեքը (1 սովորողներ, 2 ծնողներ, 3 այլ)՝ չի փոխվում</summary>
        public int TypeValue { get; set; }
        public string TypeKey { get; set; } = "";
        public string TypeLabel { get; set; } = "";

        /// <summary>Ընթացիկ անդամները՝ հեռացնելու համար</summary>
        public List<EditGroupMemberViewModel> Members { get; set; } = new();

        /// <summary>Նոր անդամներ ավելացնելու համար՝ դասարաններ և սովորողներ (առանց արդեն խմբում եղածների)</summary>
        public List<AssignmentClassGroupViewModel> ClassGroups { get; set; } = new();

        public string SubmitUrl { get; set; } = "#";
        public string CancelUrl { get; set; } = "#";
        public string? Error { get; set; }
    }

    public class EditGroupMemberViewModel
    {
        /// <summary>Անդամի UserId-ն (հեռացնելու ժամանակ ուղարկվում է սերվեր)</summary>
        public string UserId { get; set; } = "";
        public string Name { get; set; } = "";
        public bool IsAdmin { get; set; }
        public string RoleLabel { get; set; } = "";
    }

    /// <summary>Խումբը խմբագրելու ձևից եկող տվյալները</summary>
    public class EditGroupRequest
    {
        public string Name { get; set; } = "";

        /// <summary>Նոր նկար (JPG/PNG, մինչև 5MB)</summary>
        public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }

        /// <summary>Ընթացիկ նկարը հեռացնել</summary>
        public bool RemoveImage { get; set; }

        public List<string> RemoveMemberIds { get; set; } = new();
        public List<string> AddMemberIds { get; set; } = new();
    }
}
