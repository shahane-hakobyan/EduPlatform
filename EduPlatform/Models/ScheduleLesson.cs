namespace EduPlatform.Models
{
    /// <summary>
    /// Ուսուցչի շաբաթական դասացուցակի մեկ միավոր՝ կա՛մ իրական դաս, կա՛մ ազատ ժամ:
    /// </summary>
    /// <remarks>
    /// Ուսուցիչը կարող է ունենալ միայն մեկ դաս
    /// (ուսումնական տարի, օր, ժամի համար) համակցության դեպքում —
    /// ապահովվում է եզակի ինդեքսով <c>OnModelCreating</c>-ում:
    /// </remarks>
    public class ScheduleLesson
    {
        /// <summary>Դասի Id- ն բազայում</summary>
        public int Id { get; set; }

        /// <summary>Ուսուցչի Id-ն AuthServer-ում (OpenIddict «sub»)</summary>
        /// <remarks>
        /// Պահում ենք միայն Id- ն, ոչ թե ուսուցչի տվյալները —
        /// դրանք մնում են AuthServer-ում և բեռնվում են ըստ պահանջի:
        /// </remarks>
        public string TeacherUserId { get; set; } = "";

        /// <summary>Ուսուցչի Id-ն ԴՇՀ համակարգում</summary>
        /// <remarks>Դատարկ է մինչև AuthServer-ի հետ ինտեգրումը</remarks>
        public string? DshhTeacherId { get; set; }

        /// <summary>Ուսումնական տարին՝ «2025-2026»</summary>
        /// <remarks>
        /// Դասացուցակը կազմվում է ամեն տարի նորից: Տարին պահելով՝ նոր
        /// դասացուցակ կարելի է ստեղծել առանց նախորդը ջնջելու:
        /// </remarks>
        public string AcademicYear { get; set; } = "";

        /// <summary>Շաբաթվա օրը</summary>
        public ScheduleDay Day { get; set; }

        /// <summary>Դասի հերթական համարը օրվա մեջ (սկսած 1-ից)</summary>
        /// <remarks>
        /// Օգտագործվում է <c>teacher-schedule.js</c>-ում ժամի կանխադրված արժեքները
        /// հաշվելու համար՝ սկիզբ 08:15, 45 րոպե դաս, 5 րոպե ընդմիջում,
        /// բացի 4-րդ դասից առաջ եղող 20 րոպեանոց ընդմիջումից:
        /// </remarks>
        public int PeriodNumber { get; set; }

        /// <summary>Դասի սկիզբը</summary>
        public TimeOnly StartTime { get; set; }

        /// <summary>Դասի ավարտը</summary>
        public TimeOnly EndTime { get; set; }

        /// <summary><c>true</c>, եթե սա ազատ ժամ է</summary>
        /// <remarks>
        /// Բացահայտ դաշտ է, ոչ թե դատարկ (null) առարկայից կամ դասարանից
        /// ենթադրվող վիճակ, որպեսզի իմաստը միանշանակ լինի:
        /// Ազատ ժամը զբաղեցնում է նույն 45 րոպեանոց հատվածը, բայց
        /// չունի առարկա, դասարան կամ լսարան:
        /// </remarks>
        public bool IsFree { get; set; }

        /// <summary>Լսարանի համարը</summary>
        /// <remarks>
        /// Ուսուցիչը մուտքագրում է ձեռքով, ցանկից չի ընտրում
        /// Ազատ ժամի դեպքում՝ դատարկ:
        /// </remarks>
        public string? RoomNumber { get; set; }

        /// <summary>Լրացուցիչ նշում, ցուցադրվում է քարտի վրա</summary>
        /// <remarks>Միակ դաշտն է, որը թույլատրվում է նաև ազատ ժամի դեպքում</remarks>
        public string? Notes { get; set; }

        /// <summary>Այս դասին դասավանդվող առարկաները</summary>
        /// <remarks>
        /// Մեկ դասը կարող է ընդգրկել մի քանի առարկա և մի քանի դասարան
        /// (օր․՝ 10Ա և 10Բ միասին): Առարկաների և դասարանների ցանկերն
        /// անկախ են իրարից — տես <see cref="LessonSubject"/>:
        /// </remarks>
        public List<LessonSubject> LessonSubjects { get; set; } = new();

        /// <summary>Այս դասին մասնակցող դասարանները</summary>
        public List<LessonGrade> LessonGrades { get; set; } = new();
    }
}