using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Data
{
    public class EduPlatformDbContext : DbContext
    {
        public EduPlatformDbContext(DbContextOptions<EduPlatformDbContext> options)
            : base(options)
        {
        }
    }
}