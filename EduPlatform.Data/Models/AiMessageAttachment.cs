namespace EduPlatform.Data.Models
{
    /// <summary>Հաղորդագրությանը կցված ֆայլ կամ ձայնագրություն</summary>
    public class AiMessageAttachment
    {
        public int Id { get; set; }

        public int MessageId { get; set; }
        public AiMessage Message { get; set; } = null!;

        public AiAttachmentKind Kind { get; set; }
        public string FileName { get; set; } = "";

        /// <summary>Ֆայլի բանալին սերվերի ֆայլային պահոցում (IAssignmentFileService)</summary>
        public string StoredFileName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public long SizeBytes { get; set; }

        /// <summary>Ձայնագրության տեքստը (բրաուզերի խոսքի ճանաչումից)՝ ԱԲ-ին տրվում է տեքստի տեսքով</summary>
        public string? Transcript { get; set; }
    }
}
