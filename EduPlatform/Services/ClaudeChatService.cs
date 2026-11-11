using Anthropic;
using Anthropic.Models.Messages;
using AnthropicMediaType = Anthropic.Models.Messages.MediaType;   // «MediaType» անվանումը ASP.NET-ի նույնանուն տիպի հետ չշփոթելու համար
using EduPlatform.Data.Models;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace EduPlatform.Services
{
    /// <summary>Հաղորդագրությանը կցված ֆայլ՝ Claude-ին փոխանցելու համար</summary>
    /// <param name="Bytes">Ֆայլի բովանդակությունը (null՝ եթե այս հաղորդագրության ֆայլերը չեն ուղարկվում, միայն նշվում է անունը)</param>
    public record ChatAttachmentInput(AiAttachmentKind Kind, string FileName, string ContentType, byte[]? Bytes, string? Transcript);

    /// <summary>Զրույցի մեկ հաղորդագրություն՝ Claude-ին փոխանցելու համար</summary>
    public record ChatTurnInput(AiMessageRole Role, string Text, IReadOnlyList<ChatAttachmentInput> Attachments);

    public interface IClaudeChatService
    {
        /// <summary>Վերադարձնում է Claude-ի պատասխանը տեքստային կտորներով (streaming)։</summary>
        /// <param name="language">Միջերեսի լեզուն՝ «hy», «ru», «en» (եթե օգտատերը այլ լեզվով է գրում՝ Claude-ը նրա լեզվով կպատասխանի)</param>
        IAsyncEnumerable<string> StreamReplyAsync(string? agentKey, string language, IReadOnlyList<ChatTurnInput> turns, CancellationToken ct = default);
    }

    /// <summary>
    /// «ԱԲ զրուցարան»՝ Claude API-ի միջոցով (պաշտոնական Anthropic SDK)։
    /// Կարգավորումներ՝ Anthropic:ApiKey (պարտադիր), Anthropic:Model (կանխադրված՝ claude-sonnet-5-5), Anthropic:MaxTokens (կանխադրված՝ 8000)։
    /// </summary>
    public class ClaudeChatService : IClaudeChatService
    {
        private const int MaxExtractedChars = 200_000;

        private readonly AnthropicClient _client;
        private readonly string _model;
        private readonly int _maxTokens;

        public ClaudeChatService(IConfiguration config)
        {
            var apiKey = config["Anthropic:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Anthropic:ApiKey is not configured.");

            _client = new AnthropicClient { ApiKey = apiKey };
            _model = config["Anthropic:Model"] is { Length: > 0 } m ? m : "claude-sonnet-5-5";
            _maxTokens = config.GetValue("Anthropic:MaxTokens", 8000);
        }

        public async IAsyncEnumerable<string> StreamReplyAsync(
            string? agentKey, string language, IReadOnlyList<ChatTurnInput> turns,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var parameters = new MessageCreateParams
            {
                Model = _model,
                MaxTokens = _maxTokens,
                System = BuildSystemPrompt(agentKey, language),
                Messages = BuildMessages(turns),
                OutputConfig = new OutputConfig { Effort = Effort.Medium },
            };

            await foreach (RawMessageStreamEvent streamEvent in _client.Messages.CreateStreaming(parameters).WithCancellation(ct))
            {
                ct.ThrowIfCancellationRequested();

                if (streamEvent.TryPickContentBlockDelta(out var delta) &&
                    delta.Delta.TryPickText(out var text))
                {
                    yield return text.Text;
                }
            }
        }

        // ── Համակարգային հրահանգ ────────────────────────────────────────────

        private static string BuildSystemPrompt(string? agentKey, string language)
        {
            var languageName = language switch { "ru" => "Russian", "en" => "English", _ => "Armenian" };

            var sb = new StringBuilder();
            sb.AppendLine("You are the AI assistant of EduPlatform, a platform for school teachers in Armenia.");
            sb.AppendLine("Be accurate, practical and concise. Reply in the language of the user's message; if it is unclear, reply in " + languageName + ".");
            sb.AppendLine("Use Markdown (lists, tables, code blocks) when it helps. If you are not sure about something, say so instead of guessing.");
            sb.AppendLine("You cannot see the platform's data (assignments, pupils, grades) and must not pretend to. Do not ask for personal data of pupils.");

            var agent = AiAgents.Find(agentKey);
            if (agent != null)
            {
                sb.AppendLine();
                sb.AppendLine(agent.Prompt);
            }

            return sb.ToString();
        }

        // ── Հաղորդագրությունների կառուցում ──────────────────────────────────

        private static List<MessageParam> BuildMessages(IReadOnlyList<ChatTurnInput> turns)
        {
            var result = new List<MessageParam>();

            // ֆայլերի բովանդակությունը ուղարկվում է միայն վերջին օգտատիրոջ հաղորդագրության հետ
            var lastUser = -1;
            for (var i = 0; i < turns.Count; i++)
                if (turns[i].Role == AiMessageRole.User) lastUser = i;

            for (var i = 0; i < turns.Count; i++)
            {
                var turn = turns[i];

                if (turn.Role == AiMessageRole.Assistant)
                {
                    if (!string.IsNullOrWhiteSpace(turn.Text))
                        result.Add(new AssistantMessageParam { Content = turn.Text });
                    continue;
                }

                var blocks = new List<ContentBlockParam>();
                var extra = new StringBuilder();

                foreach (var a in turn.Attachments)
                {
                    if (!string.IsNullOrWhiteSpace(a.Transcript))
                        extra.AppendLine($"[Voice message \"{a.FileName}\", transcript]: {a.Transcript}");
                    else if (a.Kind == AiAttachmentKind.Audio)
                        extra.AppendLine($"[Voice message \"{a.FileName}\" was attached, but no transcript is available.]");
                    else if (i == lastUser && a.Bytes != null)
                        AddFile(blocks, extra, a);
                    else
                        extra.AppendLine($"[Attached file: {a.FileName}]");
                }

                var text = (turn.Text + (extra.Length > 0 ? "\n\n" + extra : "")).Trim();
                if (text.Length == 0) text = "(empty message)";

                blocks.Add(new TextBlockParam { Text = text });
                result.Add(new UserMessageParam { Content = blocks });
            }

            return result;
        }

        private static void AddFile(List<ContentBlockParam> blocks, StringBuilder extra, ChatAttachmentInput a)
        {
            var ext = Path.GetExtension(a.FileName).ToLowerInvariant();
            var bytes = a.Bytes!;

            switch (ext)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".gif":
                case ".webp":
                    blocks.Add(new ImageBlockParam
                    {
                        Source = new Base64ImageSource
                        {
                            Data = Convert.ToBase64String(bytes),
                            MediaType = ext switch
                            {
                                ".png" => AnthropicMediaType.ImagePng,
                                ".gif" => AnthropicMediaType.ImageGif,
                                ".webp" => AnthropicMediaType.ImageWebP,
                                _ => AnthropicMediaType.ImageJpeg
                            }
                        }
                    });
                    extra.AppendLine($"[Attached image: {a.FileName}]");
                    break;

                case ".pdf":
                    blocks.Add(new DocumentBlockParam { Source = new Base64PdfSource { Data = Convert.ToBase64String(bytes) } });
                    extra.AppendLine($"[Attached PDF: {a.FileName}]");
                    break;

                case ".txt":
                case ".md":
                case ".csv":
                case ".json":
                    AppendText(extra, a.FileName, Truncate(Encoding.UTF8.GetString(bytes)));
                    break;

                case ".docx":
                case ".xlsx":
                case ".pptx":
                    var office = ExtractOfficeText(ext, bytes);
                    if (string.IsNullOrWhiteSpace(office))
                        extra.AppendLine($"[Attached file \"{a.FileName}\" could not be read.]");
                    else
                        AppendText(extra, a.FileName, Truncate(office));
                    break;

                default:
                    extra.AppendLine($"[Attached file: {a.FileName}]");
                    break;
            }
        }

        private static void AppendText(StringBuilder sb, string fileName, string content)
        {
            sb.AppendLine($"[Content of the attached file \"{fileName}\"]");
            sb.AppendLine(content);
            sb.AppendLine("[End of file]");
        }

        private static string Truncate(string s) =>
            s.Length <= MaxExtractedChars ? s : s[..MaxExtractedChars] + "\n…(truncated)";

        // ── Word / Excel / PowerPoint ֆայլերի տեքստը (առանց լրացուցիչ գրադարանի) ──

        private static string? ExtractOfficeText(string ext, byte[] bytes)
        {
            try
            {
                using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);

                IEnumerable<ZipArchiveEntry> entries = ext switch
                {
                    ".docx" => zip.Entries.Where(e => e.FullName == "word/document.xml"),
                    ".xlsx" => zip.Entries.Where(e => e.FullName == "xl/sharedStrings.xml"),
                    ".pptx" => zip.Entries
                        .Where(e => e.FullName.StartsWith("ppt/slides/slide", StringComparison.Ordinal) && e.FullName.EndsWith(".xml", StringComparison.Ordinal))
                        .OrderBy(e => SlideNumber(e.FullName)),
                    _ => Enumerable.Empty<ZipArchiveEntry>()
                };

                var sb = new StringBuilder();
                foreach (var entry in entries)
                {
                    if (entry.Length > 30_000_000) continue;   // չափազանց մեծ XML՝ բաց ենք թողնում

                    using var stream = entry.Open();
                    var doc = XDocument.Load(stream);

                    // docx/pptx՝ պարբերություններ («p»), xlsx՝ տողեր («si»)
                    foreach (var block in doc.Descendants().Where(x => x.Name.LocalName is "p" or "si"))
                    {
                        var line = string.Concat(block.Descendants().Where(x => x.Name.LocalName == "t").Select(x => x.Value));
                        if (line.Length > 0) sb.AppendLine(line);
                        if (sb.Length > MaxExtractedChars) return sb.ToString();
                    }

                    if (ext == ".pptx") sb.AppendLine();
                }

                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static int SlideNumber(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path).Replace("slide", "");
            return int.TryParse(name, out var n) ? n : int.MaxValue;
        }
    }
}
