namespace EduPlatform.Data.Models
{
    /// <summary>Զրույցի մեկ հաղորդագրություն (օգտատիրոջ կամ ԱԲ-ի)</summary>
    public class AiMessage
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }
        public AiConversation Conversation { get; set; } = null!;

        public AiMessageRole Role { get; set; }
        public string Text { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Երբ է օգտատերը խմբագրել իր հաղորդագրությունը (null՝ չի խմբագրվել)</summary>
        public DateTime? EditedAt { get; set; }

        public List<AiMessageAttachment> Attachments { get; set; } = new();
    }
}
