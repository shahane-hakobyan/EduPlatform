namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Առաջադրանք ↔ Առարկա (մեկ առաջադրանք կարող է ունենալ մի քանի առարկա)
    /// </summary>
    public class AssignmentSubject
    {
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;

        public int SubjectId { get; set; }
        public Subject Subject { get; set; } = null!;
    }
}
