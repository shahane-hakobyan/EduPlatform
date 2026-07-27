namespace EduPlatform.Models
{
    public class LessonSubject
    {
        public int Id { get; set; }
        public int ScheduleLessonId { get; set; }
        public ScheduleLesson ScheduleLesson { get; set; } = null!;
        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;
    }
}