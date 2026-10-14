namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Օգտատիրոջ «Իմ գրադարան»-ում պահված Library նյութը (միայն հղում, ոչ պատճեն)
    /// </summary>
    /// <remarks>
    /// Library-ի տվյալների բազայում օգտատերերի մասին ոչինչ չի պահվում.
    /// Նյութի տվյալները ստացվում են Library.Api-ից՝ <see cref="LibraryMaterialId"/>-ով:
    /// </remarks>
    public class UserLibraryMaterial
    {
        public int Id { get; set; }

        /// <summary>Օգտատիրոջ Id-ն AuthServer-ում («sub»)</summary>
        /// <remarks>Նույնն է, ինչ <see cref="ScheduleLesson.TeacherUserId"/>:</remarks>
        public string UserId { get; set; } = "";

        /// <summary>
        /// Library-ի նյութի տեսակը (MaterialKind)՝
        /// Textbook, MethodicalGuide, OtherMaterial, OtherBook
        /// </summary>
        public string MaterialType { get; set; } = "";

        /// <summary>Library-ի <c>EducationalMaterial.Id</c></summary>
        public int LibraryMaterialId { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }
}
