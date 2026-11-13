using EduPlatform.Services;
using EduPlatform.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EduPlatform.Controllers
{
    /// <summary>
    /// «Իմ գրապահոց»՝ ընդհանուր բաժին (ուսուցիչ և սովորող)։
    /// Հասցեները՝ /Repository/MyTools, /Repository/MyCreatedMaterials
    /// Էջերը՝ Views/Shared/Repository/
    /// </summary>
    [Authorize]
    [Route("Repository")]
    public class RepositoryController : Controller
    {
        private readonly ISidebarMenuBuilder _menuBuilder;
        private readonly IConfiguration _config;

        public RepositoryController(ISidebarMenuBuilder menuBuilder, IConfiguration config)
        {
            _menuBuilder = menuBuilder;
            _config = config;
        }

        // Կողային մենյուն ամեն էջի համար  (ըստ դերի՝ ուսուցչի կամ սովորողի)
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ViewBag.SidebarItems = _menuBuilder.ForUser(User);
            ViewBag.SidebarFooterItems = _menuBuilder.FooterForUser(User);
            base.OnActionExecuting(context);
        }

        // «Իմ գործիքները»
        [HttpGet("MyTools")]
        public IActionResult MyTools()
        {
            return View(new ToolsPageViewModel
            {
                Tools = new(),
                AddToolUrl = $"{_config["Authentication:Authority"]}/Tools"
            });
        }

        // «Իմ ստեղծած նյութերը»՝ առայժմ առանց տվյալների աղբյուրի (դատարկ ցանկ)
        [HttpGet("MyCreatedMaterials")]
        public IActionResult MyCreatedMaterials()
        {
            return View(new CreatedMaterialsPageViewModel());
        }
    }
}
