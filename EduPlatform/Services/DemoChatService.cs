using EduPlatform.Data.Models;
using System.Runtime.CompilerServices;
using System.Text;

namespace EduPlatform.Services
{
    /// <summary>
    /// ԴԵՄՈ ռեժիմ՝ Claude API-ի բանալի չկա։ Այս ծառայությունը ոչ մի արտաքին հարցում չի անում,
    /// այլ հոսքով վերադարձնում է պատրաստի օրինակելի պատասխան, որ կարողանաք տեսնել և փորձարկել ԱԲ զրուցարանի էջը։
    /// Ակտիվանում է միայն Development-ում (կամ DemoAutoLogin-ի դեպքում), երբ Anthropic:ApiKey-ը դատարկ է (տես Program.cs)։
    /// </summary>
    public class DemoChatService : IClaudeChatService
    {
        public async IAsyncEnumerable<string> StreamReplyAsync(
            string? agentKey, string language, IReadOnlyList<ChatTurnInput> turns,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var last = turns.LastOrDefault(t => t.Role == AiMessageRole.User);
            var question = string.IsNullOrWhiteSpace(last?.Text) ? "" : last!.Text.Trim();
            var agent = AiAgents.Find(agentKey);

            var sb = new StringBuilder();
            sb.AppendLine("### Դեմո պատասխան");
            sb.AppendLine();
            sb.AppendLine("Claude API-ի բանալի դեռ կարգավորված չէ, ուստի սա **պատրաստի օրինակելի պատասխան** է՝ միջերեսը փորձարկելու համար։");
            sb.AppendLine();

            if (agent != null) sb.AppendLine($"- Ընտրված ագենտ՝ `{agent.Key}`");
            if (question.Length > 0) sb.AppendLine($"- Ձեր հարցը՝ «{(question.Length > 120 ? question[..120] + "…" : question)}»");

            foreach (var a in last?.Attachments ?? Array.Empty<ChatAttachmentInput>())
                sb.AppendLine($"- Կցված՝ *{a.FileName}*" + (string.IsNullOrWhiteSpace(a.Transcript) ? "" : $" (տեքստ՝ «{a.Transcript}»)"));

            sb.AppendLine();
            sb.AppendLine("Իրական Claude-ը կպատասխանի ձեր հարցին։ Ահա Markdown-ի օրինակներ, որ կերևան պատասխանում՝");
            sb.AppendLine();
            sb.AppendLine("1. համարակալված ցուցակ");
            sb.AppendLine("2. **թավ** և *շեղ* տեքստ");
            sb.AppendLine();
            sb.AppendLine("| Դաս | Թեմա |");
            sb.AppendLine("|---|---|");
            sb.AppendLine("| 5 | Կոտորակներ |");
            sb.AppendLine("| 6 | Տոկոսներ |");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine("2 + 2 = 4");
            sb.AppendLine("```");

            // խոսքերով հոսք՝ իրական պատասխանի զգացողության համար
            foreach (var piece in SplitKeepingWhitespace(sb.ToString()))
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(25, ct);
                yield return piece;
            }
        }

        private static IEnumerable<string> SplitKeepingWhitespace(string text)
        {
            var current = new StringBuilder();
            foreach (var ch in text)
            {
                current.Append(ch);
                if (char.IsWhiteSpace(ch))
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }
            if (current.Length > 0) yield return current.ToString();
        }
    }
}
