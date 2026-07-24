namespace EduPlatform.Models
{
    public class TeacherGrade
    {
        public int Id { get; set; }
        public string TeacherUserId { get; set; } = "";
        public int GradeId { get; set; }
        public Grade Grade { get; set; } = null!;
    }
}