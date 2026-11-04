namespace EduPlatform.Data.Models
{
    /// <summary>
    /// Աշակերտի կցած ֆայլը իր պատասխանին
    /// </summary>
    public class AssignmentSubmissionFile
    {
        public int Id { get; set; }

        public int RecipientId { get; set; }
        public AssignmentRecipient Recipient { get; set; } = null!;

        /// <summary>Ֆայլի սկզբնական անունը (ցուցադրելու համար)</summary>
        public string FileName { get; set; } = "";

        /// <summary>Սերվերի վրա պահված եզակի անունը</summary>
        public string StoredFileName { get; set; } = "";

        public string ContentType { get; set; } = "";

        public long SizeBytes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
