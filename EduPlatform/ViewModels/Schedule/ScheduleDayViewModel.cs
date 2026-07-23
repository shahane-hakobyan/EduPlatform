namespace EduPlatform.ViewModels
{
    public class ScheduleDayViewModel
    {
        public string Day { get; set; } = "";
        public string DayLabel { get; set; } = "";
        public int ClassCount { get; set; }
        public int FreeCount { get; set; }
        public bool IsSelected { get; set; }
    }
}
