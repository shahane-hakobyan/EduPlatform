using EduPlatform.Data;
using EduPlatform.Resources;
using EduPlatform.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace EduPlatform.Services
{
    /// <summary>
    /// Ուսուցչի դասարանները և նրանց սովորողները (օգտագործվում է առաջադրանքների և խմբերի կողմից)
    /// </summary>
    public interface IClassRosterService
    {
        Task<List<AssignmentClassGroupViewModel>> BuildClassGroupsAsync(string teacherUserId);
    }

    public class ClassRosterService : IClassRosterService
    {
        private readonly EduPlatformDbContext _db;
        private readonly IStringLocalizer<SharedResource> _l;

        public ClassRosterService(EduPlatformDbContext db, IStringLocalizer<SharedResource> localizer)
        {
            _db = db;
            _l = localizer;
        }

        // Ուսուցչի դասարանները (վերջին ուսումնական տարով) և նրանց աշակերտները
        public async Task<List<AssignmentClassGroupViewModel>> BuildClassGroupsAsync(string teacherUserId)
        {
            var teacherGrades = (await _db.TeacherGrades
                    .Where(tg => tg.TeacherUserId == teacherUserId)
                    .Select(tg => new { tg.GradeId, tg.Grade.GradeName, tg.AcademicYear })
                    .ToListAsync())
                .GroupBy(g => g.GradeId)
                .Select(g => g.OrderByDescending(x => x.AcademicYear).First())
                .OrderBy(g => g.GradeName)
                .ToList();

            var gradeIds = teacherGrades.Select(g => g.GradeId).ToList();
            var pupilRows = await _db.PupilGrades
                .Where(pg => gradeIds.Contains(pg.GradeId))
                .Select(pg => new { pg.PupilUserId, pg.GradeId, pg.AcademicYear })
                .ToListAsync();

            var classGroups = teacherGrades.Select(g => new AssignmentClassGroupViewModel
            {
                GradeId = g.GradeId,
                GradeName = g.GradeName,
                // Անուններն ու նկարները AuthServer-ում են՝ առայժմ տեղապահներ
                Pupils = pupilRows
                    .Where(p => p.GradeId == g.GradeId && p.AcademicYear == g.AcademicYear)
                    .OrderBy(p => p.PupilUserId)
                    .Select((p, i) => new RosterPupilViewModel
                    {
                        UserId = p.PupilUserId,
                        Name = $"{_l["Pupil"]} {i + 1}",
                        PhotoUrl = "/images/avatar-placeholder.jpg"
                    })
                    .ToList()
            }).ToList();

            return classGroups;
        }
    }
}
