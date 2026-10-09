namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Որ դասարանում է աշակերտը՝ ըստ ուսումնական տարվա
    /// </summary>
    /// <remarks>
    /// Աշակերտը տվյալ տարում գտնվում է ուղիղ մեկ դասարանում, բայց տարիների
    /// ընթացքում անցնում է 9Ա → 10Ա → 11Ա, ուստի տարին մաս է կազմում բանալու,
    /// և տողը չի վերագրվում ամեն սեպտեմբերին:
    /// Առաջնային բանալին՝ <c>(PupilUserId, GradeId, AcademicYear)</c>:
    /// </remarks>
    public class PupilGrade
    {
        /// <summary>Աշակերտի Id-ն AuthServer-ում («sub»)</summary>
        public string PupilUserId { get; set; } = "";

        /// <summary>Աշակերտի Id-ն ԴՇՀ համակարգում</summary>
        /// <remarks>Դատարկ է մինչև AuthServer-ի հետ ինտեգրումը</remarks>
        public string? DshhPupilId { get; set; }

        /// <summary>Դասարանը, որտեղ սովորում է աշակերտը</summary>
        public int GradeId { get; set; }

        /// <summary>Նավիգացիա դեպի <see cref="Grade"/>:</summary>
        public Grade Grade { get; set; } = null!;

        /// <summary>Ուսումնական տարին՝ «2025-2026»</summary>
        public string AcademicYear { get; set; } = "";
    }
}