namespace EduPlatform.ViewModels
{
    /// <summary>ԱԲ ագենտ՝ ցուցադրվող անունով (ընթացիկ լեզվով)</summary>
    public record AiAgentOption(string Key, string Name);

    public class AiChatPageViewModel
    {
        public List<AiAgentOption> Agents { get; set; } = new();

        /// <summary>Բացվող զրույցը (?c=...), եթե կա</summary>
        public int? InitialConversationId { get; set; }
    }
}
