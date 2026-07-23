namespace EduPlatform.ViewModels
{
    public class ScheduleReadViewModel
    {
        public List<ScheduleDayViewModel> Days { get; set; } = new();
        public List<ScheduleEntryViewModel> SelectedDayEntries { get; set; } = new();
        public int TotalClassCount { get; set; }
        public int TotalFreeCount { get; set; }
    }
}