namespace EduPlatform.ViewModels
{
    public class SidebarMenuItem
    {
        public string Label { get; set; } = "";
        public string IconSvg { get; set; } = "";      
        public string? Url { get; set; }
        public List<SidebarMenuItem> Children { get; set; } = new();
    }
}
