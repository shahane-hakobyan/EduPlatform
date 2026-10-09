namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Կապող աղյուսակ՝ որ առարկաներն են դասավանդվում տվյալ դասին:
    /// </summary>
    /// <remarks>
    /// Միտումնավոր առանձին է <see cref="LessonGrade"/>-ից — երկուսն էլ անկախ
    /// բազմություններ են, ոչ թե զույգեր: Միավորելու դեպքում կստացվեր տող ամեն
    /// (առարկա × դասարան) համակցության համար և կենթադրվեր կապ, որը
    /// դասացուցակն իրականում չի պահում:
    /// Առաջնային բանալին՝ <c>(ScheduleLessonId, SubjectId)</c> — առանձին
    /// <c>Id</c> պետք չէ, և բանալին արգելում է նույն առարկան երկու անգամ
    /// ավելացնել նույն դասին:
    /// </remarks>
    public class LessonSubject
    {
        /// <summary>Դասը, որին վերաբերում է գրառումը</summary>
        public int ScheduleLessonId { get; set; }

        /// <summary>Նավիգացիա դեպի <see cref="ScheduleLesson"/>:</summary>
        public ScheduleLesson ScheduleLesson { get; set; } = null!;

        /// <summary>Դասավանդվող առարկան</summary>
        public int SubjectId { get; set; }

        /// <summary>Նավիգացիա դեպի <see cref="Subject"/>:</summary>
        public Subject Subject { get; set; } = null!;
    }
}