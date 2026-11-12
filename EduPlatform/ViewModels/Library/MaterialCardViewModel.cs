namespace EduPlatform.ViewModels.Library
{
    public class MaterialCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        /// <summary>Library-ի տեսակը՝ Textbook, MethodicalGuide, OtherMaterial, OtherBook (հեռացնելու համար):</summary>
        public string MaterialType { get; set; } = "";
        /// <summary>«Այլ ուսումնական նյութեր»-ի տեսակը (Տեքստային, Վիդեո, Խաչբառ...), ընթացիկ լեզվով:</summary>
        public string? TypeLabel { get; set; }
        public List<string> Grades { get; set; } = new();
        public List<string> Subjects { get; set; } = new();
        public string ColorVariant { get; set; } = "";
        public string? ThumbnailUrl { get; set; }
        public string Authors { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string? Language { get; set; }
        public int Pages { get; set; }
        /// <summary>Հրատարակման տարին, null՝ եթե հայտնի չէ</summary>
        public int? Year { get; set; }
        public string ViewUrl { get; set; } = "";
        public string DownloadUrl { get; set; } = "";

        /// <summary>Կարգավիճակի անունը ընթացիկ լեզվով («Գործող» / «Արխիվացված»)՝ ֆիլտրի համար, դատարկ է մնացած դեպքերում</summary>
        public string StatusLabel { get; set; } = "";

        /// <summary>Արխիվացված նյութ՝ ցուցադրվում է փոքր պատկերակով</summary>
        public bool IsArchived { get; set; }
    }
}
