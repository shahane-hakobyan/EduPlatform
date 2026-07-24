namespace EduPlatform.Models
{
    public class TeacherSubject
    {
        public int Id { get; set; }
        public string TeacherUserId { get; set; } = "";
        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;
    }
}