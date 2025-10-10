using Dapper;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Helpers.DiApi;
using KTC_SalesAppWAPI.Models.Bread;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BreadInvFileUploadController : ControllerBase
    {
        readonly IConfiguration _configuration;
        ILogger _logger;

        string LastError = string.Empty;
        string _webPortal_Host_EndPoint = "";
        string _fileSavePath = "";
        string _webAccessPath = "";
        string _commDbConnStr_Bread = "";

        public BreadInvFileUploadController(IConfiguration configuration,
            ILogger<BreadInvFileUploadController> logger)
        {
            _configuration = configuration;
            _fileSavePath = _configuration.GetSection("WebAttachmentPath").Value;
            _webAccessPath = _configuration.GetSection("WebAccessPath").Value;
            _webPortal_Host_EndPoint = _configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
            _commDbConnStr_Bread = _configuration.GetConnectionString("MasterConn_Bread");
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                HttpContext.Request.Headers.TryGetValue("guid", out StringValues guid);
                HttpContext.Request.Headers.TryGetValue("userCode", out StringValues userCode);
                HttpContext.Request.Headers.TryGetValue("subsi", out StringValues subsi);
                HttpContext.Request.Headers.TryGetValue("docEntry", out StringValues docEntry);

                if (file == null || file.Length == 0)
                {
                    return Content("file not selected");
                }

                if (!Directory.Exists(_fileSavePath))
                {
                    Directory.CreateDirectory(_fileSavePath);
                }

                var path = Path.Combine(_fileSavePath, file.FileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                if (System.IO.File.Exists(path))
                {
                    var fileInf = new FileInfo(path);
                    var newFileName = $"{DateTime.Now.Ticks}{fileInf.Extension}";
                    var newFilePath = Path.Combine(_fileSavePath, newFileName);
                    var webAccess = Path.Combine(_webAccessPath, newFileName);

                    System.IO.File.Delete(newFilePath); // Delete the existing file if exists
                    System.IO.File.Move(path, newFilePath); // Rename the oldFileName into newFileName

                    UpdateFilePath(subsi, userCode, guid, newFilePath);

                    var err = UpdateSAP_InvDocAttch(subsi, int.Parse(docEntry), newFilePath);
                    if (string.IsNullOrWhiteSpace(err))
                    {
                        return Ok(webAccess);
                    }

                    return BadRequest(err);
                }

                return BadRequest("File saved wirh error");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        string UpdateSAP_InvDocAttch(string subsi, int docEntry, string filePath)
        {
            try
            {
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, subsi);
                if (db == null)
                {
                    LastError = "invalid db infor";
                    _logger.LogError(LastError);
                    return LastError;
                }

                var helper = new BreadDiApi_Delivery(db, _commDbConnStr_Bread, "OINV", SAPbobsCOM.BoObjectTypes.oInvoices);

                var result = helper.Add_FileAttachment(docEntry, filePath, SAPbobsCOM.BoObjectTypes.oInvoices);
                if (string.IsNullOrWhiteSpace(result))
                {
                    return "";
                }

                return result;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return LastError;
            }
        }

        void UpdateFilePath(string subsi, string userCode, string guid, string svrPhyPath)
        {
            try
            {
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, subsi);
                if (db == null)
                {
                    LastError = "invalid db infor";
                    _logger.LogError(LastError);
                    return;
                }

                var exFile_query = $@"select * 
                                    from {db.WEBDB}..FTAPP_MWFileUpload
                                    where HeaderGuid = @guid";

                var conn = new SqlConnection(_commDbConnStr_Bread);
                var _file = conn.Query<FTAPP_MWFileUpload>(exFile_query, new { guid = guid }).FirstOrDefault();

                if (_file == null)
                {
                    var insertsql = @$"Insert into  {db.WEBDB}..FTAPP_MWFileUpload ( 
                                        HeaderGuid
                                        ,UploadDatetime
                                        ,AppUser
                                        ,ServerSavedPath
                                    ) values ( 
                                         @HeaderGuid
                                         ,GETDATE()
                                         ,@AppUser 
                                         ,@ServerSavedPath   )";

                    _file = new FTAPP_MWFileUpload
                    {
                        HeaderGuid = Guid.Parse(guid),
                        AppUser = userCode,
                        ServerSavedPath = svrPhyPath
                    };

                    conn.Execute(insertsql, _file);
                    return;
                }

                // update the file 
                _file.ServerSavedPath = svrPhyPath;
                var updatesql = $@"update  {db.WEBDB}..FTAPP_MWFileUpload
                                   set ServerSavedPath = @ServerSavedPath , 
                                       UploadDatetime = GETDATE()
                                   where HeaderGuid = @HeaderGuid";

                conn.Execute(updatesql, new
                {
                    ServerSavedPath = svrPhyPath,
                    HeaderGuid = _file.HeaderGuid
                });

            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);

            }
        }

    }
}
