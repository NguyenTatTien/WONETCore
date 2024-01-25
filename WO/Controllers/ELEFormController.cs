using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Data;
using WO.Models;
using Kendo.Mvc.UI;
using CommandType = System.Data.CommandType;
using Kendo.Mvc.Extensions;
using Newtonsoft.Json.Linq;
using System.Text;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic;
using System.Globalization;

namespace WO.Controllers
{
    public class ELEFormController : Controller
    {
        private readonly ILogger<ELEFormController> _logger;
        private readonly IConfiguration _configuration;
        public static string wocode = "";
        public static string Env_Type = "";
        public static string vEvironment = "";
        public static string vHost = "";
        public static string vTennant = "";
        public static string vOrg = "";
        public static string vConnection = "";
        public ELEFormController(ILogger<ELEFormController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        [HttpGet]
        public IActionResult Index(string wocode2)
        {
            try
            {
                Env_Type = _configuration["Env_Type"];
                wocode = HttpContext.Request.Query["EVT_CODE"];
                if (Env_Type == "XNK")
                {
                    vEvironment = _configuration["vEvironmentXNK"];
                    vHost = _configuration["vHostXNK"];
                    vTennant = _configuration["vTennantXNK"];
                    vOrg = _configuration["vOrgXNK"];
                    vConnection = _configuration["connectStringXNK"];
                    if (wocode == null)
                    {
                        wocode = wocode2;
                    }
                }
                else
                {
                    vEvironment = _configuration["vEvironment"];
                    vHost = _configuration["vHost"];
                    vTennant = _configuration["vTennant"];
                    vOrg = _configuration["vOrg"];
                    vConnection = _configuration["connectString"];
                    if (wocode == null)
                    {
                        wocode = wocode2;
                    }
                }

                LoaddropListLDG();
                var pre = LoaddropListPreson();
                GetWODataListORA(wocode);

                loadDataBoo(wocode);
                loadDataTool(wocode);
                loadlistStock();
                loadlistNDCV();
                var WOlist = JsonConvert.DeserializeObject<List<WODtoORA>>(HttpContext.Session.GetString("WOList"));
                var WOObj = WOlist.FirstOrDefault();


                LoadTablePerson(WOObj);
                var x = Task.Run(async () => await getList2());
                HttpContext.Session.SetString("tablePart", JsonConvert.SerializeObject(x.Result));
                getParts();
                getStore();
                loadDataTool();
                return View(WOObj);
            }catch(Exception e)
            {
                //string filePath = @"C:\error\error.txt";
                //using (StreamWriter writer = new StreamWriter(filePath))
                //{
                //    // Write content to the file
                //    writer.WriteLine("Error:"+e);

                //}

                return View("~/Views/Shared/Error.cshtml", e.Message.ToString() + ";" + e.StackTrace.ToString()+";"+e.ToString()) ;
            }
        }

        private void loadlistNDCV()
        {
            DataTable dt = new DataTable();
            OracleParameter paramUsername = new OracleParameter("p_user", OracleDbType.Varchar2);
            paramUsername.Direction = ParameterDirection.Input;
            paramUsername.Value = wocode;
            DataSet ds;
            var WOList = new List<WOACTDtoORA>();
            OracleConnection con = new OracleConnection(vConnection);
            this.ExcuteQueryNotValue(con, "RefreshTempTable");
            con = new OracleConnection(vConnection);
            ds = this.GetDataSet(con, "get_WO_ACT_r5", new[] { paramUsername });
            int index = 0;
            if (ds != null)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    index++;
                    WOACTDtoORA wo = new WOACTDtoORA();
                    wo.ACT_EVENT = row.IsNull("ACT_EVENT") ? "" : row["ACT_EVENT"].ToString();
                    wo.ACT_ACT = row.IsNull("ACT_ACT") ? "" : row["ACT_ACT"].ToString();
                    wo.MaCV = row.IsNull("MaCV") ? "" : row["MaCV"].ToString();
                    wo.hangmuccv = row.IsNull("hangmuccv") ? "" : row["hangmuccv"].ToString();
                    wo.nhanluc = row.IsNull("nhanluc") ? 0 : int.Parse(row["nhanluc"].ToString());
                    wo.thoigian = row.IsNull("thoigian") ? 0 : int.Parse(row["thoigian"].ToString());
                    wo.nguoithuchien = row.IsNull("nguoithuchien") ? "" : row["nguoithuchien"].ToString();
                    wo.trangthai = row.IsNull("trangthai") ? "" : row["trangthai"].ToString();
                    wo.ghichu = row.IsNull("ghichu") ? "" : row["ghichu"].ToString();
                    wo.ngaythuchien = row.Field<DateTime?>("ngaythuchien") ?? DateTime.MinValue;
                    wo.isnew = 0;
                    wo.idRow = index;
                    wo.status = 0;
                    wo.matlist = row.IsNull("matlist") ? "" : row["matlist"].ToString();
                    WOList.Add(wo);
                }
            }
            // grdData.DataSource = WOList;
            HttpContext.Session.SetString("WOListACT", JsonConvert.SerializeObject(WOList));
            HttpContext.Session.SetString("WOListACTOrigin", JsonConvert.SerializeObject(WOList));
        }
        public string ExcuteQueryNotValue(OracleConnection conn, string SPName)
        {
            var mess = string.Empty;
            using (conn)
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                using (OracleCommand command = new OracleCommand(SPName, conn))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    try
                    {
                        //command.Parameters.AddRange(OraclePrms);
                        command.ExecuteNonQuery();
                        conn.Close();
                        command.Parameters.Clear();
                    }
                    catch (Exception e)
                    {
                        mess = e.Message;
                    }
                }
            }

            return mess;
        }
        private async Task<List<WOPartDtoORA>> getList2()
        {
            DataTable b21 = null;
            List<WOPartDtoORA> listobj = new List<WOPartDtoORA>();
            List<WOPartDtoORA> listobj2 = new List<WOPartDtoORA>();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Host", vHost);
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                client.DefaultRequestHeaders.Add("Request-Type", "XMLHTTP");
                var vEamid = LoginEAM(client);
                var url2 = vEvironment + "/web/base/EWSUSR.TAB?eamid=" + vEamid.Result.Replace("'", "") + "&tenant=" + vTennant;
                var values2 = new Dictionary<string, string>
                 {
                     { "SYSTEM_FUNCTION_NAME", "WSJOBS" },
                     { "USER_FUNCTION_NAME", "WSJOBS" },
                     { "CURRENT_TAB_NAME", "XVT" },
                     { "workordernum", wocode },
                     { "workorderrtype", "PM" },
                     { "organization", vOrg }
                 };

                var data2 = new FormUrlEncodedContent(values2);
                var response2 = await client.PostAsync(url2, data2);
                response2.EnsureSuccessStatusCode();
                var resultBytes2 = await response2.Content.ReadAsByteArrayAsync();
                var decompressedResult2 = Decompress(resultBytes2);

                var resultString2 = Encoding.UTF8.GetString(decompressedResult2);
                JObject jsonObject = JsonConvert.DeserializeObject<JObject>(resultString2);

                var ac = jsonObject["pageData"]["grid"]["GRIDRESULT"]["GRID"]["DATA"];
                listobj = JsonConvert.DeserializeObject<List<WOPartDtoORA>>(ac.ToString());
                listobj2 = JsonConvert.DeserializeObject<List<WOPartDtoORA>>(ac.ToString());
                int index = 0;
                foreach (var item in listobj)
                {
                    index++;
                    item.idRow = index;
                    item.status = 0;
                    if (HttpContext.Session.GetString("parts") != null && (JsonConvert.DeserializeObject<List<Part>>(HttpContext.Session.GetString("parts")).Count > 0))
                    {
                        var part = (JsonConvert.DeserializeObject<List<Part>>(HttpContext.Session.GetString("parts"))).FirstOrDefault(t => t.partCode == item.par_code);
                        item.par_desc = part.partDesc;
                        item.par_longdescription = part.partLongDesc;
                        item.oldUsedQty = item.calculated_column.ToString();
                        item.oldPlanedQty = item.mlp_qty;
                    }

                }

                // for table2
                index = 0;
                foreach (var item2 in listobj2)
                {
                    index++;
                    item2.idRow = index;
                    item2.status = 0;
                    if (HttpContext.Session.GetString("parts") != null && JsonConvert.DeserializeObject<List<Part>>(HttpContext.Session.GetString("parts")).Count > 0)
                    {
                        var part = (JsonConvert.DeserializeObject<List<Part>>(HttpContext.Session.GetString("parts"))).FirstOrDefault(t => t.partCode == item2.par_code);
                        item2.par_desc = part.partDesc;
                        item2.par_longdescription = part.partLongDesc;
                        item2.oldUsedQty = item2.calculated_column.ToString();
                        item2.oldPlanedQty = item2.mlp_qty;
                    }

                }
                //   b21 = JsonConvert.DeserializeObject<DataTable>(ac.ToString());
                var vlogout = LogoutEAM(client, vEamid.Result.Replace("'", ""), vTennant);
                var rq = vlogout.Result;
            }
            HttpContext.Session.SetString("tablePart", JsonConvert.SerializeObject(listobj));
            HttpContext.Session.SetString("originpart", JsonConvert.SerializeObject(listobj2));
            return listobj;
        }
        private async Task<string> LoginEAM(HttpClient client)
        {
            try
            {
                var vUser = "ADMIN1";
                var vPass = "Ingr.123";
                var url = vEvironment + "/web/base/login?userid=" + vUser + "&password=" + vPass + "&tenant=" + vTennant;
                var values = new Dictionary<string, string>
            {
                { "SYSTEM_FUNCTION_NAME", "LOGIN" },
                { "USER_FUNCTION_NAME", "LOGIN" },
                { "window", "main_eam" },
                { "userid", vUser },
                { "password", vPass },
                { "tenant", vTennant }
            };
                var data = new FormUrlEncodedContent(values);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var resultBytes = await response.Content.ReadAsByteArrayAsync();
                var decompressedResult = Decompress(resultBytes);
                var resultString = Encoding.UTF8.GetString(decompressedResult);
                var b = resultString.Replace("\r\n", string.Empty).Replace("{", String.Empty).Split(',')[1].Split(':')[1].Replace("\"", String.Empty);
                return b;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        private async Task<string> LogoutEAM(HttpClient client, string vEAMID, String vTENNANT)
        {
            var url = vEvironment + "/web/base/logout?eamid=" + vEAMID + "&tenant=" + vTENNANT;
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var resultBytes = await response.Content.ReadAsByteArrayAsync();
            var decompressedResult = Decompress(resultBytes);
            var resultString = Encoding.UTF8.GetString(decompressedResult);
            return resultString;
        }
        private static byte[] Decompress(byte[] compressedData)
        {
            using (var compressedStream = new MemoryStream(compressedData))
            using (var decompressedStream = new MemoryStream())
            {
                using (var decompressor = new System.IO.Compression.GZipStream(compressedStream, System.IO.Compression.CompressionMode.Decompress))
                {
                    decompressor.CopyTo(decompressedStream);
                }

                return decompressedStream.ToArray();
            }
        }
        public void loadlistStock()
        {
            DataTable dt = new DataTable();
            OracleParameter paramUsername = new OracleParameter("p_user", OracleDbType.Varchar2);
            paramUsername.Direction = ParameterDirection.Input;
            paramUsername.Value = wocode;
            DataSet ds;
            var BOList = new List<BinStockDto>();
            OracleConnection con = new OracleConnection(vConnection);

            ds = this.GetDataSet(con, "get_BIN_STOCK_r5", new[] { paramUsername });
            int index = 0;
            if (ds != null)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    index++;
                    BinStockDto wo = new BinStockDto();
                    wo.BIS_PART = row.IsNull("BIS_PART") ? null : row["BIS_PART"].ToString();
                    wo.BIS_STORE = row.IsNull("BIS_STORE") ? null : row["BIS_STORE"].ToString();
                    wo.BIS_BIN = row.IsNull("BIS_BIN") ? null : row["BIS_BIN"].ToString();
                    wo.BIS_LOT = row.IsNull("BIS_LOT") ? null : row["BIS_LOT"].ToString();
                    wo.BIS_PART_ORG = row.IsNull("BIS_PART_ORG") ? null : row["BIS_PART_ORG"].ToString();
                    wo.BIS_QTY = row.IsNull("BIS_QTY") ? 0 : Convert.ToInt32(row["BIS_QTY"].ToString().Replace(",", "").Replace(".", ""));
                    BOList.Add(wo);
                }
            }
            HttpContext.Session.SetString("BinStockList", JsonConvert.SerializeObject(BOList));
        }
        public List<TablePart> getStore()
        {
            DataTable dt = new DataTable();
            DataSet ds;
            var stores = new List<TablePart>();
            OracleConnection con = new OracleConnection(vConnection);
            ds = this.GetDataSet(con, "get_STORE_ALL_PART", null);
            if (ds != null)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {

                    TablePart store = new TablePart() { sto_store = row.IsNull("sto_store") ? null : row["sto_store"].ToString(), par_code = row.IsNull("par_code") ? null : row["par_code"].ToString(), STO_QTY = row.IsNull("sto_qty") ? null : row["sto_qty"].ToString(), par_desc = row.IsNull("par_desc") ? null : row["par_desc"].ToString(), par_longdescription = row.IsNull("par_longdescription") ? null : row["par_longdescription"].ToString() };
                    stores.Add(store);
                }
            }
            HttpContext.Session.SetString("partAndStore", JsonConvert.SerializeObject(stores));
            return stores;
        }
        public List<Part> getParts()
        {
            DataTable dt = new DataTable();
            HttpContext.Session.Remove("parts");
            DataSet ds;
            var parts = new List<Part>();
            OracleConnection con = new OracleConnection(vConnection);
            ds = this.GetDataSet(con, "GET_PART_LIST", null);
            if (ds != null)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {

                    var part = new Part() { partCode = row.IsNull("PAR_CODE") ? null : row["PAR_CODE"].ToString(), partDesc = row.IsNull("par_desc") ? null : row["par_desc"].ToString(), partLongDesc = row.IsNull("par_longdescription") ? null : row["par_longdescription"].ToString() };
                    parts.Add(part);
                }
            }
            HttpContext.Session.SetString("parts", JsonConvert.SerializeObject(parts));
            return parts;
        }
        private void GetWODataListORA(string WO)
        {
            List<Person> persons = JsonConvert.DeserializeObject<List<Person>>(HttpContext.Session.GetString("ListPerson"));
            DataTable dt = new DataTable();
            OracleParameter paramUsername = new OracleParameter("p_user", OracleDbType.Varchar2);
            paramUsername.Direction = ParameterDirection.Input;
            paramUsername.Value = WO;
            DataSet ds;
            OracleConnection con = new OracleConnection(vConnection);
            var WOList = new List<WODtoORA>();
            ds = this.GetDataSet(con, "get_WO_r5", new[] { paramUsername });
            if (ds != null)
            {
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    WODtoORA wo = new WODtoORA();
                    wo.evt_code = row.IsNull("evt_code") ? null : row["evt_code"].ToString();
                    wo.gian = row.IsNull("gian") ? null : row["gian"].ToString();
                    wo.bophan = row.IsNull("bophan") ? null : row["bophan"].ToString();
                    wo.tentbi = row.IsNull("tentbi") ? null : row["tentbi"].ToString();
                    wo.matbi = row.IsNull("matbi") ? null : row["matbi"].ToString();
                    wo.MaBD = row.IsNull("MaBD") ? null : row["MaBD"].ToString();
                    wo.TenBD = row.IsNull("TenBD") ? null : row["TenBD"].ToString();
                    wo.nhanluc = row.IsNull("nhanluc") ? 0 : int.Parse(row["nhanluc"].ToString());
                    wo.thoigianBD = row.IsNull("thoigianBD") ? 0 : int.Parse(row["thoigianBD"].ToString());
                    wo.evt_TGBD = row.IsNull("TGBatDau") ? (DateTime?)null : DateTime.Parse(row["TGBatDau"].ToString());
                    wo.evt_TGKT = row.IsNull("TGKetThuc") ? (DateTime?)null : DateTime.Parse(row["TGKetThuc"].ToString());
                    wo.TTHDThietBi = row.IsNull("TTThietBi") ? false : row["TTThietBi"].ToString() == "+" ? true : false;
                    wo.KQNghiemThu = row.IsNull("KQNghiemThu") ? false : row["KQNghiemThu"].ToString() == "+" ? true : false;
                    wo.LanhDaoGian = row.IsNull("LanhDaoGian") ? null : row["LanhDaoGian"].ToString();
                    wo.KSBBDCS = row.IsNull("KSBBDCS") ? null : row["KSBBDCS"].ToString();
                    wo.TruongBoPhan = row.IsNull("TruongBoPhan") ? null : row["TruongBoPhan"].ToString();
                    wo.KSVanHanh = row.IsNull("KSVanHanh") ? null : row["KSVanHanh"].ToString();
                    wo.TGLVThietBi = row.IsNull("TGLVThietBi") ? 0 : CalculateMonthsApart(DateTime.Parse(row["TGLVThietBi"].ToString()), DateTime.Now) / 12;
                    wo.Org_TBi = row.IsNull("Org_TBi") ? null : row["Org_TBi"].ToString();
                    wo.NgayLanhDao = row.IsNull("NgayLanhDao") ? "" : row["NgayLanhDao"].ToString();
                    wo.NgayTruongBoPhan = row.IsNull("NgayTruongBoPhan") ? "" : row["NgayTruongBoPhan"].ToString();
                    wo.NgayKSVH = row.IsNull("NgayKSVH") ? "" : row["NgayKSVH"].ToString();
                    wo.NgayKSBBDSC = row.IsNull("NgayKSBBDSC") ? "" : row["NgayKSBBDSC"].ToString();
                    wo.CapBD = row.IsNull("CapBD") ? null : row["CapBD"].ToString();
                    wo.ViTriLapDat = row.IsNull("vitrilapdat") ? null : row["vitrilapdat"].ToString();

                    wo.TinhTrangThietBi = row.IsNull("TinhTrangTbi") ? null : row["TinhTrangTbi"].ToString();
                    wo.NguoiChoPhep = row.IsNull("NguoiChoPhep") ? null : row["NguoiChoPhep"].ToString();
                    wo.ChucDanh = persons.FirstOrDefault(p => p.per_code == wo.NguoiChoPhep).per_jobtitle;
                    WOList.Add(wo);
                }
            }
            HttpContext.Session.SetString("WOList", JsonConvert.SerializeObject(WOList).ToString());
        }
        public DataSet GetDataSet(OracleConnection conn, string SPName, OracleParameter[] OraclePrms = null)
        {
            var ds = new DataSet();
            try
            {
                using (conn)
                {
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }
                    using (OracleCommand command = new OracleCommand(SPName, conn))
                    {
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        if (OraclePrms != null)
                        {
                            command.Parameters.AddRange(OraclePrms);
                        }
                        OracleParameter paramCursor = new OracleParameter("p_cursor", OracleDbType.RefCursor);
                        paramCursor.Direction = ParameterDirection.Output;
                        command.Parameters.Add(paramCursor);
                        using (OracleDataAdapter adapter = new OracleDataAdapter(command))
                        {
                            adapter.Fill(ds);
                        }
                    }
                }
                return ds;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        static int CalculateMonthsApart(DateTime startDate, DateTime endDate)
        {
            int monthsApart = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;

            return monthsApart;
        }
        [HttpPost]
        public ActionResult SaveChangeData([FromBody] List<WOACTDtoORA> wOACTDtoORAs)
        {
            if (HttpContext.Session.GetString("WOListACT") != null)
            {
                List<WOACTDtoORA> main = JsonConvert.DeserializeObject<List<WOACTDtoORA>>(HttpContext.Session.GetString("WOListACT"));
                int id = 0;
                foreach (var item in wOACTDtoORAs)
                {
                    id++;
                    item.idRow = id;
                }
                wOACTDtoORAs.AddRange(main.Where(t => t.status == 3));
            }
            HttpContext.Session.SetString("WOListACT", JsonConvert.SerializeObject(wOACTDtoORAs));
            return Ok();
        }
        public ActionResult getBOO([DataSourceRequest] DataSourceRequest request)
        {
            if (HttpContext.Session.GetString("BOOList") != null)
            {
                return Json(JsonConvert.DeserializeObject<List<Boo>>(HttpContext.Session.GetString("BOOList")).Where(b => b.status != 3).ToDataSourceResult(request));
            }
            return Json(new { });
        }
        public void loadDataBoo(string WO)
        {
            {
                DataTable dt = new DataTable();
                OracleParameter paramUsername = new OracleParameter("p_user", OracleDbType.Varchar2);
                paramUsername.Direction = ParameterDirection.Input;
                paramUsername.Value = WO;
                DataSet ds;
                OracleConnection con = new OracleConnection(vConnection);
                var BOOList = new List<Boo>();
                ds = this.GetDataSet(con, "get_BOO_r5", new[] { paramUsername });
                if (ds != null)
                {
                    int index = 0;
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        index++;
                        Boo boo = new Boo();
                        boo.MaPhieuCV = row.IsNull("MaPhieuCV") ? null : row["MaPhieuCV"].ToString();
                        boo.MaCV = row.IsNull("MaCV") ? null : row["MaCV"].ToString();
                        boo.MaNV = row.IsNull("MaNV") ? null : row["MaNV"].ToString();
                        boo.TenNV = row.IsNull("TenNV") ? null : row["TenNV"].ToString();
                        boo.ChucDanh = row.IsNull("ChucDanh") ? null : row["ChucDanh"].ToString();
                        boo.Bac = row.IsNull("Bac") ? null : row["Bac"].ToString();
                        boo.Thoigianlamviec = row.IsNull("Thoigianlamviec") ? null : row["Thoigianlamviec"].ToString();
                        boo.idRow = index;
                        boo.status = 0;
                        BOOList.Add(boo);
                    }
                }
                HttpContext.Session.SetString("BOOList", JsonConvert.SerializeObject(BOOList).ToString());
                HttpContext.Session.SetString("BOOList_Origin", JsonConvert.SerializeObject(BOOList).ToString());
            }
        }
        public void loadDataTool(string WO)
        {
            {
                DataTable dt = new DataTable();
                OracleParameter paramUsername = new OracleParameter("p_user", OracleDbType.Varchar2);
                paramUsername.Direction = ParameterDirection.Input;
                paramUsername.Value = WO;
                DataSet ds;
                OracleConnection con = new OracleConnection(vConnection);
                var ToolList = new List<Tool>();
                ds = this.GetDataSet(con, "get_PLAN_TOOL_r5", new[] { paramUsername });
                if (ds != null)
                {
                    int index = 0;
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        index++;
                        Tool tool = new Tool();
                        tool.Ma_Tool = row.IsNull("MATOOL") ? null : row["MATOOL"].ToString();
                        tool.Nha_SX = row.IsNull("NHASANXUAT") ? null : row["NHASANXUAT"].ToString();
                        tool.So_Seri = row.IsNull("SOSERI") ? null : row["SOSERI"].ToString();
                        tool.Hang_Kiem_Dinh = row.IsNull("HANGKIEMDINH") ? null : row["HANGKIEMDINH"].ToString();
                        tool.Ma_Ky_Hieu = row.IsNull("KIHIEU") ? null : row["KIHIEU"].ToString();
                        tool.Ten_TB = row.IsNull("TENTBI") ? null : row["TENTBI"].ToString();

                        tool.idRow = index;
                        tool.status = 0;
                        ToolList.Add(tool);
                    }
                }
                HttpContext.Session.SetString("ToolList", JsonConvert.SerializeObject(ToolList).ToString());
            }
        }
        public ActionResult getDataTool([DataSourceRequest] DataSourceRequest request)
        {
            if (HttpContext.Session.GetString("ToolList") != null)
            {
                return Json(JsonConvert.DeserializeObject<List<Tool>>(HttpContext.Session.GetString("ToolList")).Where(t => t.status != 3).ToDataSourceResult(request));
            }
            return Json(new { });
        }
        public ActionResult SaveChangeTool([FromBody] List<Tool> tools)
        {
            if (HttpContext.Session.GetString("ToolList") != null)
            {
                List<Tool> main = JsonConvert.DeserializeObject<List<Tool>>(HttpContext.Session.GetString("ToolList"));
                int id = 0;
                foreach (var item in tools)
                {
                    
                    id++;
                    item.idRow = id;
                }
                tools.AddRange(main.Where(t => t.status == 3));
            }
            HttpContext.Session.SetString("ToolList", JsonConvert.SerializeObject(tools));
            return Ok();
        }
        public ActionResult DeleteItemTool(int idRow)
        {
            List<Tool> tools = JsonConvert.DeserializeObject<List<Tool>>(HttpContext.Session.GetString("ToolList"));
            var item = tools.FirstOrDefault(w => w.idRow == idRow);
            if (item.status == 1)
            {
                tools.Remove(item);
            }
            else
            {
                item.status = 3;
            }
            HttpContext.Session.SetString("ToolList", JsonConvert.SerializeObject(tools));
            return Ok();
        }
        public ActionResult SaveChangeBoo([FromBody] List<Boo> boos)
        {
            if (HttpContext.Session.GetString("BOOList") != null)
            {
                List<Boo> main = JsonConvert.DeserializeObject<List<Boo>>(HttpContext.Session.GetString("BOOList"));
                int id = 0;
                foreach (var item in boos)
                {
                    id++;
                    item.idRow = id;
                }
                boos.AddRange(main.Where(t => t.status == 3));
            }
            HttpContext.Session.SetString("BOOList", JsonConvert.SerializeObject(boos));
            return Ok();
        }
        public ActionResult DeleteItemBOO(int idRow)
        {
            List<Boo> boos = JsonConvert.DeserializeObject<List<Boo>>(HttpContext.Session.GetString("BOOList"));
            var item = boos.FirstOrDefault(w => w.idRow == idRow);
            if (item.status == 1)
            {
                boos.Remove(item);
            }
            else
            {
                item.status = 3;
            }
            HttpContext.Session.SetString("BOOList", JsonConvert.SerializeObject(boos));
            return Ok();
        }
        public ActionResult getTablePerson([DataSourceRequest] DataSourceRequest request)
        {
            if (HttpContext.Session.GetString("PersonTable") != null)
            {
                return Json(JsonConvert.DeserializeObject<List<PersonTable>>(HttpContext.Session.GetString("PersonTable")).ToDataSourceResult(request));
            }
            return Json(new { });
        }
        public void LoadTablePerson(WODtoORA wODtoORA)
        {
            List<Person> persons = JsonConvert.DeserializeObject<List<Person>>(HttpContext.Session.GetString("ListPerson"));
            if (wODtoORA != null)
            {
                var CDTBP = persons.FirstOrDefault(p => p.per_code == wODtoORA.TruongBoPhan) != null ? persons.FirstOrDefault(p => p.per_code == wODtoORA.TruongBoPhan).per_jobtitle : "";
                var CDKSBD = persons.FirstOrDefault(p => p.per_code == wODtoORA.KSBBDCS) != null ? persons.FirstOrDefault(p => p.per_code == wODtoORA.KSBBDCS).per_jobtitle : "";
                var CDLDG = persons.FirstOrDefault(p => p.per_code == wODtoORA.LanhDaoGian) != null ? persons.FirstOrDefault(p => p.per_code == wODtoORA.LanhDaoGian).per_jobtitle : "";
                var CDKSVH = persons.FirstOrDefault(p => p.per_code == wODtoORA.KSVanHanh) != null ? persons.FirstOrDefault(p => p.per_code == wODtoORA.KSVanHanh).per_jobtitle : "";
                var TenTBP = persons.FirstOrDefault(p => p.per_code == wODtoORA.TruongBoPhan) != null ? persons.FirstOrDefault(p => p.per_code == wODtoORA.TruongBoPhan).per_desc : "";
                var TenKSBD = persons.FirstOrDefault(p => p.per_code == wODtoORA.KSBBDCS) != null ? persons.FirstOrDefault(p => p.per_code == wODtoORA.KSBBDCS).per_desc : "";
                var TenLDG = persons.FirstOrDefault(p => p.per_code == wODtoORA.LanhDaoGian) != null ? persons.FirstOrDefault(p => p.per_code == wODtoORA.LanhDaoGian).per_desc : "";
                var TenKSVH = persons.FirstOrDefault(p => p.per_code == wODtoORA.KSVanHanh) != null ? persons.FirstOrDefault(p => p.per_code == wODtoORA.KSVanHanh).per_desc : "";
                List<PersonTable> personTables = new List<PersonTable>() {
                new PersonTable() {idRow = 1,ViTri = "Trưởng nhóm bảo dưỡng/ " + Environment.NewLine + "Старший группъі ППР", MaPer = wODtoORA.TruongBoPhan,HoTen = TenTBP,ChucDanh = CDTBP,Ngay=string.IsNullOrEmpty(wODtoORA.NgayTruongBoPhan)?(DateTime?)null:DateTime.ParseExact(wODtoORA.NgayTruongBoPhan, "dd/MM/yyyy", CultureInfo.InvariantCulture),status=0 },
                new PersonTable() {idRow = 1,ViTri = "Người chịu trách nhiệm bảo dưỡng/ " + Environment.NewLine + "Лицо ответственное за въіполние ППР", MaPer = wODtoORA.KSBBDCS,HoTen = TenKSBD,ChucDanh = CDKSBD,Ngay=string.IsNullOrEmpty(wODtoORA.NgayKSBBDSC)?(DateTime?)null: DateTime.ParseExact(wODtoORA.NgayKSBBDSC, "dd/MM/yyyy", CultureInfo.InvariantCulture),status=0 },
                new PersonTable() {idRow = 1,ViTri = "Lãnh đạo công trình biển/  " + Environment.NewLine + "Руководитель объекта", MaPer = wODtoORA.LanhDaoGian,HoTen = TenLDG,ChucDanh = CDLDG,Ngay=string.IsNullOrEmpty(wODtoORA.NgayLanhDao)?(DateTime?)null: DateTime.ParseExact(wODtoORA.NgayLanhDao, "dd/MM/yyyy", CultureInfo.InvariantCulture),status=0 },
                    new PersonTable() {idRow = 1,ViTri = "Đại diện PVGas-SE/ " + Environment.NewLine + "Представитель PVGAS-SE", MaPer = wODtoORA.KSVanHanh,HoTen = TenKSVH,ChucDanh = CDKSVH,Ngay= string.IsNullOrEmpty(wODtoORA.NgayKSVH)?(DateTime?)null:DateTime.ParseExact(wODtoORA.NgayKSVH, "dd/MM/yyyy", CultureInfo.InvariantCulture),status=0 },
             };
                HttpContext.Session.SetString("PersonTable", JsonConvert.SerializeObject(personTables));
            }
            else
            {
                List<PersonTable> personTables = new List<PersonTable>() {
                new PersonTable() {idRow = 1,ViTri = "Trưởng nhóm bảo dưỡng/ " + Environment.NewLine + "Старший группъі ППР", MaPer = "",HoTen = "",ChucDanh = "",Ngay=(DateTime?)null,status=0 },
                new PersonTable() {idRow = 1,ViTri = "Người chịu trách nhiệm bảo dưỡng/ " + Environment.NewLine + "Лицо ответственное за въіполние ППР", MaPer ="",HoTen = "",ChucDanh = "",Ngay=(DateTime?)null,status=0 },
                new PersonTable() {idRow = 1,ViTri = "Lãnh đạo công trình biển/  " + Environment.NewLine + "Руководитель объекта", MaPer = "",HoTen = "",ChucDanh = "",Ngay=(DateTime?)null,status=0 },
                new PersonTable() {idRow = 1,ViTri = "Đại diện PVGas-SE/ " + Environment.NewLine + "Представитель PVGAS-SE", MaPer = "",HoTen = "",ChucDanh = "",Ngay= (DateTime?)null,status=0 },

             };
                HttpContext.Session.SetString("PersonTable", JsonConvert.SerializeObject(personTables));
            }
            
        }
        public List<Person> LoaddropListPreson()
        {
            DataTable dt = new DataTable();
            DataSet ds;
            var WOList = new List<WOPartDtoORA>();
            OracleConnection con = new OracleConnection(vConnection);
            ds = this.GetDataSet(con, "get_PERSON", null);
            if (ds != null)
            {
                List<Person> presons = new List<Person>();
                presons.Add(new Person() { per_code = "", per_desc = "" });
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    presons.Add(new Person()
                    {
                        per_code = row.IsNull("PER_CODE") ? null : row["PER_CODE"].ToString(),
                        per_desc = row.IsNull("PER_DESC") ? null : row["PER_DESC"].ToString(),
                        per_jobtitle = row.IsNull("PER_JOBTITLE") ? null : row["PER_JOBTITLE"].ToString(),
                        per_trade = row.IsNull("PER_TRADE") ? null : row["PER_TRADE"].ToString(),
                    });
                }
                HttpContext.Session.SetString("ListPerson", JsonConvert.SerializeObject(presons));
                return presons;
            }
            HttpContext.Session.SetString("ListPerson", JsonConvert.SerializeObject(new List<Person>()));
            return new List<Person>();
        }
        public List<Person> LoaddropListLDG()
        {
            DataTable dt = new DataTable();
            DataSet ds;
            var WOList = new List<WOPartDtoORA>();
            OracleConnection con = new OracleConnection(vConnection);
            ds = this.GetDataSet(con, "get_LanhDaoGian", null);
            if (ds != null)
            {
                List<Person> presons = new List<Person>();
                presons.Add(new Person() { per_code = "", per_desc = "" });
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    presons.Add(new Person()
                    {
                        per_code = row.IsNull("PER_CODE") ? null : row["PER_CODE"].ToString(),
                        per_desc = row.IsNull("PER_DESC") ? null : row["PER_DESC"].ToString()
                    });
                }
                HttpContext.Session.SetString("ListLDG", JsonConvert.SerializeObject(presons));
                return presons;
            }
            HttpContext.Session.SetString("ListLDG", JsonConvert.SerializeObject(new List<Person>()));
            return new List<Person>();
        }
        public ActionResult saveClick(string gian, string bophan, string org_tbi, string matb, string tinhtrangthietbi, string nguoichophep)
        {
            string checkupdate = "";
            try
            {
                string tinhtrangtbi = tinhtrangthietbi;
                string vnguoichophep = nguoichophep;
                var PersonList = JsonConvert.DeserializeObject<List<PersonTable>>(HttpContext.Session.GetString("PersonTable"));
                string cbKSBBDSC = PersonList[1].MaPer;
                string cbLanhDaoGian = PersonList[2].MaPer;
                string cbKSVanHanh = PersonList[3].MaPer;
                string cbTruongBoPhan = PersonList[0].MaPer;
                string ngaylanhdao = PersonList[2].Ngay.Value.ToString("dd/MM/yyyy");
                string ngaytruongbophan = PersonList[0].Ngay.Value.ToString("dd/MM/yyyy");
                string ngayksvh = PersonList[3].Ngay.Value.ToString("dd/MM/yyyy");
                string ngayksbbdsc = PersonList[1].Ngay.Value.ToString("dd/MM/yyyy");

                checkupdate = UpdateWOR5(tinhtrangtbi, cbKSBBDSC, cbLanhDaoGian, cbTruongBoPhan, cbKSVanHanh, vnguoichophep, ngaylanhdao, ngaytruongbophan, ngayksvh, ngayksbbdsc);
                var x = Task.Run(async () => await updatePart(gian, bophan, org_tbi, matb, cbKSBBDSC, cbLanhDaoGian, cbTruongBoPhan, cbKSVanHanh));
                var xz = x.Result;
                checkupdate += xz;
                var WOList = JsonConvert.DeserializeObject<List<WOACTDtoORA>>(HttpContext.Session.GetString("WOListACT"));
                if (WOList != null)
                {
                    foreach (var item in WOList)
                    {
                        OracleConnection con = new OracleConnection(vConnection);
                        OracleParameter paramWO = new OracleParameter("p_wo", OracleDbType.Int32);
                        paramWO.Value = int.Parse(wocode);
                        OracleParameter paramACT = new OracleParameter("p_act", OracleDbType.Varchar2);
                        paramACT.Value = item.ACT_ACT;
                        var txtMACV = item.MaCV;
                        OracleParameter paramTask = new OracleParameter("p_taskid", OracleDbType.Varchar2);
                        paramTask.Value = (txtMACV != null) ? txtMACV : "";
                        OracleParameter paramHangMucCV = new OracleParameter("p_hangmuccongviec", OracleDbType.NVarchar2);
                        paramHangMucCV.Value = (item.hangmuccv != null) ? item.hangmuccv : "";
                        OracleParameter paramTrangthai = new OracleParameter("p_trangthai", OracleDbType.NVarchar2);
                        var txtTrangThai = item.trangthai;
                        paramTrangthai.Value = (txtTrangThai != null) ? txtTrangThai : "";
                        OracleParameter paramNote = new OracleParameter("p_note", OracleDbType.NVarchar2);
                        var txtGhichu = item.ghichu;
                        paramNote.Value = (txtGhichu != null) ? txtGhichu : "";
                        DataSet ds;
                        ds = this.GetDataSet(con, "get_WO_ACT_COMMENT_r5", new[] { paramWO, paramACT });

                        var hfisnew = item.status;
                        if (hfisnew != 0 && hfisnew == 1)
                        {
                            con = new OracleConnection(vConnection);
                            checkupdate += this.ExcuteQuery(con, "create_wo_r5_ELE_act", new[] { paramWO, paramHangMucCV, paramTrangthai });
                            if (item.ghichu.Trim().ToString() != "")
                            {
                                con = new OracleConnection(vConnection);
                                DataSet pds;
                                pds = this.GetDataSet(con, "get_WO_ACT_COMMENT_r5_newACT", new[] { paramWO });
                                if (pds != null && pds.Tables.Count > 0 && pds.Tables[0].Rows.Count > 0)
                                {
                                    string actActValue = pds.Tables[0].Rows[0]["Act_act"].ToString();
                                    paramACT.Value = actActValue;
                                    con = new OracleConnection(vConnection);
                                    checkupdate += this.ExcuteQuery(con, "CREATE_WO_ACT_COMMENT_r5", new[] { paramWO, paramACT, paramNote });
                                }
                            }
                        }
                        else if (hfisnew != 0 && hfisnew == 2)
                        {
                            con = new OracleConnection(vConnection);
                            checkupdate += this.ExcuteQuery(con, "update_WO_ACT_ELE_r5", new[] { paramWO, paramACT, paramTrangthai, paramHangMucCV });
                            if (ds != null && ds.Tables[0].Rows.Count >= 1)
                            {
                                if (txtGhichu != null)
                                {
                                    if (txtGhichu == "")
                                    {
                                        paramNote.Value = "  ";
                                    }
                                    con = new OracleConnection(vConnection);
                                    checkupdate += this.ExcuteQuery(con, "UPDATE_WO_ACT_COMMENT_r5", new[] { paramWO, paramACT, paramNote });
                                }
                            }
                            else
                            {
                                if (item.ghichu.Trim().ToString() != "")
                                {
                                    con = new OracleConnection(vConnection);
                                    checkupdate += this.ExcuteQuery(con, "CREATE_WO_ACT_COMMENT_r5", new[] { paramWO, paramACT, paramNote });
                                }
                            }
                        }
                        else if (hfisnew != 0 && hfisnew == 3)
                        {
                            con = new OracleConnection(vConnection);
                            checkupdate += this.ExcuteQuery(con, "delete_wo_r5_act", new[] { paramWO, paramACT });
                        }
                    }
                }
                var BOOList = JsonConvert.DeserializeObject<List<Boo>>(HttpContext.Session.GetString("BOOList"));
                var BOOListOrigin = JsonConvert.DeserializeObject<List<Boo>>(HttpContext.Session.GetString("BOOList_Origin"));
                foreach (var boo in BOOList)
                {
                    if (boo.status == 2)
                    {
                        OracleConnection con = new OracleConnection(vConnection);
                        OracleParameter paramWO = new OracleParameter("p_wo", OracleDbType.Varchar2);
                        paramWO.Value = wocode;
                        OracleParameter paramACT = new OracleParameter("p_act", OracleDbType.Varchar2);
                        paramACT.Value = boo.MaCV;
                        OracleParameter paramBophan = new OracleParameter("p_bophan", OracleDbType.Varchar2);
                        paramBophan.Value = bophan;
                        OracleParameter paramEmp = new OracleParameter("p_emp", OracleDbType.Varchar2);
                        paramEmp.Value = boo.MaNV;
                        OracleParameter paramtime = new OracleParameter("p_time", OracleDbType.Varchar2);
                        string hourBooOrigin = BOOListOrigin.FirstOrDefault(p => p.MaNV == boo.MaNV)?.Thoigianlamviec ?? "0";
                        paramtime.Value = Int32.Parse(boo.Thoigianlamviec) - Int32.Parse(hourBooOrigin);
                        checkupdate += this.ExcuteQuery(con, "create_WO_BOO_ELE_r5", new[] { paramWO, paramACT, paramBophan, paramEmp, paramtime });
                    }
                    else if (boo.status == 1)
                    {
                        string pact = BOOListOrigin.FirstOrDefault(p => p.MaNV == boo.MaNV)?.MaCV ?? "";
                        if (pact == "")
                        {
                            OracleConnection con2 = new OracleConnection(vConnection);
                            DataSet pds;
                            OracleParameter paramWO2 = new OracleParameter("p_wo", OracleDbType.Varchar2);
                            paramWO2.Value = wocode;
                            pds = this.GetDataSet(con2, "get_WO_BOO_r5_firstACT", new[] { paramWO2 });
                            if (pds != null && pds.Tables.Count > 0 && pds.Tables[0].Rows.Count > 0)
                            {
                                string actActValue = pds.Tables[0].Rows[0]["Act_act"].ToString();
                                pact = actActValue;
                            }
                        }
                        OracleConnection con = new OracleConnection(vConnection);
                        OracleParameter paramWO = new OracleParameter("p_wo", OracleDbType.Varchar2);
                        paramWO.Value = wocode;
                        OracleParameter paramACT = new OracleParameter("p_act", OracleDbType.Varchar2);
                        paramACT.Value = pact;
                        OracleParameter paramBophan = new OracleParameter("p_bophan", OracleDbType.Varchar2);
                        paramBophan.Value = bophan;
                        OracleParameter paramEmp = new OracleParameter("p_emp", OracleDbType.Varchar2);
                        paramEmp.Value = boo.MaNV;
                        OracleParameter paramtime = new OracleParameter("p_time", OracleDbType.Varchar2);
                        string hourBooOrigin = BOOListOrigin.FirstOrDefault(p => p.MaNV == boo.MaNV)?.Thoigianlamviec ?? "0";
                        paramtime.Value = Int32.Parse(boo.Thoigianlamviec) - Int32.Parse(hourBooOrigin);
                        checkupdate += this.ExcuteQuery(con, "create_WO_BOO_ELE_r5", new[] { paramWO, paramACT, paramBophan, paramEmp, paramtime });
                    }
                }
                var ToolList = JsonConvert.DeserializeObject<List<Tool>>(HttpContext.Session.GetString("ToolList"));
                foreach (var too in ToolList)
                {
                    if (too.status == 1)
                    {
                        string pact = "";
                        if (pact == "")
                        {
                            OracleConnection con2 = new OracleConnection(vConnection);
                            DataSet pds;
                            OracleParameter paramWO2 = new OracleParameter("p_wo", OracleDbType.Varchar2);
                            paramWO2.Value = wocode;
                            pds = this.GetDataSet(con2, "get_WO_BOO_r5_firstACT", new[] { paramWO2 });
                            if (pds != null && pds.Tables.Count > 0 && pds.Tables[0].Rows.Count > 0)
                            {
                                string actActValue = pds.Tables[0].Rows[0]["Act_act"].ToString();
                                pact = actActValue;
                            }
                        }
                        OracleConnection con = new OracleConnection(vConnection);
                        OracleParameter paramWO = new OracleParameter("p_wo", OracleDbType.Varchar2);
                        paramWO.Value = wocode;
                        OracleParameter paramACT = new OracleParameter("p_act", OracleDbType.Varchar2);
                        paramACT.Value = pact;
                        OracleParameter paramBophan = new OracleParameter("p_bophan", OracleDbType.Varchar2);
                        paramBophan.Value = bophan;
                        OracleParameter paramtool = new OracleParameter("p_tool", OracleDbType.Varchar2);
                        paramtool.Value = too.Ma_Tool;
                        checkupdate += this.ExcuteQuery(con, "create_TOOL_PLAN_ELE_r5", new[] { paramWO, paramACT, paramBophan, paramtool });
                    }
                    else if (too.status == 3)
                    {
                        OracleConnection con = new OracleConnection(vConnection);
                        OracleParameter paramWO = new OracleParameter("p_wo", OracleDbType.Varchar2);
                        paramWO.Value = wocode;
                        OracleParameter paramtool = new OracleParameter("p_tool", OracleDbType.Varchar2);
                        paramtool.Value = too.Ma_Tool;
                        checkupdate += this.ExcuteQuery(con, "delete_WO_TOOL_PLAN_r5", new[] { paramWO, paramtool });
                    }
                }
            }
            catch(Exception e)
            {
                checkupdate +=  e.StackTrace;
            }
            
            return Ok();
        }

        public List<MatlistPart> GET_MATLISTPART_BY_MATLIST(string codematlist)
        {
            DataTable dt = new DataTable();

            DataSet ds;
            var WOList = new List<MatlistPart>();
            OracleConnection con = new OracleConnection(vConnection);
            OracleParameter paramUsername = new OracleParameter("p_user", OracleDbType.Varchar2);
            paramUsername.Direction = ParameterDirection.Input;
            paramUsername.Value = codematlist;
            ds = this.GetDataSet(con, "get_PARTLIST_BY_MATLIST_r5", new[] { paramUsername });
            if (ds != null)
            {
                List<MatlistPart> Taskplan = new List<MatlistPart>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    Taskplan.Add(new MatlistPart()
                    {
                        MLP_PART = row.IsNull("MLP_PART") ? null : row["MLP_PART"].ToString(),
                        MLP_QTY = row.IsNull("MLP_QTY") ? null : row["MLP_QTY"].ToString(),
                        MLP_UOM = row.IsNull("MLP_UOM") ? null : row["MLP_UOM"].ToString(),
                    });
                }
                return Taskplan;
            }
            return null;
        }

        private async Task<string> updatePart(string gian, string bophan, string org_bi, string matb, string cbKSBBDSC, string cbLanhDaoGian, string cbTruongBoPhan, string cbKSVanHanh)
        {
            try
            {
                string checkupdate = "";
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Host", vHost);
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                    client.DefaultRequestHeaders.Add("Request-Type", "XMLHTTP");
                    //var checkupdate = "";
                    var vEamid = LoginEAM(client);
                    var eamVal = vEamid.Result.Replace("'", "");
                    var WOList = JsonConvert.DeserializeObject<List<WOPartDtoORA>>(HttpContext.Session.GetString("tablePart"));
                    var OriginPart = JsonConvert.DeserializeObject<List<WOPartDtoORA>>(HttpContext.Session.GetString("originpart"));
                    if (WOList != null)
                    {
                        string vDataSpyID = (Env_Type == "XNK") ? "100064" : "237";
                        List<WOACTDtoORA> ACTOrigin = JsonConvert.DeserializeObject<List<WOACTDtoORA>>(HttpContext.Session.GetString("WOListACTOrigin"));
                        int soluongact = ACTOrigin.Count;
                        foreach (var item in WOList)
                        {
                            if (item.status == 2)
                            {
                                string TransType = "";
                                string PlannedType = "";

                                string oldplannedQty = OriginPart.FirstOrDefault(p => p.par_code == item.par_code && p.sto_store == item.sto_store)?.mlp_qty.ToString() ?? "0";
                                //  string oldusedQty = OriginPart.FirstOrDefault(p => p.par_code == item.par_code && p.sto_store == item.sto_store).calculated_column.ToString();

                                string oldusedQty = OriginPart.FirstOrDefault(p => p.par_code == item.par_code && p.sto_store == item.sto_store)?.calculated_column?.ToString() ?? "0";

                                int soluongTrans = 0;
                                if (Int32.Parse(item.calculated_column) > Int32.Parse(oldusedQty))
                                {
                                    TransType = "ISSUE";
                                    soluongTrans = Int32.Parse(item.calculated_column) - Int32.Parse(oldusedQty);
                                }
                                else if (Int32.Parse(item.calculated_column) < Int32.Parse(oldusedQty))
                                {
                                    TransType = "RETURN";
                                    soluongTrans = Int32.Parse(oldusedQty) - Int32.Parse(item.calculated_column);
                                }
                                int SoluongPlanedChange = 0;
                                if (Int32.Parse(item.mlp_qty) > Int32.Parse(oldplannedQty))
                                {
                                    PlannedType = "ISSUE";
                                    SoluongPlanedChange = Int32.Parse(item.mlp_qty);
                                }
                                else if (Int32.Parse(item.mlp_qty) < Int32.Parse(oldplannedQty))
                                {
                                    PlannedType = "ISSUE";
                                    SoluongPlanedChange = Int32.Parse(item.mlp_qty);
                                }
                                //status 2
                                if ((SoluongPlanedChange >= 0 && Int32.Parse(oldplannedQty) != 0) || (SoluongPlanedChange > 0 && Int32.Parse(oldplannedQty) == 0))
                                {
                                    string wonum = wocode;
                                    string MaCV = item.act_act;
                                    DateTime currentTime = DateTime.Now;
                                    string transDate = currentTime.ToString("MM/dd/yyyy HH:mm");
                                    string typeTrans = PlannedType;
                                    string matlist = ACTOrigin.FirstOrDefault(p => p.ACT_ACT == MaCV)?.matlist ?? "";
                                    List<MatlistPart> matlistPartsDB = GET_MATLISTPART_BY_MATLIST(matlist);
                                    List<WOPartDto> matlistParts = JsonConvert.DeserializeObject<List<WOPartDto>>(HttpContext.Session.GetString("partTAB"));
                                    //string jsonString = JsonConvert.SerializeObject(matlistParts);
                                    //checkupdate += jsonString;
                                    string matlistlineno = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.usedqty == null && p.plannedqty == oldplannedQty)?.matlist_lineno ?? "0";
                                    if (Env_Type == "Test")
                                    {
                                        matlistlineno = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.usedqty == "0" && p.plannedqty == oldplannedQty)?.matlist_lineno ?? "0";
                                    }

                                    int soluongmatlistpart = matlistPartsDB.Count;
                                    string pUOM = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.storecode == item.sto_store)?.partuom ?? "";
                                    string bin = "*";
                                    string lot = "*";
                                    string STO_QTY = item.STO_QTY?.Replace(",", "") ?? "";
                                    string partorg = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.storecode == item.sto_store)?.partorganization ?? "";
                                    string activity_display = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.storecode == item.sto_store)?.activity_display ?? "";
                                    string PKID = item.par_code + "#" + partorg + "#" + item.act_act + "#" + item.act_act + "#@[EMPTY]#" + matlistlineno + "#" + typeTrans + "#" + transDate + "#@[EMPTY]#@[EMPTY]#@[EMPTY]#@[EMPTY]";
                                    string mlp_id = "";
                                    string matlistlineno4 = "0";
                                    if (Int32.Parse(oldplannedQty) != 0) //kiem tra neu la update plan_qty can get mlp_ip moi update duoc
                                    {
                                        string PKID4 = item.par_code + "#" + partorg + "#" + item.act_act + "#" + item.act_act + "#@[EMPTY]#" + matlistlineno4 + "#" + typeTrans + "#" + transDate + "#@[EMPTY]#@[EMPTY]#@[EMPTY]#" + STO_QTY;
                                        var url4 = vEvironment + "/web/base/WSJOBS.PAR?eamid=" + vEamid.Result.Replace("'", "") + "&tenant=" + vTennant;
                                        var values4 = new Dictionary<string, string>
                                        {
                                            { "SYSTEM_FUNCTION_NAME", "WSJOBS" },
                                            { "USER_FUNCTION_NAME", "WSJOBS" },
                                            { "CURRENT_TAB_NAME", "PAR" },
                                            { "workordernum", wocode },
                                            { "organization", gian },
                                            { "workorderrtype", "PM" },
                                            { "partcode", item.par_code },
                                            { "partorganization", partorg },
                                            { "activity", MaCV },
                                            { "dbactivity", MaCV },
                                            { "job", "" },
                                            { "matlist_lineno", matlistlineno.ToString() },
                                            { "transactiontype", typeTrans },
                                            { "transactiondate", transDate },
                                            { "equipmentcode", "" },
                                            { "is_completed", "false" },
                                            { "is_multi_equip_parent", "false" },
                                            { "totalavailableqty", STO_QTY },
                                            { "pagemode", "view" },
                                            { "extId", "" },
                                            { "headeractivity", "0" },
                                            { "headerjob", "0" },
                                            { "partdescription", item.par_desc },
                                            { "longdescription", item.par_longdescription },
                                            { "conditioncode", "" },
                                            { "partuom", pUOM },
                                            { "byasset", "0" },
                                            { "bylot", "0" },
                                            { "plannedqty", oldplannedQty },
                                            { "source", "STOCK" },
                                            { "reservedqty", "" },
                                            { "matlcode", matlist },
                                            { "pickticket", "" },
                                            { "storecode", "" },
                                            { "availableqty", STO_QTY },
                                            { "allocatedqty", "" },
                                            { "usedqty", "" },
                                            { "equipmentorganization", "" },
                                            { "relatedworkorder", "" },
                                            { "transactionquantity", "" },
                                            { "returnconditioncode", "" },
                                            { "assetid", "" },
                                            { "assetidorganization", "" },
                                            { "bincode", bin },
                                            { "lotcode", lot },
                                            { "primarymanufacturer", "" },
                                            { "primarymanufactpart", "" },
                                            { "manufacturer", "" },
                                            { "manufactpart", "" },
                                            { "toolhours", "" },
                                            { "returnforrepair", "0" },
                                            { "preventreorders", "0" },
                                            { "failedqty", "" },
                                            { "datefailed", "" },
                                            { "problemcode", "" },
                                            { "actioncode", "" },
                                            { "failurecode", "" },
                                            { "causecode", "" },
                                            { "failurenotes", "" },
                                            { "storeorganization", gian },
                                            { "is_ppm", "true" },
                                            { "partmosflag", "false" },
                                            { "parttype", "consumable_notlot" },
                                            { "can_issue", "true" },
                                            { "can_return", "true" },
                                            { "showlot", "false" },
                                            { "part_isplanned", "true" },
                                            { "matlist_ispreplanned", "false" },
                                            { "rtnany", "true" },
                                            { "matlrev", "0" },
                                            { "is_qtyfieldschanged", "" },
                                            { "delete_what", "delete_part" },
                                            { "validate_what", "" },
                                            { "mlp_recordid", "" },
                                            { "res_recordid", "" },
                                            { "wodepartmentcode", bophan },
                                            { "woequipment", matb },
                                            { "woequipmentorg", org_bi },
                                            { "originalplannedqty", "" },
                                            { "partistool", "" },
                                            { "audittablename", "R5RESERVATIONS" },
                                            { "requisitionstatus", "" },
                                            { "repairablespare", "false" },
                                            { "originalbin", bin },
                                            { "repairbin", "" },
                                            { "trackrtype", "" },
                                            { "install_issuedays", "0" },
                                            { "install_returndays", "14" },
                                            { "is_scheduled_by_msproject", "false" },
                                            { "is_scheduled_by_woloadbalance", "false" },
                                            { "is_scheduled_by_wolaborsched", "false" },
                                            { "is_scheduling", "false" },
                                            { "number_of_activities", soluongact.ToString() },
                                            { "is_workrequest", "false" },
                                            { "jtauth_can_update", "true" },
                                            { "is_multi_equip_child", "false" },
                                            { "has_multi_child_closed", "false" },
                                            { "all_multi_child_closed", "false" },
                                            { "splittoopenonly", "false" },
                                            { "confirmaddpart", "prompt" },
                                            { "deptsecreadonly", "false" },
                                            { "number_of_matlist_parts", soluongmatlistpart.ToString() }, //SOLUONG PART CUA MATLIST
                                            { "install_manupart", "NO" },
                                            { "is_ppm_revision_control_on", "false" },
                                            { "pmrvctrllist", "" },
                                            { "is_project_frozen", "false" },
                                            { "is_campaign_onhold", "false" },
                                            { "orgoption_clgroup", "OFF" },
                                            { "parentpart", "" },
                                            { "trackbycondition", "false" },
                                            { "activity_display", activity_display },
                                            { "job_display", "" },
                                            { "showjob", "YES" },
                                            { "numberofjobs", "" },
                                            { "stock", "0" },
                                            { "can_insert", "true" },
                                            { "can_delete", "true" },
                                            { "can_update", "true" },
                                            { "recordid", "" },
                                            { "refreshpagadata", "true" },
                                            { "id", "" },
                                            { "PKID", PKID4},
                                            { "ONLY_DATA_REQUIRED", "true" },
                                        };
                                        //string result = DictionaryToString(values4);
                                        //checkupdate += result;
                                        var data4 = new FormUrlEncodedContent(values4);
                                        var response4 = await client.PostAsync(url4, data4);
                                        response4.EnsureSuccessStatusCode();
                                        var resultBytes4 = await response4.Content.ReadAsByteArrayAsync();
                                        var decompressedResult4 = Decompress(resultBytes4);
                                        var resultStringt4 = Encoding.UTF8.GetString(decompressedResult4);
                                        var jsonObject = JObject.Parse(resultStringt4);
                                        var Rmessage = jsonObject["pageData"]["messages"].ToString();
                                        if (Rmessage != "")
                                        {
                                            checkupdate += Environment.NewLine;
                                            checkupdate += "Vật tư - Line: " + item.idRow + " " + Rmessage;
                                        }
                                        else
                                        {
                                            var mlpRecordId = jsonObject["pageData"]["values"]["mlp_recordid"].ToString();
                                            mlp_id = mlpRecordId;
                                        }
                                    }

                                    var url3 = vEvironment + "/web/base/WSJOBS.PAR?pageaction=SAVE&eamid=" + vEamid.Result.Replace("'", "") + "&tenant=" + vTennant;
                                    var values3 = new Dictionary<string, string>
                                    {
                                        { "GRID_ID", "226" },
                                        { "GRID_NAME", "WSJOBS_PAR" },
                                        { "DATASPY_ID", vDataSpyID },
                                        { "SYSTEM_FUNCTION_NAME", "WSJOBS" },
                                        { "USER_FUNCTION_NAME", "WSJOBS" },
                                        { "CURRENT_TAB_NAME", "PAR" },
                                        { "headeractivity", "0" },
                                        { "headerjob", "0" },
                                        { "activity", MaCV },  //activity
	                                    { "job", "" },
                                        { "partcode", item.par_code }, //partcode
	                                    { "partdescription", item.par_desc }, //partdescription
	                                    { "longdescription", "" },
                                        { "conditioncode", "" },  //conditioncode
	                                    { "partuom", pUOM },  //partuom
	                                    { "byasset", "0" },
                                        { "bylot", "0" },
                                        { "plannedqty", item.mlp_qty.ToString() },  //plannedqty  
	                                    { "source", "STOCK" },  //source not change
	                                    { "reservedqty", "" },  //reservedqty
	                                    { "partorganization", partorg }, //partorganization
	                                    { "matlcode", matlist },
                                        { "pickticket", "" },
                                        { "storecode", "" },  //storecode edit
	                                    { "availableqty", "" },
                                        { "allocatedqty", "" },  //allocatedqty
	                                    { "usedqty", "" },  //usedqty old qty
	                                    { "transactiontype", typeTrans },
                                        { "transactiondate", transDate },
                                        { "equipmentcode", "" },
                                        { "equipmentorganization", "" },
                                        { "relatedworkorder", "" },
                                        { "transactionquantity", "" }, //soluong trans
	                                    { "returnconditioncode", "" },
                                        { "assetid", "" },
                                        { "assetidorganization", "" },
                                        { "bincode", "" },
                                        { "lotcode", "" },
                                        { "primarymanufacturer", "" },
                                        { "primarymanufactpart", "" },  //manufactpart
	                                    { "manufacturer", "" },
                                        { "manufactpart", "" },
                                        { "toolhours", "" },
                                        { "totalavailableqty", "" },
                                        { "returnforrepair", "0" },
                                        { "preventreorders", "0" },  //preventreorders
	                                    { "failedqty", "" },
                                        { "datefailed", "" },
                                        { "problemcode", "" },
                                        { "actioncode", "" },
                                        { "failurecode", "" },
                                        { "causecode", "" },
                                        { "failurenotes", "" },
                                        { "workordernum", wocode },
                                        { "dbactivity", MaCV }, //dbactivity - NEEDEDIT
	                                    { "organization", gian },
                                        { "storeorganization", gian },
                                        { "is_completed", "false" },
                                        { "is_ppm", "true" },
                                        { "partmosflag", "false" },
                                        { "parttype", "consumable_notlot" },
                                        { "can_issue", "true" },
                                        { "can_return", "true" },
                                        { "showlot", "false" },
                                        { "part_isplanned", "true" },
                                        { "matlist_ispreplanned", "false" },
                                        { "rtnany", "true" },
                                        { "matlist_lineno", matlistlineno.ToString() }, //matlist_lineno        hmmmm need data
	                                    { "matlrev", "0" },
                                        { "is_qtyfieldschanged", "true" },
                                        { "delete_what", "delete_part" },
                                        { "validate_what", "plannedqty" },
                                        { "mlp_recordid", mlp_id },       // Tim cach lay mlp_recordid
	                                    { "res_recordid", "" },
                                        { "wodepartmentcode", bophan },
                                        { "woequipment", matb }, //not found equip
	                                    { "woequipmentorg", org_bi },
                                        { "originalplannedqty", oldplannedQty },
                                        { "partistool", "" },
                                        { "audittablename", "R5RESERVATIONS" },
                                        { "requisitionstatus", "" },
                                        { "repairablespare", "false" },
                                        { "originalbin", "" }, //not use originalbin
	                                    { "repairbin", "" },
                                        { "trackrtype", "" },
                                        { "install_issuedays", "0" },
                                        { "install_returndays", "14" },
                                        { "is_scheduled_by_msproject", "false" },
                                        { "is_scheduled_by_woloadbalance", "false" },
                                        { "is_scheduled_by_wolaborsched", "false" },
                                        { "is_scheduling", "false" },
                                        { "number_of_activities", soluongact.ToString() },
                                        { "is_workrequest", "false" },
                                        { "jtauth_can_update", "true" },
                                        { "is_multi_equip_child", "false" },
                                        { "is_multi_equip_parent", "false" },
                                        { "has_multi_child_closed", "false" },
                                        { "all_multi_child_closed", "false" },
                                        { "splittoopenonly", "false" },
                                        { "confirmaddpart", "prompt" },
                                        { "deptsecreadonly", "false" },
                                        { "number_of_matlist_parts", soluongmatlistpart.ToString() }, //SOLUONG PART CUA MATLIST
	                                    { "install_manupart", "NO" },
                                        { "is_ppm_revision_control_on", "false" },
                                        { "pmrvctrllist", "" },
                                        { "is_project_frozen", "false" },
                                        { "is_campaign_onhold", "false" },
                                        { "orgoption_clgroup", "OFF" },
                                        { "parentpart", "" },  //parentpart
	                                    { "trackbycondition", "false" },  //trackbycondition
	                                    { "activity_display", activity_display },  //activity_display
	                                    { "job_display", "" },
                                        { "showjob", "YES" },
                                        { "numberofjobs", "" },
                                        { "stock", "0" },  //stock  not change
	                                    { "can_insert", "true" },
                                        { "can_delete", "true" },
                                        { "can_update", "true" },
                                        { "pagemode", "view" },
                                        { "recordid", "" },
                                        { "refreshpagadata", "true" },
                                        { "id", "" },
                                        { "PKID", PKID}
                                    };
                                    //string result2 = DictionaryToString(values3);
                                    //checkupdate += result2;
                                    var data3 = new FormUrlEncodedContent(values3);
                                    var response3 = await client.PostAsync(url3, data3);
                                    response3.EnsureSuccessStatusCode();
                                    var resultBytes3 = await response3.Content.ReadAsByteArrayAsync();
                                    var decompressedResult3 = Decompress(resultBytes3);
                                    var resultString3 = Encoding.UTF8.GetString(decompressedResult3);
                                    var jsonObject2 = JObject.Parse(resultString3);
                                    var Rmessage2 = jsonObject2["pageData"]["messages"].ToString();
                                    if (Rmessage2 != "")
                                    {
                                        checkupdate += Environment.NewLine;
                                        checkupdate += "Vật tư - Line: " + item.idRow + " " + Rmessage2;
                                    }
                                }

                                if (soluongTrans != 0)
                                {

                                    string wonum = wocode;
                                    string MaCV = item.act_act;
                                    DateTime currentTime = DateTime.Now;
                                    string transDate = currentTime.ToString("MM/dd/yyyy HH:mm");
                                    string typeTrans = TransType;
                                    string matlist = ACTOrigin.FirstOrDefault(p => p.ACT_ACT == MaCV)?.matlist ?? "";
                                    List<MatlistPart> matlistPartsDB = GET_MATLISTPART_BY_MATLIST(matlist);
                                    List<WOPartDto> matlistParts = JsonConvert.DeserializeObject<List<WOPartDto>>(HttpContext.Session.GetString("partTAB"));
                                    string matlistlineno = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.storecode == item.sto_store)?.matlist_lineno ?? "0";
                                    string pUOM = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.storecode == item.sto_store)?.partuom ?? "";
                                    string soluongmatlistpart = "";
                                    if (matlist == "")
                                    {
                                        soluongmatlistpart = "";
                                    }
                                    else
                                    {
                                        soluongmatlistpart = matlistPartsDB.Count.ToString();
                                    }
                                    string bin = "*";
                                    string lot = "*";
                                    string partorg = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.storecode == item.sto_store)?.partorganization ?? "";
                                    string activity_display = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.storecode == item.sto_store)?.activity_display ?? "";
                                    string STO_QTY = item.STO_QTY?.Replace(",", "") ?? "";
                                    var url3 = vEvironment + "/web/base/WSJOBS.PAR?pageaction=SAVE&eamid=" + vEamid.Result.Replace("'", "") + "&tenant=" + vTennant;
                                    var values3 = new Dictionary<string, string>
                                    {
                                        { "GRID_ID", "226" },
                                        { "GRID_NAME", "WSJOBS_PAR" },
                                        { "DATASPY_ID", vDataSpyID },
                                        { "SYSTEM_FUNCTION_NAME", "WSJOBS" },
                                        { "USER_FUNCTION_NAME", "WSJOBS" },
                                        { "CURRENT_TAB_NAME", "PAR" },
                                        { "headeractivity", "0" },
                                        { "headerjob", "0" },
                                        { "activity", item.act_act },  //activity
                                        { "job", "" }, //job
	                                    { "partcode", item.par_code }, //partcode
	                                    { "partdescription", item.par_desc }, //partdescription
	                                    { "longdescription", item.par_longdescription },
                                        { "conditioncode", "" },  //conditioncode	
	                                    { "partuom", pUOM },  //partuom
	                                    { "byasset", "0" },
                                        { "bylot", "0" },
                                        { "plannedqty", "" },  //plannedqty  
	                                    { "source", "STOCK" },  //source not change
	                                    { "reservedqty", "" },  //reservedqty
	                                    { "partorganization", partorg }, //partorganization 
	                                    { "matlcode", matlist },
                                        { "pickticket", "" },
                                        { "storecode", item.sto_store },  //storecode edit
	                                    { "availableqty", STO_QTY },
                                        { "allocatedqty", "" },  //allocatedqty
	                                    { "usedqty", item.oldUsedQty.ToString() },  //usedqty old qty
	                                    { "transactiontype", typeTrans },
                                        { "transactiondate", transDate },
                                        { "equipmentcode", "" },
                                        { "equipmentorganization", "" },
                                        { "relatedworkorder", "" },
                                        { "transactionquantity", soluongTrans.ToString() }, //soluong trans
	                                    { "returnconditioncode", "" },
                                        { "assetid", "" },
                                        { "assetidorganization", "" },
                                        { "bincode", bin },
                                        { "lotcode", lot },
                                        { "primarymanufacturer", "" },
                                        { "primarymanufactpart", "" },  //manufactpart
	                                    { "manufacturer", "" },
                                        { "manufactpart", "" },
                                        { "toolhours", "" },
                                        { "totalavailableqty", STO_QTY },
                                        { "returnforrepair", "0" },
                                        { "preventreorders", "0" },  //preventreorders
	                                    { "failedqty", "" },
                                        { "datefailed", "" },
                                        { "problemcode", "" },
                                        { "actioncode", "" },
                                        { "failurecode", "" },
                                        { "causecode", "" },
                                        { "failurenotes", "" },
                                        { "workordernum", wocode },
                                        { "dbactivity", MaCV }, //dbactivity - NEEDEDIT
	                                    { "organization", gian },
                                        { "storeorganization", gian },
                                        { "is_completed", "false" },
                                        { "is_ppm", "true" },
                                        { "partmosflag", "false" },
                                        { "parttype", "consumable_notlot" },
                                        { "can_issue", "true" },
                                        { "can_return", "true" },
                                        { "showlot", "false" },
                                        { "part_isplanned", "false" },
                                        { "matlist_ispreplanned", "" },
                                        { "rtnany", "true" },
                                        { "matlist_lineno", matlistlineno.ToString() }, //matlist_lineno
	                                    { "matlrev", "" },
                                        { "is_qtyfieldschanged", "" },
                                        { "delete_what", "" },
                                        { "validate_what", "transactionquantity" },
                                        { "mlp_recordid", "" },
                                        { "res_recordid", "" },
                                        { "wodepartmentcode", bophan },
                                        { "woequipment", matb }, //not found equip
	                                    { "woequipmentorg", org_bi },
                                        { "originalplannedqty", "" },
                                        { "partistool", "" },
                                        { "audittablename", "R5RESERVATIONS" },
                                        { "requisitionstatus", "" },
                                        { "repairablespare", "false" },
                                        { "originalbin", bin },
                                        { "repairbin", "" },
                                        { "trackrtype", "" },
                                        { "install_issuedays", "0" },
                                        { "install_returndays", "14" },
                                        { "is_scheduled_by_msproject", "false" },
                                        { "is_scheduled_by_woloadbalance", "false" },
                                        { "is_scheduled_by_wolaborsched", "false" },
                                        { "is_scheduling", "false" },
                                        { "number_of_activities", soluongact.ToString() },
                                        { "is_workrequest", "false" },
                                        { "jtauth_can_update", "true" },
                                        { "is_multi_equip_child", "false" },
                                        { "is_multi_equip_parent", "false" },
                                        { "has_multi_child_closed", "false" },
                                        { "all_multi_child_closed", "false" },
                                        { "splittoopenonly", "false" },
                                        { "confirmaddpart", "prompt" },
                                        { "deptsecreadonly", "false" },
                                        { "number_of_matlist_parts", soluongmatlistpart.ToString() }, //SOLUONG PART CUA MATLIST
	                                    { "install_manupart", "NO" },
                                        { "is_ppm_revision_control_on", "false" },
                                        { "pmrvctrllist", "" },
                                        { "is_project_frozen", "false" },
                                        { "is_campaign_onhold", "false" },
                                        { "orgoption_clgroup", "OFF" },
                                        { "parentpart", "" },  //parentpart
	                                    { "trackbycondition", "false" },  //trackbycondition
	                                    { "activity_display", activity_display },  //activity_display
	                                    { "job_display", "" },
                                        { "showjob", "YES" },
                                        { "numberofjobs", "" },
                                        { "stock", "0" },  //stock  not change
	                                    { "can_insert", "true" },
                                        { "can_delete", "true" },
                                        { "can_update", "true" },
                                        { "pagemode", "view" },
                                        { "recordid", "" },
                                        { "refreshpagadata", "true" },
                                        { "id", "" },
                                        { "PKID", item.par_code+"#"+partorg+"#"+item.act_act+"#"+item.act_act+"#@[EMPTY]#"+matlistlineno+"#"+TransType+"#"+transDate+"#@[EMPTY]#@[EMPTY]#@[EMPTY]#"+ STO_QTY},
                                    };
                                    var data3 = new FormUrlEncodedContent(values3);
                                    var response3 = await client.PostAsync(url3, data3);
                                    response3.EnsureSuccessStatusCode();
                                    var resultBytes3 = await response3.Content.ReadAsByteArrayAsync();
                                    var decompressedResult3 = Decompress(resultBytes3);
                                    var resultString3 = Encoding.UTF8.GetString(decompressedResult3);
                                    var jsonObject = JObject.Parse(resultString3);
                                    var Rmessage = jsonObject["pageData"]["messages"].ToString();
                                    if (Rmessage != "")
                                    {
                                        checkupdate += Environment.NewLine;
                                        checkupdate += "Vật tư - Line: " + item.idRow + " " + Rmessage;
                                    }
                                }
                            }

                            if (item.status == 1)
                            {
                                string TransType = "";
                                string PlannedType = "";

                                TransType = "ISSUE";
                                int soluongTrans = Int32.Parse(item.calculated_column);

                                PlannedType = "ISSUE";
                                int SoluongPlanedChange = Int32.Parse(item.mlp_qty);
                                //string oldplannedQty = OriginPart.FirstOrDefault(p => p.par_code == item.par_code && p.sto_store == item.sto_store).mlp_qty.ToString();
                                string oldplannedQty = "";
                                if (soluongTrans > 0)
                                {
                                    string wonum = wocode;
                                    string MaCV = "";
                                    string activity_display = "";
                                    if (ACTOrigin != null && ACTOrigin.Any())
                                    {
                                        MaCV = ACTOrigin.First().ACT_ACT;
                                        if (ACTOrigin.First().hangmuccv != null && ACTOrigin.First().hangmuccv != "")
                                        {
                                            activity_display = MaCV + " - " + ACTOrigin.First().hangmuccv;
                                        }
                                    }
                                    DateTime currentTime = DateTime.Now;
                                    string transDate = currentTime.ToString("MM/dd/yyyy HH:mm");
                                    string typeTrans = TransType;
                                    string matlist = ACTOrigin.FirstOrDefault(p => p.ACT_ACT == MaCV)?.matlist ?? "";
                                    List<MatlistPart> matlistPartsDB = GET_MATLISTPART_BY_MATLIST(matlist);
                                    List<WOPartDto> matlistParts = JsonConvert.DeserializeObject<List<WOPartDto>>(HttpContext.Session.GetString("partTAB"));
                                    List<BinStockDto> BinstockList = JsonConvert.DeserializeObject<List<BinStockDto>>(HttpContext.Session.GetString("BinStockList"));
                                    string matlistlineno = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.storecode == item.sto_store)?.matlist_lineno ?? "";
                                    int soluongmatlistpart = matlistPartsDB.Count;
                                    string pUOM = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.storecode == item.sto_store)?.partuom ?? "";
                                    string trade = "*";
                                    string bin = "*";
                                    string lot = "*";
                                    string partorg = BinstockList.FirstOrDefault(p => p.BIS_PART == item.par_code && p.BIS_STORE == item.sto_store && p.BIS_BIN == "*" && p.BIS_LOT == "*")?.BIS_PART_ORG ?? "";
                                    string STO_QTY = BinstockList.FirstOrDefault(p => p.BIS_PART == item.par_code && p.BIS_STORE == item.sto_store && p.BIS_BIN == "*" && p.BIS_LOT == "*")?.BIS_QTY.ToString() ?? "";
                                    var url3 = vEvironment + "/web/base/WSJOBS.PAR?pageaction=SAVE&eamid=" + vEamid.Result.Replace("'", "") + "&tenant=" + vTennant;

                                    //checkupdate += "PKID Ins P: " + item.par_code + "#" + partorg + "#" + MaCV + "#@[EMPTY]#@[EMPTY]#@[EMPTY]#" + TransType + "#" + transDate + "#@[EMPTY]#@[EMPTY]#@[EMPTY]#" + STO_QTY;
                                    var values3 = new Dictionary<string, string>
                                    {
                                        { "GRID_ID", "226" },
                                        { "GRID_NAME", "WSJOBS_PAR" },
                                        { "DATASPY_ID", vDataSpyID },
                                        { "SYSTEM_FUNCTION_NAME", "WSJOBS" },
                                        { "USER_FUNCTION_NAME", "WSJOBS" },
                                        { "CURRENT_TAB_NAME", "PAR" },
                                        { "headeractivity", "0" },
                                        { "headerjob", "0" },
                                        { "activity", MaCV },  //activity
	                                    { "job", "" }, //job
	                                    { "partcode", item.par_code }, //partcode
	                                    { "partdescription", item.par_desc }, //partdescription
	                                    { "longdescription", item.par_longdescription },
                                        { "conditioncode", "" },  //conditioncode
	                                    { "partuom", pUOM },  //partuom
	                                    { "byasset", "0" },
                                        { "bylot", "0" },
                                        { "plannedqty", "" },  //plannedqty  
	                                    { "source", "STOCK" },  //source not change
	                                    { "reservedqty", "" },  //reservedqty
	                                    { "partorganization", partorg }, //partorganization 
	                                    { "matlcode", matlist },
                                        { "pickticket", "" },
                                        { "storecode", item.sto_store },  //storecode edit
	                                    { "availableqty", STO_QTY },
                                        { "allocatedqty", "" },  //allocatedqty
	                                    { "usedqty", "" },  //usedqty old qty
	                                    { "transactiontype", typeTrans },
                                        { "transactiondate", transDate },
                                        { "equipmentcode", "" },
                                        { "equipmentorganization", "" },///
                                        { "relatedworkorder", "" },
                                        { "transactionquantity", soluongTrans.ToString() }, //soluong trans
	                                    { "returnconditioncode", "" },
                                        { "assetidorganization", "" },
                                        { "bincode", bin },
                                        { "lotcode", lot },
                                        { "primarymanufacturer", "" },
                                        { "primarymanufactpart", "" },  //manufactpart
	                                    { "manufacturer", "" },
                                        { "manufactpart", "" },
                                        { "toolhours", "" },
                                        { "totalavailableqty", STO_QTY },
                                        { "returnforrepair", "0" },
                                        { "preventreorders", "0" },  //preventreorders
	                                    { "failedqty", "" },
                                        { "datefailed", "" },
                                        { "problemcode", "" },
                                        { "actioncode", "" },
                                        { "failurecode", "" },  ///
	                                    { "causecode", "" },
                                        { "failurenotes", "" },
                                        { "workordernum", wocode },
                                        { "dbactivity", "" }, //dbactivity - NEEDEDIT
	                                    { "organization", gian },
                                        { "storeorganization", gian },
                                        { "is_completed", "false" },
                                        { "is_ppm", "true" },
                                        { "partmosflag", "false" },
                                        { "parttype", "consumable_notlot" },
                                        { "can_issue", "true" },
                                        { "can_return", "true" },
                                        { "showlot", "false" }, ///
	                                    { "part_isplanned", "false" },
                                        { "matlist_ispreplanned", "" },
                                        { "rtnany", "true" },
                                        { "matlist_lineno", "" }, //matlist_lineno        hmmmm need data
	                                    { "matlrev", "" },
                                        { "is_qtyfieldschanged", "" },
                                        { "delete_what", "" },
                                        { "validate_what", "transactionquantity" },
                                        { "mlp_recordid", "" },
                                        { "res_recordid", "" },
                                        { "wodepartmentcode", bophan },
                                        { "woequipment", matb }, //not found equip
	                                    { "woequipmentorg", org_bi },
                                        { "originalplannedqty", "" },
                                        { "partistool", "" },
                                        { "audittablename", "R5RESERVATIONS" },
                                        { "requisitionstatus", "" },
                                        { "repairablespare", "false" },
                                        { "originalbin", bin },
                                        { "repairbin", "" },
                                        { "trackrtype", "TRPQ" },
                                        { "install_issuedays", "0" },
                                        { "install_returndays", "14" },
                                        { "is_scheduled_by_msproject", "false" },
                                        { "is_scheduled_by_woloadbalance", "false" },
                                        { "is_scheduled_by_wolaborsched", "false" },
                                        { "is_scheduling", "false" },
                                        { "number_of_activities", soluongact.ToString() },
                                        { "is_workrequest", "false" },
                                        { "jtauth_can_update", "true" },
                                        { "is_multi_equip_child", "false" },
                                        { "is_multi_equip_parent", "false" },
                                        { "has_multi_child_closed", "false" },
                                        { "all_multi_child_closed", "false" },
                                        { "splittoopenonly", "false" },
                                        { "confirmaddpart", "prompt" },
                                        { "deptsecreadonly", "false" },
                                        { "number_of_matlist_parts", soluongmatlistpart.ToString() }, //SOLUONG PART CUA MATLIST
	                                    { "install_manupart", "NO" },
                                        { "is_ppm_revision_control_on", "false" },
                                        { "pmrvctrllist", "" },
                                        { "is_project_frozen", "false" },
                                        { "is_campaign_onhold", "false" },
                                        { "orgoption_clgroup", "OFF" },
                                        { "parentpart", "" },  //parentpart
	                                    { "trackbycondition", "false" },  //trackbycondition
	                                    { "activity_display", activity_display },  //activity_display  edit nay lai
	                                    { "job_display", "" },
                                        { "showjob", "YES" },
                                        { "numberofjobs", "" },
                                        { "stock", "0" },  //stock  not change
	                                    { "can_insert", "true" },
                                        { "can_delete", "true" },
                                        { "can_update", "true" },
                                        { "pagemode", "display" },
                                        { "recordid", "" },
                                        { "refreshpagadata", "true" },
                                        { "PKID", item.par_code+"#"+partorg+"#"+MaCV+"#@[EMPTY]#@[EMPTY]#@[EMPTY]#"+TransType+"#"+transDate+"#@[EMPTY]#@[EMPTY]#@[EMPTY]#"+ STO_QTY}
                                    };
                                    var data3 = new FormUrlEncodedContent(values3);
                                    var response3 = await client.PostAsync(url3, data3);
                                    response3.EnsureSuccessStatusCode();
                                    var resultBytes3 = await response3.Content.ReadAsByteArrayAsync();
                                    var decompressedResult3 = Decompress(resultBytes3);
                                    var resultString3 = Encoding.UTF8.GetString(decompressedResult3);
                                    var jsonObject = JObject.Parse(resultString3);
                                    var Rmessage = jsonObject["pageData"]["messages"].ToString();
                                    if (Rmessage != "")
                                    {
                                        checkupdate += Environment.NewLine;
                                        checkupdate += "Vật tư - Line: " + item.idRow + " " + Rmessage;
                                    }
                                }

                                if (SoluongPlanedChange > 0)
                                {
                                    string wonum = wocode;
                                    string MaCV = item.act_act;
                                    string activity_display = "";

                                    if (ACTOrigin != null && ACTOrigin.Any())
                                    {
                                        MaCV = ACTOrigin.First().ACT_ACT;
                                        if (ACTOrigin.First().hangmuccv != null && ACTOrigin.First().hangmuccv != "")
                                        {
                                            activity_display = MaCV + " - " + ACTOrigin.First().hangmuccv;
                                        }
                                    }
                                    DateTime currentTime = DateTime.Now;
                                    string transDate = currentTime.ToString("MM/dd/yyyy HH:mm");
                                    string typeTrans = PlannedType;
                                    string matlist = ACTOrigin.FirstOrDefault(p => p.ACT_ACT == MaCV)?.matlist ?? "";
                                    List<MatlistPart> matlistPartsDB = GET_MATLISTPART_BY_MATLIST(matlist);
                                    List<WOPartDto> matlistParts = JsonConvert.DeserializeObject<List<WOPartDto>>(HttpContext.Session.GetString("partTAB"));
                                    List<BinStockDto> BinstockList = JsonConvert.DeserializeObject<List<BinStockDto>>(HttpContext.Session.GetString("BinStockList"));
                                    string matlistlineno = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.storecode == item.sto_store)?.matlist_lineno ?? "";
                                    int soluongmatlistpart = matlistPartsDB.Count;
                                    string pUOM = matlistParts.FirstOrDefault(p => p.partcode == item.par_code && p.storecode == item.sto_store)?.partuom ?? "";
                                    string trade = "*";
                                    string bin = "*";
                                    string lot = "*";
                                    string partorg = BinstockList.FirstOrDefault(p => p.BIS_PART == item.par_code && p.BIS_STORE == item.sto_store && p.BIS_BIN == "*" && p.BIS_LOT == "*")?.BIS_PART_ORG ?? "";
                                    string STO_QTY = BinstockList.FirstOrDefault(p => p.BIS_PART == item.par_code && p.BIS_STORE == item.sto_store && p.BIS_BIN == "*" && p.BIS_LOT == "*")?.BIS_QTY.ToString() ?? "";
                                    string PKID = item.par_code + "#" + partorg + "#" + MaCV + "#" + "@[EMPTY]" + "#@[EMPTY]#@[EMPTY]#" + TransType + "#" + transDate + "#@[EMPTY]#@[EMPTY]#@[EMPTY]#" + STO_QTY;
                                    var url3 = vEvironment + "/web/base/WSJOBS.PAR?pageaction=SAVE&eamid=" + vEamid.Result.Replace("'", "") + "&tenant=" + vTennant;
                                    var values3 = new Dictionary<string, string>
                                    {
                                        { "GRID_ID", "226" },
                                        { "GRID_NAME", "WSJOBS_PAR" },
                                        { "DATASPY_ID", vDataSpyID },
                                        { "SYSTEM_FUNCTION_NAME", "WSJOBS" },
                                        { "USER_FUNCTION_NAME", "WSJOBS" },
                                        { "CURRENT_TAB_NAME", "PAR" },
                                        { "headeractivity", "0" },
                                        { "headerjob", "0" },
                                        { "activity", MaCV },  //activity
	                                    { "job", "" },
                                        { "partcode", item.par_code }, //partcode
	                                    { "partdescription", item.par_desc }, //partdescription
	                                    { "longdescription", "" },
                                        { "conditioncode", "" },  //conditioncode
	                                    { "partuom", pUOM },  //partuom
	                                    { "byasset", "0" },
                                        { "bylot", "0" },
                                        { "plannedqty", item.mlp_qty.ToString() },  //plannedqty  
	                                    { "source", "STOCK" },  //source not change
	                                    { "reservedqty", "" },  //reservedqty
	                                    { "partorganization", partorg }, //partorganization
	                                    { "matlcode", matlist },
                                        { "pickticket", "" },
                                        { "storecode", item.sto_store },  //storecode edit
	                                    { "availableqty", STO_QTY },
                                        { "allocatedqty", "" },  //allocatedqty
	                                    { "usedqty", item.oldUsedQty.ToString() },  //usedqty old qty
	                                    { "transactiontype", typeTrans },
                                        { "transactiondate", transDate },
                                        { "equipmentcode", "" },
                                        { "equipmentorganization", "" },
                                        { "relatedworkorder", "" },
                                        { "transactionquantity", "" }, //soluong trans
	                                    { "returnconditioncode", "" },
                                        { "assetid", "" },
                                        { "assetidorganization", "" },
                                        { "bincode", bin },
                                        { "lotcode", lot },
                                        { "primarymanufacturer", "" },
                                        { "primarymanufactpart", "" },  //manufactpart
	                                    { "manufacturer", "" },
                                        { "manufactpart", "" },
                                        { "toolhours", "" },
                                        { "totalavailableqty", STO_QTY },
                                        { "returnforrepair", "0" },
                                        { "preventreorders", "0" },  //preventreorders
	                                    { "failedqty", "" },
                                        { "datefailed", "" },
                                        { "problemcode", "" },
                                        { "actioncode", "" },
                                        { "failurecode", "" },
                                        { "causecode", "" },
                                        { "failurenotes", "" },
                                        { "workordernum", wocode },
                                        { "dbactivity", MaCV }, //dbactivity - NEEDEDIT
	                                    { "organization", gian },
                                        { "storeorganization", gian },
                                        { "is_completed", "false" },
                                        { "is_ppm", "true" },
                                        { "partmosflag", "false" },
                                        { "parttype", "consumable_notlot" },
                                        { "can_issue", "true" },
                                        { "can_return", "true" },
                                        { "showlot", "false" },
                                        { "part_isplanned", "false" },
                                        { "matlist_ispreplanned", "" },
                                        { "rtnany", "true" },
                                        { "matlist_lineno", matlistlineno.ToString() }, //matlist_lineno        hmmmm need data
	                                    { "matlrev", "0" },
                                        { "is_qtyfieldschanged", "true" },
                                        { "delete_what", "delete_part" },
                                        { "validate_what", "plannedqty" },
                                        { "mlp_recordid", "" },       // Tim cach lay mlp_recordid
	                                    { "res_recordid", "" },
                                        { "wodepartmentcode", bophan },
                                        { "woequipment", matb }, //not found equip
	                                    { "woequipmentorg", org_bi },
                                        { "originalplannedqty", oldplannedQty },
                                        { "partistool", "" },
                                        { "audittablename", "R5RESERVATIONS" },
                                        { "requisitionstatus", "" },
                                        { "repairablespare", "false" },
                                        { "originalbin", bin }, //not use originalbin
	                                    { "repairbin", "" },
                                        { "trackrtype", "" },
                                        { "install_issuedays", "0" },
                                        { "install_returndays", "14" },
                                        { "is_scheduled_by_msproject", "false" },
                                        { "is_scheduled_by_woloadbalance", "false" },
                                        { "is_scheduled_by_wolaborsched", "false" },
                                        { "is_scheduling", "false" },
                                        { "number_of_activities", soluongact.ToString() },
                                        { "is_workrequest", "false" },
                                        { "jtauth_can_update", "true" },
                                        { "is_multi_equip_child", "false" },
                                        { "is_multi_equip_parent", "false" },
                                        { "has_multi_child_closed", "false" },
                                        { "all_multi_child_closed", "false" },
                                        { "splittoopenonly", "false" },
                                        { "confirmaddpart", "prompt" },
                                        { "deptsecreadonly", "false" },
                                        { "number_of_matlist_parts", soluongmatlistpart.ToString() }, //SOLUONG PART CUA MATLIST
	                                    { "install_manupart", "NO" },
                                        { "is_ppm_revision_control_on", "false" },
                                        { "pmrvctrllist", "" },
                                        { "is_project_frozen", "false" },
                                        { "is_campaign_onhold", "false" },
                                        { "orgoption_clgroup", "OFF" },
                                        { "parentpart", "" },  //parentpart
	                                    { "trackbycondition", "false" },  //trackbycondition
	                                    { "activity_display", activity_display },  //activity_display
	                                    { "job_display", "" },
                                        { "showjob", "YES" },
                                        { "numberofjobs", "" },
                                        { "stock", "0" },  //stock  not change
	                                    { "can_insert", "true" },
                                        { "can_delete", "true" },
                                        { "can_update", "true" },
                                        { "pagemode", "view" },
                                        { "recordid", "" },
                                        { "refreshpagadata", "true" },
                                        { "id", "" },
                                        { "PKID", PKID}
                                    };
                                    var data3 = new FormUrlEncodedContent(values3);
                                    var response3 = await client.PostAsync(url3, data3);
                                    response3.EnsureSuccessStatusCode();
                                    var resultBytes3 = await response3.Content.ReadAsByteArrayAsync();
                                    var decompressedResult3 = Decompress(resultBytes3);
                                    var resultString3 = Encoding.UTF8.GetString(decompressedResult3);
                                    var jsonObject = JObject.Parse(resultString3);
                                    var Rmessage = jsonObject["pageData"]["messages"].ToString();
                                    if (Rmessage != "")
                                    {
                                        checkupdate += Environment.NewLine;
                                        checkupdate += "Vật tư - Line: " + item.idRow + " " + Rmessage;
                                    }
                                }


                            }
                        }
                    }
                    //var vlogout = LogoutEAM(client, eamVal, vTennant);
                }
                return checkupdate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi: {ex.Message}");
                return ex.Message;

            }
        }

        public string UpdateWOR5(string tinhtrangtbi, string cbKSBBDSC, string cbLanhDaoGian, string cbTruongBoPhan, string cbKSVanHanh, string cbnguoichophep, string ngaylanhdao, string ngaytruongbophan, string ngayksvh, string ngayksbbdsc)
        {
            OracleConnection con = new OracleConnection(vConnection);
            OracleParameter paramWO = new OracleParameter("p_wo", OracleDbType.Int32);
            paramWO.Value = int.Parse(wocode);
            OracleParameter p_ttthietbi = new OracleParameter("p_ttthietbi", OracleDbType.NVarchar2);
            var TTThietBi = tinhtrangtbi;
            p_ttthietbi.Value = TTThietBi;
            OracleParameter p_lanhdaogian = new OracleParameter("p_lanhdaogian", OracleDbType.NVarchar2);
            var lanhdaogian = cbLanhDaoGian;
            p_lanhdaogian.Value = lanhdaogian;
            OracleParameter p_ksbbdcs = new OracleParameter("p_ksbbdcs", OracleDbType.NVarchar2);
            var ksbbdcs = cbKSBBDSC;
            p_ksbbdcs.Value = ksbbdcs;
            OracleParameter p_truongbophan = new OracleParameter("p_truongbophan", OracleDbType.NVarchar2);
            var truongbophan = cbTruongBoPhan;
            p_truongbophan.Value = truongbophan;
            OracleParameter p_ksvanhanh = new OracleParameter("p_ksvanhanh", OracleDbType.NVarchar2);
            var ksvanhanh = cbKSVanHanh;
            p_ksvanhanh.Value = ksvanhanh;

            OracleParameter p_nguoichophep = new OracleParameter("p_nguoichophep", OracleDbType.NVarchar2);
            var vnguoichoiphep = cbnguoichophep;
            p_nguoichophep.Value = vnguoichoiphep;

            OracleParameter p_ngaylanhdao = new OracleParameter("p_ngaylanhdao", OracleDbType.NVarchar2);
            var vngaylanhdao = ngaylanhdao;
            p_ngaylanhdao.Value = vngaylanhdao;

            OracleParameter p_ngaytruongbophan = new OracleParameter("p_ngaytruongbophan", OracleDbType.NVarchar2);
            var vngaytruongbophan = ngaytruongbophan;
            p_ngaytruongbophan.Value = vngaytruongbophan;

            OracleParameter p_ngayksvh = new OracleParameter("p_ngayksvh", OracleDbType.NVarchar2);
            var vngayksvh = ngayksvh;
            p_ngayksvh.Value = vngayksvh;

            OracleParameter p_ngayksbbdsc = new OracleParameter("p_ngayksbbdsc", OracleDbType.NVarchar2);
            var vngayksbbdsc = ngayksbbdsc;
            p_ngayksbbdsc.Value = vngayksbbdsc;

            return this.ExcuteQuery(con, "update_wo_ELE_r5", new[] { paramWO, p_ttthietbi, p_lanhdaogian, p_ksbbdcs, p_truongbophan, p_ksvanhanh, p_nguoichophep, p_ngaylanhdao, p_ngaytruongbophan, p_ngayksvh, p_ngayksbbdsc });
        }
        public ActionResult GetPersonList()
        {
            if(HttpContext.Session.GetString("ListPerson")  != null){
                List<Person> persons = JsonConvert.DeserializeObject<List<Person>>(HttpContext.Session.GetString("ListPerson"));
                return Json(persons);
            }
            return Json(new List<Person>());
            
        }
        public string ExcuteQuery(OracleConnection conn, string SPName, OracleParameter[] OraclePrms = null)
        {
            var mess = string.Empty;
            using (conn)
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                using (OracleCommand command = new OracleCommand(SPName, conn))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    try
                    {
                        command.Parameters.AddRange(OraclePrms);
                        command.ExecuteNonQuery();
                        conn.Close();
                        command.Parameters.Clear();
                    }
                    catch (Exception e)
                    {
                        mess = e.Message;
                    }
                }
            }
            return mess;
        }
        public JsonResult GetTableData([DataSourceRequest] DataSourceRequest request)
        {
            if (HttpContext.Session.GetString("WOListACT") != null)
            {
                return Json(JsonConvert.DeserializeObject<List<WOACTDtoORA>>(HttpContext.Session.GetString("WOListACT")).Where(w => w.status != 3).ToDataSourceResult(request));
            }
            return Json(new { });

        }
        public JsonResult GetTablePart([DataSourceRequest] DataSourceRequest request)
        {
            if (HttpContext.Session.GetString("tablePart") != null)
            {

                return Json(JsonConvert.DeserializeObject<List<WOPartDtoORA>>(HttpContext.Session.GetString("tablePart")).Where(w => w.status != 3).ToDataSourceResult(request));
            }
            return Json(new { });

        }
        public ActionResult SaveChangePersonTable([FromBody] List<PersonTable> personTables)
        {
            foreach(var item in personTables)
            {
                item.Ngay = item.Ngay != (DateTime?)null ? item.Ngay.Value.AddHours(7): item.Ngay;
            }
            HttpContext.Session.SetString("PersonTable", JsonConvert.SerializeObject(personTables));
            return Ok();
        }
        public List<Tool> loadDataTool()
        {
            DataTable dt = new DataTable();
            DataSet ds;
            
            OracleConnection con = new OracleConnection(vConnection);
            ds = this.GetDataSet(con, "get_TOOLS", null);
            if (ds != null)
            {
                List<Tool> tools = new List<Tool>();
              
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    tools.Add(new Tool()
                    {
                        Ma_Tool = row.IsNull("MATOOL") ? null : row["MATOOL"].ToString(),
                        Ten_TB = row.IsNull("TENTBI") ? null : row["TENTBI"].ToString(),
                        So_Seri = row.IsNull("SOSERI") ? null : row["SOSERI"].ToString(),
                        Ma_Ky_Hieu = row.IsNull("KIHIEU") ? null : row["KIHIEU"].ToString(),
                        Nha_SX = row.IsNull("NHASANXUAT") ? null : row["NHASANXUAT"].ToString(),
                        Hang_Kiem_Dinh = row.IsNull("HANGKIEMDINH") ? null : row["HANGKIEMDINH"].ToString(),
                    });
                }
                HttpContext.Session.SetString("LoadDataTool", JsonConvert.SerializeObject(tools));
                return tools;
            }
            HttpContext.Session.SetString("LoadDataTool", JsonConvert.SerializeObject(new List<Tool>()));
            return new List<Tool>();
            
        }
        public IActionResult getToolSelect()
        {
            if (HttpContext.Session.GetString("toolSelect") != null)
            {
                var tool = JsonConvert.DeserializeObject<Tool>(HttpContext.Session.GetString("toolSelect"));
                return Json(new { Ma_Tool = tool.Ma_Tool, Ma_Ky_Hieu = tool.Ma_Ky_Hieu, So_Seri = tool.So_Seri, Ten_TB = tool.Ten_TB, Nha_SX = tool.Nha_SX, Hang_Kiem_Dinh = tool.Hang_Kiem_Dinh });
            }
            return Json(new { Ma_Tool = "", Ma_Ky_Hieu = "", So_Seri = "", Ten_TB = "", Nha_SX = "", Hang_Kiem_Dinh = "" });
        }
        public ActionResult SelectTool(string Ten_TB)
        {

            var Ma_Ky_Hieu = "";
            var Nha_SX = "";
            var Ma_Tool = "";
            var Hang_Kiem_Dinh = "";
            var So_Seri = "";
            if (HttpContext.Session.GetString("LoadDataTool") != null)
            {
                Tool tool = (JsonConvert.DeserializeObject<List<Tool>>(HttpContext.Session.GetString("LoadDataTool")).FirstOrDefault(t => t.Ten_TB == Ten_TB));
                if (tool != null)
                {

                    Ma_Tool = tool.Ma_Tool;
                    Nha_SX = tool.Nha_SX;
                    Ma_Ky_Hieu = tool.Ma_Ky_Hieu;
                    Hang_Kiem_Dinh = tool.Hang_Kiem_Dinh;
                    So_Seri = tool.So_Seri;
                }
            }
            return Json(new { Ma_Tool = Ma_Tool, Nha_SX = Nha_SX, Ma_Ky_Hieu = Ma_Ky_Hieu, Hang_Kiem_Dinh  = Hang_Kiem_Dinh, So_Seri = So_Seri });
        }
        public ActionResult getStoreList(string par_code)
        {
            if (HttpContext.Session.GetString("partAndStore") != null)
            {
                List<string> storecodes = JsonConvert.DeserializeObject<List<TablePart>>(HttpContext.Session.GetString("partAndStore")).ToList().Where(t => t.par_code == par_code).Select(t => t.sto_store).ToList();
                return Json(storecodes);
            }
            return Json(new List<string>());

        }
        
    }

}