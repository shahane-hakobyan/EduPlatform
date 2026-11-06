using System.Security.Cryptography;
using System.Text;

namespace EduPlatform.Services
{
    /// <summary>
    /// Առաջադրանքների ֆայլերի պահոց՝ սերվերի սկավառակի վրա, wwwroot-ից դուրս։
    /// Կառուցվածքը՝ {RootPath}/{օգտատիրոջ թղթապանակ}/{տարի}/{ամիս}/{օր}/{պատահական անուն}.{ընդլայնում}
    /// Օրինակ՝ E:\EduPlatformAssignments\teacher-9f3a1c27\2026\11\06\a1b2c3....pdf
    /// </summary>
    public interface IAssignmentFileService
    {
        /// <summary>Պահպանում է ֆայլը և վերադարձնում հարաբերական բանալին («/»-ներով), որը գրվում է բազայում (StoredFileName)։</summary>
        /// <param name="ownerUserId">Ֆայլի սեփականատիրոջ UserId-ն</param>
        /// <param name="ownerPrefix">«teacher» կամ «pupil»՝ թղթապանակի անվան սկիզբը</param>
        Task<string> SaveAsync(string ownerUserId, string ownerPrefix, IFormFile file, DateTime now);

        /// <summary>Բացում է ֆայլը կարդալու համար։ null՝ եթե ֆայլը չկա կամ բանալին անվտանգ չէ։</summary>
        FileStream? OpenRead(string key);

        /// <summary>Ջնջում է ֆայլը (սխալները անտեսվում են)։</summary>
        void Delete(string key);
    }

    public class AssignmentFileService : IAssignmentFileService
    {
        // StoredFileName սյունը բազայում 100 նիշ է
        private const int MaxKeyLength = 100;

        private readonly string _root;
        private readonly string _legacyRoot;
        private readonly byte[] _salt;

        public AssignmentFileService(IConfiguration config, IWebHostEnvironment env)
        {
            var configuredRoot = config["AssignmentFiles:RootPath"];
            if (string.IsNullOrWhiteSpace(configuredRoot))
                throw new InvalidOperationException("AssignmentFiles:RootPath is not configured.");

            _root = Path.GetFullPath(configuredRoot);
            Directory.CreateDirectory(_root);

            // Հին ֆայլերը (առանց թղթապանակների)՝ նախկին վայրում
            var legacy = config["AssignmentFiles:LegacyRootPath"];
            _legacyRoot = Path.GetFullPath(string.IsNullOrWhiteSpace(legacy)
                ? Path.Combine(env.ContentRootPath, "App_Data", "assignment-files")
                : legacy);

            var salt = config["AssignmentFiles:FolderSalt"];
            if (string.IsNullOrWhiteSpace(salt))
                throw new InvalidOperationException("AssignmentFiles:FolderSalt is not configured.");
            _salt = Encoding.UTF8.GetBytes(salt);
        }

        // Օգտատիրոջ թղթապանակը՝ «teacher-9f3a1c27» (UserId-ն ուղիղ չի երևում, բայց նույն օգտատերը միշտ նույն թղթապանակն ունի)
        private string OwnerFolder(string ownerPrefix, string ownerUserId)
        {
            var hash = HMACSHA256.HashData(_salt, Encoding.UTF8.GetBytes(ownerUserId));
            return $"{ownerPrefix}-{Convert.ToHexString(hash, 0, 4).ToLowerInvariant()}";
        }

        public async Task<string> SaveAsync(string ownerUserId, string ownerPrefix, IFormFile file, DateTime now)
        {
            // ընդլայնումը՝ միայն փոքր տառեր և թվեր (օրինակ՝ .pdf)
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext.Length > 6 || !ext.Skip(1).All(c => char.IsAsciiLetterOrDigit(c))) ext = "";

            var folder = OwnerFolder(ownerPrefix, ownerUserId);
            var key = $"{folder}/{now:yyyy}/{now:MM}/{now:dd}/{Guid.NewGuid():N}{ext}";
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
