namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Առաջադրանքի գնահատման տեսակը (ընտրվում է միայն մեկը)
    /// </summary>
    /// <remarks>
    /// Գնահատման դաշտի կառուցվածքը կախված է այս արժեքից՝
    /// <list type="bullet">
    /// <item><see cref="Diagnostic"/>՝ «Նախնական միավոր» (թիվ) + «Հետադարձ կապ»</item>
    /// <item><see cref="Formative"/>՝ միայն «Հետադարձ կապ»</item>
    /// <item><see cref="CurrentScore"/>՝ «Ընթացիկ միավոր» (1–10) + «Հետադարձ կապ»</item>
    /// </list>
    /// </remarks>
    public enum AssignmentGradingType
    {
        /// <summary>Հայտորոշիչ</summary>
        Diagnostic = 1,

        /// <summary>Ձևավորող</summary>
        Formative = 2,

        /// <summary>Ընթացիկ միավորային</summary>
        CurrentScore = 3
    }
}
