namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Առաջադրանք ↔ Դասարանի մակարդակ (1–12)
    /// </summary>
    /// <remarks>
    /// Սա դասարանի <b>մակարդակն</b> է (օր․ 9), այլ ոչ թե «9Ա» խումբը
    /// (տես <see cref="Grade"/>): Կոնկրետ խմբերը/աշակերտները պահվում են
    /// <see cref="AssignmentRecipient"/>-ում:
    /// </remarks>
    public class AssignmentGradeLevel
    {
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;

        /// <summary>Դասարանի մակարդակը՝ 1–12</summary>
        public int Level { get; set; }
    }
}
