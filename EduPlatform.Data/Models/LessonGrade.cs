namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Կապող աղյուսակ՝ որ դասարաններն են մասնակցում տվյալ դասին:
    /// </summary>
    /// <remarks>
    /// Դասացուցակի քարտի վրա դասարանի համարին սեղմելիս բացվում է
    /// այդ դասարանի աշակերտների ցանկը:
    /// Առաջնային բանալին՝ <c>(ScheduleLessonId, GradeId)</c>:
    /// </remarks>
    public class LessonGrade
    {
        /// <summary>Դասը, որին վերաբերում է գրառումը</summary>
        public int ScheduleLessonId { get; set; }

        /// <summary>Նավիգացիա դեպի <see cref="ScheduleLesson"/>:</summary>
        public ScheduleLesson ScheduleLesson { get; set; } = null!;

        /// <summary>Դասին մասնակցող դասարանը</summary>
        public int GradeId { get; set; }

        /// <summary>Նավիգացիա դեպի <see cref="Grade"/>:</summary>
        public Grade Grade { get; set; } = null!;
    }
}