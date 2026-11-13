namespace EduPlatform.ViewModels.Statistics
{
    /// <summary>«Իմ վիճակագրությունը» էջի տվյալները (ուսուցիչ և սովորող)</summary>
    public class StatisticsPageViewModel
    {
        /// <summary>Ժամանակաշրջան՝ 7d, month, 3m, year</summary>
        public string Period { get; set; } = "month";

        /// <summary>Նյութի տեսակ՝ "" (բոլորը), folder, digitized, crossword</summary>
        public string Type { get; set; } = "";

        public int TotalMaterials { get; set; }
        public int TotalViews { get; set; }
        public int TotalLikes { get; set; }
        public int TotalShares { get; set; }

        public int ViewsChangePercent { get; set; }
        public int LikesChangePercent { get; set; }
        public int SharesChangePercent { get; set; }

        public List<int> MonthlyViews { get; set; } = new();
        public List<int> MonthlyLikes { get; set; } = new();
        public List<TopMaterialDto> TopMaterials { get; set; } = new();

        /// <summary>Քանի աղբյուր չպատասխանեց (ցույց է տրվում զգուշացում)</summary>
        public int FailedSources { get; set; }
    }
}