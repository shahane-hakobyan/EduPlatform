namespace EduPlatform.ViewModels
{
    public class ScheduleEntryViewModel
    {
        public int Id { get; set; }
        public int? GradeId { get; set; }
        public int PeriodNumber { get; set; }
        public string TimeRange { get; set; } = "";
        public bool IsFree { get; set; }
        public List<string> SubjectNames { get; set; } = new();
        public List<GradeChip> Grades { get; set; } = new();
        public string? RoomNumber { get; set; }
        public string? Notes { get; set; }
    }
    public class GradeChip
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}