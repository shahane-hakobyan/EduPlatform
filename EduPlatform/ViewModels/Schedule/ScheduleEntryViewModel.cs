namespace EduPlatform.ViewModels
{
    public class ScheduleEntryViewModel
    {
        public int Id { get; set; }
        public int? GradeId { get; set; }
        public int PeriodNumber { get; set; }
        public string TimeRange { get; set; } = "";
        public bool IsFree { get; set; }
        public string? SubjectName { get; set; }
        public string? GradeName { get; set; }
        public string? RoomNumber { get; set; }
        public string? Notes { get; set; }
    }
}