namespace EduPlatform.ViewModels
{
    public class CreatedMaterialsPageViewModel
    {
        public List<CreatedMaterialViewModel> Materials { get; set; } = new();
    }

    public class CreatedMaterialViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Grade { get; set; } = "";
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public string Description { get; set; } = "";
        public DateOnly Date { get; set; }
        public string IconClass { get; set; } = "bi-file-earmark-text";
        public string ColorVariant { get; set; } = "indigo";
        public string EditUrl { get; set; } = "#";
        public string DownloadUrl { get; set; } = "#";
    }
}