namespace EduPlatform.ViewModels
{
    public class MaterialCardViewModel
    {
        public int Id { get; set; }
        public List<string> Grades { get; set; } = new();     
        public List<string> Subjects { get; set; } = new();   
        public string ColorVariant { get; set; } = "";
        public string? ThumbnailUrl { get; set; }
        public string Authors { get; set; } = "";
        public string Publisher { get; set; } = "";
        public int Pages { get; set; }
        public int Year { get; set; }
        public string ViewUrl { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
    }
}
