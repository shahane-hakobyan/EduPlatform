namespace EduPlatform.Models
{
    public class LessonGrade
    {
        public int Id { get; set; }
        public int ScheduleLessonId { get; set; }
        public ScheduleLesson ScheduleLesson { get; set; } = null!;
        public int GradeId { get; set; }
        public Grade Grade { get; set; } = null!;
    }
}