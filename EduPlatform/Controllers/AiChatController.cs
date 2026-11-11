using EduPlatform.Data;
using EduPlatform.Data.Models;
using EduPlatform.Resources;
using EduPlatform.Services;
using EduPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace EduPlatform.Controllers
{
    /// <summary>
    /// «ԱԲ զրուցարան»՝ զրույցներ Claude-ի հետ։ Էջը՝ /Teacher/AIChat, ձախ մասում՝ նոր զրույց, ԱԲ ագենտներ, ամրացված և նախկին զրույցներ։
    /// Պատասխանը հոսում է (streaming) բրաուզեր՝ տեքստային կտորներով։
    /// </summary>
    [Authorize(Roles = "Teacher")]
    [Route("Teacher/AIChat")]
    public class AiChatController : Controller
    {
        private const int MaxFiles = 5;
        private const long MaxFileBytes = 10L * 1024 * 1024;
        private const int MaxTextLength = 8000;
        private const int MaxTranscriptLength = 20000;

        // Պատասխանի հոսքում սխալը նշելու նշան (բրաուզերը փնտրում է այն)
        private const string ErrorMarker = "\u001eERROR";

        // Թույլատրվող ֆայլերը՝ ընդլայնում → content type (սերվերն է որոշում, ոչ թե բրաուզերը)
        private static readonly Dictionary<string, string> FileTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".webp"] = "image/webp",
            [".txt"] = "text/plain",
            [".md"] = "text/plain",
            [".csv"] = "text/plain",
            [".json"] = "text/plain",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        };

        private static readonly Dictionary<string, string> AudioTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".webm"] = "audio/webm",
            [".ogg"] = "audio/ogg",
            [".mp3"] = "audio/mpeg",
            [".m4a"] = "audio/mp4",
            [".mp4"] = "audio/mp4",
            [".wav"] = "audio/wav",
        };

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IStringLocalizer<SharedResource> _l;
        private readonly EduPlatformDbContext _db;
        private readonly IClaudeChatService _claude;
        private readonly IFileStorageService _files;
        private readonly ILogger<AiChatController> _log;
        private readonly int _dailyLimit;
        private readonly int _maxHistory;

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public AiChatController(ISidebarMenuBuilder menuBuilder, IStringLocalizer<SharedResource> localizer,
            EduPlatformDbContext db, IClaudeChatService claude, IFileStorageService files, ILogger<AiChatController> log,
            IConfiguration config)
        {
            _menuBuilder = menuBuilder;
            _l = localizer;
            _db = db;
            _claude = claude;
            _files = files;
            _log = log;

            // սահմանաչափեր (appsettings-ի «AiChat» բաժին)՝ 0 կամ բացասական արժեքը՝ առանց սահմանի
            _dailyLimit = config.GetValue("AiChat:DailyMessageLimit", 30);
            _maxHistory = Math.Max(2, config.GetValue("AiChat:MaxHistoryMessages", 40));
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.SidebarItems = _menuBuilder.ForTeacher();
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForTeacher();
            base.OnActionExecuting(context);
        }

        // ── Էջ ──────────────────────────────────────────────────────────────

        [HttpGet("")]
        public IActionResult Index(int? c)
        {
            var vm = new AiChatPageViewModel
            {
                InitialConversationId = c,
                Agents = AiAgents.All.Select(a => new AiAgentOption(a.Key, _l[a.NameKey].Value)).ToList()
            };
            return View(vm);
        }

        // ── Զրույցների ցուցակը (ձախ մաս) ────────────────────────────────────

        [HttpGet("Conversations")]
        public async Task<IActionResult> Conversations()
        {
            var rows = await _db.AiConversations
                .Where(c => c.UserId == CurrentUserId)
                .OrderByDescending(c => c.UpdatedAt)
                .Take(200)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.IsPinned,
                    c.AgentKey,
                    c.UpdatedAt,
                    Preview = c.Messages.OrderByDescending(m => m.Id).Select(m => m.Text).FirstOrDefault()
                })
                .ToListAsync();

            var items = rows.Select(c => new
            {
                c.Id,
                c.Title,
                c.IsPinned,
                c.AgentKey,
                c.UpdatedAt,
                Preview = Shorten(c.Preview, 80)
            }).ToList();

            return Json(new
            {
                pinned = items.Where(i => i.IsPinned),
                recent = items.Where(i => !i.IsPinned)
            }, JsonOptions);
        }

        [HttpGet("Conversations/{id:int}")]
        public async Task<IActionResult> GetConversation(int id)
        {
            var conv = await _db.AiConversations
                .Where(c => c.Id == id && c.UserId == CurrentUserId)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.AgentKey,
                    Messages = c.Messages.OrderBy(m => m.Id).Select(m => new
                    {
                        m.Id,
                        Role = m.Role == AiMessageRole.User ? "user" : "assistant",
                        m.Text,
                        m.CreatedAt,
                        m.EditedAt,
                        Attachments = m.Attachments.OrderBy(a => a.Id).Select(a => new
                        {
                            a.Id,
                            Kind = a.Kind == AiAttachmentKind.Audio ? "audio" : "file",
                            a.FileName,
                            a.ContentType,
                            a.SizeBytes
                        })
                    })
                })
                .FirstOrDefaultAsync();

            if (conv == null) return NotFound();
            return Json(conv, JsonOptions);
        }

        public record PinRequest(bool Pinned);
        public record RenameRequest(string? Title);

        [HttpPost("Conversations/{id:int}/Pin")]
        public async Task<IActionResult> Pin(int id, [FromBody] PinRequest req)
        {
            var pinnedAt = req.Pinned ? DateTime.Now : (DateTime?)null;
            var affected = await _db.AiConversations
                .Where(c => c.Id == id && c.UserId == CurrentUserId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsPinned, req.Pinned)
                    .SetProperty(c => c.PinnedAt, pinnedAt));
            return affected > 0 ? Ok() : NotFound();
        }

        [HttpPost("Conversations/{id:int}/Rename")]
        public async Task<IActionResult> Rename(int id, [FromBody] RenameRequest req)
        {
            var title = (req.Title ?? "").Trim();
            if (title.Length == 0 || title.Length > 200) return BadRequest();

            var affected = await _db.AiConversations
                .Where(c => c.Id == id && c.UserId == CurrentUserId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Title, title));
            return affected > 0 ? Ok() : NotFound();
        }

        [HttpPost("Conversations/{id:int}/Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            var conv = await _db.AiConversations
                .Include(c => c.Messages).ThenInclude(m => m.Attachments)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
            if (conv == null) return NotFound();

            var files = conv.Messages.SelectMany(m => m.Attachments).Select(a => a.StoredFileName).ToList();

            _db.AiConversations.Remove(conv);   // հաղորդագրություններն ու կցումները ջնջվում են cascade-ով
            await _db.SaveChangesAsync();

            foreach (var f in files) _files.Delete(f);
            return Ok();
        }

        // ── Կցված ֆայլ (դիտել/ներբեռնել) ────────────────────────────────────

        [HttpGet("Attachments/{id:int}")]
        public async Task<IActionResult> Attachment(int id, bool download = false)
        {
            var a = await _db.AiMessageAttachments
                .Where(x => x.Id == id && x.Message.Conversation.UserId == CurrentUserId)
                .FirstOrDefaultAsync();
            if (a == null) return NotFound();

            var stream = _files.OpenRead(a.StoredFileName);
            if (stream == null) return NotFound();

            // միայն անվտանգ տեսակներ են՝ բրաուզերում բացվում են, մնացածը՝ ներբեռնվում
            var viewable = a.ContentType.StartsWith("image/") || a.ContentType.StartsWith("audio/") || a.ContentType == "application/pdf" || a.ContentType == "text/plain";
            return download || !viewable
                ? File(stream, a.ContentType, a.FileName)
                : File(stream, a.ContentType);
        }

        // ── Օրական սահմանաչափ՝ մեկ օգտատիրոջ Claude-ին արված հարցումների քանակը (պատասխանները, որ ստեղծվել են այսօր) ──

        private async Task<IActionResult?> CheckDailyLimitAsync(CancellationToken ct)
        {
            if (_dailyLimit <= 0) return null;

            var today = DateTime.Today;
            var used = await _db.AiMessages.CountAsync(m => m.Role == AiMessageRole.Assistant
                                                         && m.CreatedAt >= today
                                                         && m.Conversation.UserId == CurrentUserId, ct);
            if (used < _dailyLimit) return null;

            return StatusCode(StatusCodes.Status429TooManyRequests, new { limit = _dailyLimit, used });
        }

        // ── Հաղորդագրություն ուղարկել ───────────────────────────────────────

        public class AiSendRequest
        {
            public int? ConversationId { get; set; }
            public string? AgentKey { get; set; }
            public string? Text { get; set; }
            public List<IFormFile> Attachments { get; set; } = new();

            /// <summary>JSON՝ [{"kind":"audio","transcript":"..."}], նույն հերթականությամբ, ինչ Attachments</summary>
            public string? AttachmentMeta { get; set; }
        }

        public record AttachmentMeta(string? Kind, string? Transcript);

        [HttpPost("Send")]
        [RequestSizeLimit(80_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 80_000_000)]
        public async Task<IActionResult> Send([FromForm] AiSendRequest req, CancellationToken ct)
        {
            var limited = await CheckDailyLimitAsync(ct);
            if (limited != null) return limited;

            var text = (req.Text ?? "").Trim();
            var files = req.Attachments.Where(f => f.Length > 0).ToList();

            if (text.Length > MaxTextLength) return BadRequest();
            if (text.Length == 0 && files.Count == 0) return BadRequest();
            if (files.Count > MaxFiles) return BadRequest();

            var meta = new List<AttachmentMeta>();
            if (!string.IsNullOrWhiteSpace(req.AttachmentMeta))
            {
                try { meta = JsonSerializer.Deserialize<List<AttachmentMeta>>(req.AttachmentMeta, JsonOptions) ?? new(); }
                catch (JsonException) { return BadRequest(); }
            }

            // ստուգում՝ ընդլայնում և չափ
            var plan = new List<(IFormFile File, AiAttachmentKind Kind, string ContentType)>();
            for (var i = 0; i < files.Count; i++)
            {
                var ext = Path.GetExtension(files[i].FileName);
                var wantsAudio = i < meta.Count && string.Equals(meta[i].Kind, "audio", StringComparison.OrdinalIgnoreCase);

                if (files[i].Length > MaxFileBytes) return BadRequest();

                if (wantsAudio && AudioTypes.TryGetValue(ext, out var audioType))
                    plan.Add((files[i], AiAttachmentKind.Audio, audioType));
                else if (FileTypes.TryGetValue(ext, out var fileType))
                    plan.Add((files[i], AiAttachmentKind.File, fileType));
                else
                    return BadRequest();
            }

            var now = DateTime.Now;

            AiConversation conv;
            if (req.ConversationId is int cid)
            {
                var existing = await _db.AiConversations.FirstOrDefaultAsync(c => c.Id == cid && c.UserId == CurrentUserId, ct);
                if (existing == null) return NotFound();
                conv = existing;
            }
            else
            {
                conv = new AiConversation
                {
                    UserId = CurrentUserId,
                    AgentKey = AiAgents.Find(req.AgentKey)?.Key,
                    Title = MakeTitle(text, files),
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _db.AiConversations.Add(conv);
            }

            var (regionId, schoolId) = await TeacherLocationAsync();

            var stored = new List<string>();
            AiMessage userMsg;
            try
            {
                var attachments = new List<AiMessageAttachment>();
                for (var i = 0; i < plan.Count; i++)
                {
                    var (file, kind, contentType) = plan[i];
                    var key = await _files.SaveAsync(FileSections.AiChat, "teacher", CurrentUserId, regionId, schoolId, file);
                    stored.Add(key);

                    var transcript = kind == AiAttachmentKind.Audio && i < meta.Count ? meta[i].Transcript?.Trim() : null;
                    if (transcript is { Length: > MaxTranscriptLength }) transcript = transcript[..MaxTranscriptLength];

                    attachments.Add(new AiMessageAttachment
                    {
                        Kind = kind,
                        FileName = Shorten(Path.GetFileName(file.FileName), 255),
                        StoredFileName = key,
                        ContentType = contentType,
                        SizeBytes = file.Length,
                        Transcript = string.IsNullOrWhiteSpace(transcript) ? null : transcript
                    });
                }

                userMsg = new AiMessage
                {
                    Conversation = conv,
                    Role = AiMessageRole.User,
                    Text = text,
                    CreatedAt = now,
                    Attachments = attachments
                };
                _db.AiMessages.Add(userMsg);
                conv.UpdatedAt = now;
                await _db.SaveChangesAsync(ct);
            }
            catch
            {
                foreach (var key in stored) _files.Delete(key);
                throw;
            }

            return await StreamReplyAsync(conv, userMsg, ct);
        }

        // ── Օգտատիրոջ հաղորդագրությունը խմբագրել՝ պատասխանը նորից ստեղծվում է ──

        public record EditRequest(string? Text);

        [HttpPost("Messages/{id:int}/Edit")]
        public async Task<IActionResult> EditMessage(int id, [FromBody] EditRequest req, CancellationToken ct)
        {
            var limited = await CheckDailyLimitAsync(ct);
            if (limited != null) return limited;

            var text = (req.Text ?? "").Trim();
            if (text.Length == 0 || text.Length > MaxTextLength) return BadRequest();

            var msg = await _db.AiMessages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == id
                                       && m.Role == AiMessageRole.User
                                       && m.Conversation.UserId == CurrentUserId, ct);
            if (msg == null) return NotFound();

            // խմբագրված հաղորդագրությունից հետո եղած ամեն ինչ ջնջվում է (պատասխանը նորից կստեղծվի)
            var later = await _db.AiMessages
                .Include(m => m.Attachments)
                .Where(m => m.ConversationId == msg.ConversationId && m.Id > msg.Id)
                .ToListAsync(ct);
            var laterFiles = later.SelectMany(m => m.Attachments).Select(a => a.StoredFileName).ToList();

            _db.AiMessages.RemoveRange(later);
            msg.Text = text;
            msg.EditedAt = DateTime.Now;
            msg.Conversation.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            foreach (var f in laterFiles) _files.Delete(f);

            return await StreamReplyAsync(msg.Conversation, msg, ct);
        }

        // ── Claude-ի պատասխանը՝ հոսքով ───────────────────────────────────────

        private async Task<IActionResult> StreamReplyAsync(AiConversation conv, AiMessage userMsg, CancellationToken ct)
        {
            // Claude-ին տրվում են միայն վերջին N հաղորդագրությունները (երկար զրույցը թանկ է)
            var history = await _db.AiMessages
                .Include(m => m.Attachments)
                .Where(m => m.ConversationId == conv.Id)
                .OrderByDescending(m => m.Id)
                .Take(_maxHistory)
                .ToListAsync(ct);
            history.Reverse();
            while (history.Count > 0 && history[0].Role != AiMessageRole.User) history.RemoveAt(0);   // պատմությունը պետք է սկսվի օգտատիրոջ հաղորդագրությամբ

            var turns = await BuildTurnsAsync(history);

            var assistant = new AiMessage
            {
                ConversationId = conv.Id,
                Role = AiMessageRole.Assistant,
                Text = "",
                CreatedAt = DateTime.Now
            };
            _db.AiMessages.Add(assistant);
            await _db.SaveChangesAsync(ct);

            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = "text/plain; charset=utf-8";
            Response.Headers["Cache-Control"] = "no-store";
            Response.Headers["X-Accel-Buffering"] = "no";

            // առաջին տողը՝ JSON (զրույցի և հաղորդագրությունների Id-ները), հետո՝ պատասխանի տեքստը
            var meta = new
            {
                conversationId = conv.Id,
                title = conv.Title,
                agentKey = conv.AgentKey,
                userMessage = new
                {
                    id = userMsg.Id,
                    attachments = userMsg.Attachments.OrderBy(a => a.Id).Select(a => new
                    {
                        a.Id,
                        kind = a.Kind == AiAttachmentKind.Audio ? "audio" : "file",
                        a.FileName,
                        a.ContentType,
                        a.SizeBytes
                    })
                },
                assistantMessageId = assistant.Id
            };

            var sb = new StringBuilder();
            var failed = false;
            try
            {
                await Response.WriteAsync(JsonSerializer.Serialize(meta, JsonOptions) + "\n", ct);
                await Response.Body.FlushAsync(ct);

                var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                await foreach (var chunk in _claude.StreamReplyAsync(conv.AgentKey, language, turns, ct))
                {
                    sb.Append(chunk);
                    await Response.WriteAsync(chunk, ct);
                    await Response.Body.FlushAsync(ct);
                }
            }
            catch (OperationCanceledException)
            {
                // օգտատերը դադարեցրեց կամ փակեց էջը՝ մինչ այդ ստացված տեքստը պահվում է
            }
            catch (Exception ex)
            {
                failed = true;
                _log.LogError(ex, "AI chat: Claude request failed (conversation {ConversationId})", conv.Id);
            }
            finally
            {
                if (sb.Length == 0)
                {
                    _db.AiMessages.Remove(assistant);
                }
                else
                {
                    assistant.Text = sb.ToString();
                }
                conv.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync(CancellationToken.None);
            }

            if (failed)
            {
                try
                {
                    await Response.WriteAsync(ErrorMarker, CancellationToken.None);
                    await Response.Body.FlushAsync(CancellationToken.None);
                }
                catch { /* կապն արդեն փակ է */ }
            }

            return new EmptyResult();
        }

        // Claude-ին՝ միայն վերջին օգտատիրոջ հաղորդագրության ֆայլերի բովանդակությունն է տրվում
        private async Task<List<ChatTurnInput>> BuildTurnsAsync(List<AiMessage> history)
        {
            var lastUserId = history.LastOrDefault(m => m.Role == AiMessageRole.User)?.Id;
            var turns = new List<ChatTurnInput>();

            foreach (var m in history)
            {
                var attachments = new List<ChatAttachmentInput>();
                foreach (var a in m.Attachments.OrderBy(a => a.Id))
                {
                    byte[]? bytes = null;
                    if (m.Id == lastUserId && a.Kind == AiAttachmentKind.File)
                        bytes = await ReadAllBytesAsync(a.StoredFileName);

                    attachments.Add(new ChatAttachmentInput(a.Kind, a.FileName, a.ContentType, bytes, a.Transcript));
                }
                turns.Add(new ChatTurnInput(m.Role, m.Text, attachments));
            }

            return turns;
        }

        private async Task<byte[]?> ReadAllBytesAsync(string key)
        {
            await using var stream = _files.OpenRead(key);
            if (stream == null) return null;

            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }

        // ── Օգնականներ ───────────────────────────────────────────────────────

        // Ուսուցչի դպրոցը՝ նրա վերջին ուսումնական տարվա դասարաններից (նույնը, ինչ առաջադրանքներում)։
        // Մարզի Id-ն դեռ չկա (կգա AuthServer-ից) → 0։
        private async Task<(int RegionId, int SchoolId)> TeacherLocationAsync()
        {
            var schoolId = await _db.TeacherGrades
                .Where(tg => tg.TeacherUserId == CurrentUserId)
                .OrderByDescending(tg => tg.AcademicYear)
                .Select(tg => tg.Grade.InstitutionId)
                .FirstOrDefaultAsync();

            return (0, schoolId);
        }

        private string MakeTitle(string text, List<IFormFile> files)
        {
            var firstLine = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstLine)) return Shorten(firstLine, 60)!;
            if (files.Count > 0) return Shorten(Path.GetFileName(files[0].FileName), 60)!;
            return _l["AiChatNewTitle"].Value;
        }

        private static string? Shorten(string? s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            s = s.Replace('\n', ' ').Trim();
            return s.Length <= max ? s : s[..(max - 1)] + "…";
        }
    }
}
