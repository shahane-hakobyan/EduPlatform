namespace EduPlatform.Models
{
    public class Grade
    {
        public int Id { get; set; }
        public int InstitutionId { get; set; }
        public Institution Institution { get; set; } = null!;
        public string Name { get; set; } = "";   

    }
}