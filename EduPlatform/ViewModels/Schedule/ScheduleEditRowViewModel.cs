namespace EduPlatform.ViewModels
{
    public class ScheduleEditRowViewModel
    {
        public int PeriodNumber { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public bool IsFree { get; set; } = true;
        public int? SubjectId { get; set; }
        public int? GradeId { get; set; }
        public string? RoomNumber { get; set; }
        public string? Notes { get; set; }
    }
}