namespace EduPlatform.ViewModels
{
    public class ScheduleEditRowViewModel
    {
        public int PeriodNumber { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public bool IsFree { get; set; } = true;
        public List<int> SubjectIds { get; set; } = new();
        public List<int> GradeIds { get; set; } = new();
        public string? RoomNumber { get; set; }
        public string? Notes { get; set; }
    }
}