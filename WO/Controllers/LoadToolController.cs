using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WO.Models;

namespace WO.Controllers
{
    public class LoadToolController : Controller
    {
        public IActionResult Index()
        {
            return View("~/Views/ELEForm/LoadTool.cshtml");
        }
        public IActionResult GetToolList([DataSourceRequest] DataSourceRequest request)
        {

            if (HttpContext.Session.GetString("LoadDataTool") != null)
            {
                List<Tool> tools = JsonConvert.DeserializeObject<List<Tool>>(HttpContext.Session.GetString("LoadDataTool"));
                return Json(tools.ToDataSourceResult(request));
            }
            return Json(new { });
        }
        public IActionResult SaveToolSelect([FromBody] Tool tool)
        {
            HttpContext.Session.SetString("toolSelect", JsonConvert.SerializeObject(tool));
            return Ok();
        }
    }
}
