namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Աշակերտի կարգավիճակը տվյալ առաջադրանքի համար
    /// </summary>
    public enum AssignmentRecipientStatus
    {
        /// <summary>Ուղարկված (աշակերտը դեռ չի ներկայացրել)</summary>
        Sent = 1,

        /// <summary>Կատարված (աշակերտը ներկայացրել է պատասխանը)</summary>
        Completed = 2,

        /// <summary>Գնահատված</summary>
        Graded = 3
    }
}
