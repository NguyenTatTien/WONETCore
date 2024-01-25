using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using WO.Models;
namespace WO.Controllers
{
    public class LoadPartController : Controller
    {
        private readonly IConfiguration _configuration;
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("partAndStore") != null)
            {
                List<TablePart> parts = JsonConvert.DeserializeObject<List<TablePart>>(HttpContext.Session.GetString("partAndStore"));
                if (parts.Count > 10)
                {
                    HttpContext.Session.SetString("partToScroll", JsonConvert.SerializeObject(parts.GetRange(0, 10)));
                }
                else
                {
                    HttpContext.Session.SetString("partToScroll", JsonConvert.SerializeObject(parts));
                }
            }
            return View("~/Views/Home/LoadPart.cshtml");
        }
        public LoadPartController(IConfiguration _configuration)
        {
            this._configuration = _configuration;
        }
        //public List<TablePart> getParts()
        //{
        //    var parts = new List<TablePart>();
        //    List<StorePPM> géttores = JsonConvert.DeserializeObject<List<StorePPM>>(HttpContext.Session.GetString("partAndStore"));
        //    List<Part> getparts = JsonConvert.DeserializeObject<List<Part>>(HttpContext.Session.GetString("parts"));
        //    List<WOPartDtoORA> wOACTDtoORAs = JsonConvert.DeserializeObject<List<WOPartDtoORA>>(HttpContext.Session.GetString("tablePart"));
        //    var listPart = wOACTDtoORAs.Where(t => t.status != 3).Select(t => t.par_code).Distinct().ToList();
        //    if (getparts != null)
        //    {
        //        foreach (var item in getparts)
        //        {

        //            if (!listPart.Contains(item.partCode))
        //            {
        //                var stores = géttores.Where(p => p.sto_part == item.partCode);
        //                if (stores != null)
        //                {
        //                    foreach (var row2 in stores)
        //                    {
        //                        var pa = new TablePart()
        //                        {
        //                            par_code = item.partCode,
        //                            par_desc = item.partDesc,
        //                            par_longdescription = item.partLongDesc,
        //                            sto_store = row2.sto_store,
        //                            STO_QTY = row2.sto_qty
        //                        };
        //                        parts.Add(pa);
        //                    }

        //                }
        //            }

        //        }
        //    }

        //    return parts;
        //}
        public IActionResult GetPartList([DataSourceRequest] DataSourceRequest request)
        {

            if (HttpContext.Session.GetString("partToScroll") != null)
            {
                List<TablePart> parts = JsonConvert.DeserializeObject<List<TablePart>>(HttpContext.Session.GetString("partToScroll"));
                return Json(parts.ToDataSourceResult(request));
            }
            return Json(new { });
        }
        public IActionResult SavePartSelect([FromBody] TablePart wOPartDtoORA)
        {
            HttpContext.Session.SetString("partSelect", JsonConvert.SerializeObject(wOPartDtoORA));
            return Ok();
        }
        [HttpPost]
        public IActionResult LoadDataScroll([DataSourceRequest] DataSourceRequest request, int totalItem)
        {
            if (HttpContext.Session.GetString("partAndStore") != null)
            {
                List<TablePart> partAll = JsonConvert.DeserializeObject<List<TablePart>>(HttpContext.Session.GetString("partAndStore"));
                if (partAll.Count > totalItem)
                {
                    List<TablePart> parts = new List<TablePart>();
                    if (partAll.Count - totalItem > 3)
                    {
                        parts = partAll.GetRange(totalItem, 3);
                        return Json(parts);
                    }
                    else
                    {
                        parts = partAll.GetRange(totalItem, partAll.Count - totalItem);
                        return Json(parts);
                    }
                }
            }

            return Json(new { });
        }
        
    }
}
