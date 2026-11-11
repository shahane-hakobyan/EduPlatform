using System.Security.Cryptography;
using System.Text;

namespace EduPlatform.Services
{
    /// <summary>Ֆայլային պահոցի բաժինները՝ արմատի ներսում առանձին թղթապանակներ (նոր բաժին՝ ավելացրեք այստեղ)</summary>
    public static class FileSections
    {
        /// <summary>Առաջադրանքների ֆայլեր և սովորողների պատասխանների ֆայլեր</summary>
        public const string Assignments = "assignments";

        /// <summary>ԱԲ զրուցարանի կցված ֆայլեր և ձայնագրություններ</summary>
        public const string AiChat = "ai-chat";
    }

    /// <summary>
    /// Ընդհանուր ֆայլային պահոց՝ սերվերի սկավառակի վրա, wwwroot-ից դուրս։ Բոլոր բաժինների համար (առաջադրանքներ, ԱԲ զրուցարան, ...) նույն ծառայությունը։
    /// Կառուցվածքը՝ {RootPath}/{բաժին}/{մարզի Id}/{դպրոցի Id}/{սեփականատիրոջ թղթապանակ}/{պատահական անուն}.{ընդլայնում}
    /// Օրինակ՝ E:\EduPlatform\assignments\0\12\teacher-9f3a1c27\a1b2c3....pdf
    ///         E:\EduPlatform\ai-chat\0\12\teacher-9f3a1c27\d4e5f6....png
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>Պահպանում է ֆայլը և վերադարձնում հարաբերական բանալին («/»-ներով), որը գրվում է բազայում (StoredFileName)։</summary>
        /// <param name="section">Բաժինը՝ տես FileSections (օր․ FileSections.Assignments)</param>
        /// <param name="ownerPrefix">«teacher» կամ «pupil»՝ թղթապանակի անվան սկիզբը</param>
        /// <param name="ownerUserId">Ֆայլի սեփականատիրոջ UserId-ն</param>
        /// <param name="regionId">Մարզի Id-ն (0՝ եթե դեռ հայտնի չէ)</param>
        /// <param name="schoolId">Դպրոցի Id-ն (0՝ եթե հայտնի չէ)</param>
        Task<string> SaveAsync(string section, string ownerPrefix, string ownerUserId, int regionId, int schoolId, IFormFile file);

        /// <summary>Բացում է ֆայլը կարդալու համար։ null՝ եթե ֆայլը չկա կամ բանալին անվտանգ չէ։</summary>
        FileStream? OpenRead(string key);

        /// <summary>Ջնջում է ֆայլը (սխալները անտեսվում են)։</summary>
        void Delete(string key);
    }

    /// <remarks>
    /// Կարգավորումներ՝ FileStorage:RootPath, FileStorage:FolderSalt, FileStorage:LegacyRootPath։
    /// Եթե դրանք չկան՝ օգտագործվում են նախկին AssignmentFiles:RootPath / FolderSalt / LegacyRootPath (appsettings-ը փոխել պարտադիր չէ)։
    /// </remarks>
    public class FileStorageService : IFileStorageService
    {
        // StoredFileName սյունը բազայում 100 նիշ է
        private const int MaxKeyLength = 100;

        private readonly string _root;
        private readonly string _legacyRoot;
        private readonly byte[] _salt;

        public FileStorageService(IConfiguration config, IWebHostEnvironment env)
        {
            var configuredRoot = Setting(config, "RootPath");
            if (string.IsNullOrWhiteSpace(configuredRoot))
                throw new InvalidOperationException("FileStorage:RootPath (or AssignmentFiles:RootPath) is not configured.");

            _root = Path.GetFullPath(configuredRoot);
            Directory.CreateDirectory(_root);

            // Հին ֆայլերը (առանց թղթապանակների)՝ նախկին վայրում
            var legacy = Setting(config, "LegacyRootPath");
            _legacyRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(legacy)
                ? Path.Combine(env.ContentRootPath, "App_Data", "assignment-files")
                : legacy);

            var salt = Setting(config, "FolderSalt");
            if (string.IsNullOrWhiteSpace(salt))
                throw new InvalidOperationException("FileStorage:FolderSalt (or AssignmentFiles:FolderSalt) is not configured.");
            _salt = Encoding.UTF8.GetBytes(salt);
        }

        // «FileStorage:*» կարգավորումը, եթե չկա՝ նախկին «AssignmentFiles:*»-ը
        private static string? Setting(IConfiguration config, string name)
        {
            var value = config["FileStorage:" + name];
            return string.IsNullOrWhiteSpace(value) ? config["AssignmentFiles:" + name] : value;
        }

        // Օգտատիրոջ թղթապանակը՝ «teacher-9f3a1c27» (UserId-ն ուղիղ չի երևում, բայց նույն օգտատերը միշտ նույն թղթապանակն ունի)
        private string OwnerFolder(string ownerPrefix, string ownerUserId)
        {
            var hash = HMACSHA256.HashData(_salt, Encoding.UTF8.GetBytes(ownerUserId));
            return $"{ownerPrefix}-{Convert.ToHexString(hash, 0, 4).ToLowerInvariant()}";
        }

        public async Task<string> SaveAsync(string section, string ownerPrefix, string ownerUserId, int regionId, int schoolId, IFormFile file)
        {
            // բաժինը՝ միայն փոքր տառեր, թվեր և «-» (path traversal-ից պաշտպանություն)
            if (string.IsNullOrWhiteSpace(section) || !section.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
                throw new ArgumentException("Invalid file section.", nameof(section));

            // ընդլայնումը՝ միայն փոքր տառեր և թվեր (օրինակ՝ .pdf)
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext.Length > 6 || !ext.Skip(1).All(c => char.IsAsciiLetterOrDigit(c))) ext = "";

            var folder = OwnerFolder(ownerPrefix, ownerUserId);
            var key = $"{section}/{regionId}/{schoolId}/{folder}/{Guid.NewGuid():N}{ext}";
            if (key.Length > MaxKeyLength)
                throw new InvalidOperationException("Stored file key is longer than the database column allows.");

            var finalPath = ResolveInRoot(_root, key)
                ?? throw new InvalidOperationException("Invalid storage key.");
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

            // նախ գրում ենք .tmp ֆայլ, հետո վերանվանում ենք (ինչպես Library-ում՝ DFS-ը անտեսում է *.tmp ֆայլերը)
            var tempPath = finalPath + ".tmp";
            try
            {
                await using (var target = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                    await file.CopyToAsync(target);

                File.Move(tempPath, finalPath);
            }
            catch
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                throw;
            }

            return key;
        }

        public FileStream? OpenRead(string key)
        {
            var path = ResolvePath(key);
            if (path == null || !File.Exists(path)) return null;

            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        }

        public void Delete(string key)
        {
            try
            {
                var path = ResolvePath(key);
                if (path != null && File.Exists(path)) File.Delete(path);
            }
            catch { }
        }

        // նոր ֆայլեր՝ թղթապանակներով (բանալում կա «/»), հին ֆայլեր՝ միայն անունով՝ հին վայրում
        private string? ResolvePath(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            if (key.Contains('/') || key.Contains('\\'))
                return ResolveInRoot(_root, key);

            return ResolveInRoot(_legacyRoot, key);
        }

        // path traversal-ից պաշտպանություն՝ արդյունքը պետք է մնա արմատի ներսում
        private static string? ResolveInRoot(string root, string key)
        {
            var full = Path.GetFullPath(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)));
            return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? full : null;
        }
    }
}
