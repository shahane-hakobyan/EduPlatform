namespace EduPlatform.ViewModels
{
    public class ToolsPageViewModel
    {
        public List<ToolCardViewModel> Tools { get; set; } = new();
        public string AddToolUrl { get; set; } = "#";
    }

    public class ToolCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string IconClass { get; set; } = "bi-tools";
        public string ColorVariant { get; set; } = "blue";
        public string Url { get; set; } = "#";
    }
}