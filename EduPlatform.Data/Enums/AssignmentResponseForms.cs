namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Պատասխանի ներկայացման ձևը (կարելի է ընտրել երկուսը միասին)
    /// </summary>
    [Flags]
    public enum AssignmentResponseForms
    {
        None = 0,

        /// <summary>Տեքստային պատասխան</summary>
        Text = 1,

        /// <summary>Կցված ֆայլ</summary>
        File = 2
    }
}
