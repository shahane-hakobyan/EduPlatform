namespace EduPlatform.ViewModels.Statistics
{
    /// <summary>
    /// Գործիքների (IBook, խաչբառ, զույգեր, հարցաշտեմարան) վիճակագրության API-ի պատասխանը։
    /// Նույն ձևաչափն է, ինչ AuthServer-ի AppStatsDto-ն (մարզերի տվյալներն այստեղ պետք չեն)։
    /// </summary>
    public class MaterialStatsDto
    {
        public int TotalMaterials { get; set; }
        public int TotalViews { get; set; }
        public int TotalLikes { get; set; }
        public int TotalShares { get; set; }

        // նախորդ ժամանակաշրջանի արժեքները՝ փոփոխության տոկոսի համար
        public int PreviousPeriodViews { get; set; }
        public int PreviousPeriodLikes { get; set; }
        public int PreviousPeriodShares { get; set; }

        // 12 ամսվա բաշխում
        public List<int> MonthlyViews { get; set; } = new();
        public List<int> MonthlyLikes { get; set; } = new();

        public List<TopMaterialDto> TopMaterials { get; set; } = new();
    }

    public class TopMaterialDto
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public int ViewCount { get; set; }
    }
}