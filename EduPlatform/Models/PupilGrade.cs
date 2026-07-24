namespace EduPlatform.Models
{
    public class PupilGrade
    {
        public int Id { get; set; }
        public string PupilUserId { get; set; } = "";
        public int GradeId { get; set; }
        public Grade Grade { get; set; } = null!;
    }
}