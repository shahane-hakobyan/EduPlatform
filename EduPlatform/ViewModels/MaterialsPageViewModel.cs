namespace EduPlatform.ViewModels
{
    public class MaterialsPageViewModel
    {
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public int TotalCount { get; set; }
        public List<MaterialFilterOption> Filters { get; set; } = new();
        public List<MaterialCardViewModel> Materials { get; set; } = new();
    }
}
