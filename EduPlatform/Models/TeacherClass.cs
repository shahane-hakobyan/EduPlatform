namespace EduPlatform.Models
{
    public class TeacherClass
    {
        public int Id { get; set; }
        public string TeacherName { get; set; } = "";
        public int SchoolClassId { get; set; }
        public SchoolClass SchoolClass { get; set; } = null!;
    }
}