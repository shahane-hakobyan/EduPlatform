using System.Security.Claims;
using EduPlatform.Data;
using EduPlatform.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduPlatform.Api.Controllers
{
    /// <summary>
    /// Library-ի նյութերը «Իմ գրադարան»-ում պահելու / հեռացնելու API:
    /// Կանչվում է Library.Site-ից օգտատիրոջ տոկենով:
    /// </summary>
    [ApiController]
    [Route("api/user-library")]
    [Authorize(Roles = "Teacher")] // աշակերտների համար հետագայում՝ "Teacher,Pupil"
    public class UserLibraryController : ControllerBase
    {
        private static readonly HashSet<string> AllowedTypes =
            new() { "Textbook", "MethodicalGuide", "OtherMaterial", "OtherBook" };

        private readonly EduPlatformDbContext _db;

        public UserLibraryController(EduPlatformDbContext db) => _db = db;

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public record SaveRequest(string MaterialType, int LibraryMaterialId);

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveRequest req)
        {
            if (!AllowedTypes.Contains(req.MaterialType) || req.LibraryMaterialId <= 0)
                return BadRequest();

            var exists = await _db.UserLibraryMaterials.AnyAsync(x =>
                x.UserId == UserId &&
                x.MaterialType == req.MaterialType &&
                x.LibraryMaterialId == req.LibraryMaterialId);

            if (!exists)
            {
                _db.UserLibraryMaterials.Add(new UserLibraryMaterial
                {
                    UserId = UserId,
                    MaterialType = req.MaterialType,
                    LibraryMaterialId = req.LibraryMaterialId
                });
                await _db.SaveChangesAsync();
            }

            return Ok(new { saved = true });
        }

        [HttpDelete("{materialType}/{libraryMaterialId:int}")]
        public async Task<IActionResult> Remove(string materialType, int libraryMaterialId)
        {
            await _db.UserLibraryMaterials
                .Where(x => x.UserId == UserId
                         && x.MaterialType == materialType
                         && x.LibraryMaterialId == libraryMaterialId)
                .ExecuteDeleteAsync();

            return Ok(new { saved = false });
        }

        // GET api/user-library/saved?materialType=Textbook&ids=1&ids=2
        // → այս ids-ներից որոնք են պահված օգտատիրոջ կողմից
        [HttpGet("saved")]
        public async Task<ActionResult<List<int>>> Saved(string materialType, [FromQuery] int[] ids)
        {
            return await _db.UserLibraryMaterials
                .Where(x => x.UserId == UserId
                         && x.MaterialType == materialType
                         && ids.Contains(x.LibraryMaterialId))
                .Select(x => x.LibraryMaterialId)
                .ToListAsync();
        }
    }
}
