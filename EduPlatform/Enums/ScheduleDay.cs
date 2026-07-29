namespace EduPlatform.Models
{
    /// <summary>
    /// Շաբաթվա օրը դասացուցակում՝ Երկուշաբթի = 1 ... Կիրակի = 7:
    /// </summary>
    /// <remarks>
    /// Միտումնավոր չենք օգտագործում <see cref="System.DayOfWeek"/>-ը, որտեղ
    /// Կիրակին = 0: Փոխակերպումը տես <c>TeacherController.CurrentScheduleDay()</c>
    /// մեթոդում: Շաբաթ և Կիրակի օրերը ներառված են, քանի որ որոշ դպրոցներ
    /// այդ օրերին նույնպես դասեր ունեն:
    /// </remarks>
    public enum ScheduleDay
    {
        /// <summary>Երկուշաբթի</summary>
        Monday = 1,

        /// <summary>Երեքշաբթի</summary>
        Tuesday = 2,

        /// <summary>Չորեքշաբթի</summary>
        Wednesday = 3,

        /// <summary>Հինգշաբթի</summary>
        Thursday = 4,

        /// <summary>Ուրբաթ</summary>
        Friday = 5,

        /// <summary>Շաբաթ</summary>
        Saturday = 6,

        /// <summary>Կիրակի</summary>
        Sunday = 7
    }
}