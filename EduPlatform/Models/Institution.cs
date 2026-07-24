namespace EduPlatform.Models
{
    public class Institution
    {
        public int Id { get; set; }

        public string KtakInstitutionId { get; set; } = "";   // external app id
        public string Name { get; set; } = "";
        public Place Place { get; set; }                      // which of the 3

        public List<Grade> Grades { get; set; } = new();
    }
}