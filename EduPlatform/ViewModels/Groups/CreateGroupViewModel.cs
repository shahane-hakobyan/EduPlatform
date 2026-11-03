namespace EduPlatform.ViewModels
{
    public class CreateGroupViewModel
    {
        /// <summary>Սովորողների խմբի համար՝ ուսուցչի դասարաններն ու աշակերտները</summary>
        public List<AssignmentClassGroupViewModel> ClassGroups { get; set; } = new();

        public string SubmitUrl { get; set; } = "#";
        public string CancelUrl { get; set; } = "#";

        /// <summary>Սերվերի կողմից ստուգման սխալ</summary>
        public string? Error { get; set; }

        // սխալից հետո ձևը վերադարձվում է լրացված
        public string Name { get; set; } = "";
        public int Type { get; set; } = 1;
    }

    /// <summary>Խումբ ստեղծելու ձևից եկող տվյալները</summary>
    public class CreateGroupRequest
    {
        public string Name { get; set; } = "";

        /// <summary>GroupType-ի արժեքը (1 սովորողներ, 2 ծնողներ, 3 այլ)</summary>
        public int Type { get; set; }

        /// <summary>Խմբի նկարը (JPG/PNG, մինչև 5MB, պարտադիր չէ)</summary>
        public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }

        /// <summary>Ընտրված անդամների UserId-ները</summary>
        public List<string> MemberIds { get; set; } = new();
    }
}
