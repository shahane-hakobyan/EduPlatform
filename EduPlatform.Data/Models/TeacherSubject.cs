namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Որ առարկաներն է ուսուցիչը դասավանդում
    /// </summary>
    /// <remarks>
    /// Օգտագործվում է դասացուցակի խմբագրման պատուհանում առարկաների ցանկը
    /// լրացնելու համար, որպեսզի ուսուցիչը տեսնի միայն իր առարկաները:
    /// Առաջնային բանալին՝ <c>(TeacherUserId, SubjectId)</c>:
    /// </remarks>
    public class TeacherSubject
    {
        /// <summary>Ուսուցչի  Id-ն AuthServer-ում («sub»)</summary>
        /// <remarks>Նույնն է, ինչ <see cref="ScheduleLesson.TeacherUserId"/>:</remarks>
        public string TeacherUserId { get; set; } = "";

        /// <summary>Ուսուցչի  Id-ն ԴՇՀ համակարգում</summary>
        /// <remarks>Դատարկ է մինչև AuthServer-ի հետ ինտեգրումը</remarks>
        public string? DshhTeacherId { get; set; }

        /// <summary>Ուսուցչի դասավանդած առարկան</summary>
        public int SubjectId { get; set; }

        /// <summary>Նավիգացիա դեպի <see cref="Subject"/>:</summary>
        public Subject Subject { get; set; } = null!;
    }
}