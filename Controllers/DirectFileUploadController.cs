using Dapper;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.Bread;
using KTC_SalesAppWAPI.Models.BreadReturn;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DirectFileUploadController : ControllerBase
    {
        readonly IConfiguration _configuration;
        ILogger _logger;

        string LastError = string.Empty;
        string _webPortal_Host_EndPoint = "";
        string _fileSavePath = "";
        string _webAccessPath = "";
        string _commDbConnStr_Bread;

        public DirectFileUploadController(IConfiguration configuration,
            ILogger<DirectFileUploadController> logger)
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
                if (file == null || file.Length == 0)
                {
                    return Content("file not selected");
                }

                HttpContext.Request.Headers.TryGetValue("guid", out StringValues guid);
                HttpContext.Request.Headers.TryGetValue("subsi", out StringValues subsi);
                HttpContext.Request.Headers.TryGetValue("usercode", out StringValues userCode);

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

                    // 20230217
                    // resize the file 
                    // massage the file size and save again
                    FileReSize(path);

                    var fileInf = new FileInfo(path);
                    var newFileName = $"{DateTime.Now.Ticks}{fileInf.Extension}";
                    var newFilePath = Path.Combine(_fileSavePath, newFileName);
                    var webAccess = Path.Combine(_webAccessPath, newFileName);

                    System.IO.File.Delete(newFilePath); // Delete the existing file if exists
                    System.IO.File.Move(path, newFilePath); // Rename the oldFileName into newFileName

                    //System.IO.File.Delete(path);
                    return Ok(webAccess);
                }

                return BadRequest("File saved with error");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        [HttpPost]
        [Route("w")]
        public async Task<IActionResult> UploadFileW(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Content("file not selected");
                }

                HttpContext.Request.Headers.TryGetValue("guid", out StringValues guid);
                HttpContext.Request.Headers.TryGetValue("subsi", out StringValues subsi);
                HttpContext.Request.Headers.TryGetValue("usercode", out StringValues userCode);

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
                    // 20230217
                    // resize the file 
                    // massage the file size and save again
                    FileReSize(path);

                    var fileInf = new FileInfo(path);
                    var newFileName = $"{DateTime.Now.Ticks}{fileInf.Extension}";
                    var newFilePath = Path.Combine(_fileSavePath, newFileName);

                    var webAccess = Path.Combine(_webAccessPath, newFileName);

                    System.IO.File.Delete(newFilePath); // Delete the existing file if exists
                    System.IO.File.Move(path, newFilePath); // Rename the oldFileName into newFileName

                    //System.IO.File.Delete(path);
                    return Ok(webAccess);
                }

                return BadRequest("File saved with error");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        [HttpPost]
        [Route("FileName")]
        public async Task<IActionResult> UploadFile_RetFN(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Content("file not selected");
                }

                HttpContext.Request.Headers.TryGetValue("guid", out StringValues guid);
                HttpContext.Request.Headers.TryGetValue("subsi", out StringValues subsi);
                HttpContext.Request.Headers.TryGetValue("usercode", out StringValues userCode);

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
                    // 20230217
                    // resize the file 
                    // massage the file size and save again
                    FileReSize(path);

                    var fileInf = new FileInfo(path);
                    var newFileName = $"{DateTime.Now.Ticks}{fileInf.Extension}";
                    var newFilePath = Path.Combine(_fileSavePath, newFileName);

                    var webAccess = Path.Combine(_webAccessPath, newFileName);

                    System.IO.File.Delete(newFilePath); // Delete the existing file if exists
                    System.IO.File.Move(path, newFilePath); // Rename the oldFileName into newFileName

                    // prepare to svae file 

                    var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, subsi);
                    if (db == null)
                    {
                        return BadRequest("invalid dbi");
                    }

                    // SAVE INTO FTAPP_MWFileUpload
                    var newfile = new FTAPP_MWFileUpload
                    {
                        HeaderGuid = Guid.Parse(guid),
                        UploadDatetime = DateTime.Now,
                        AppUser = (string)userCode,
                        ServerSavedPath = newFilePath
                    };

                    var sp_insert = $@"insert into {db.WEBDB}..FTAPP_MWFileUpload 
                                        (HeaderGuid, UploadDatetime, AppUser, ServerSavedPath) 
                                        values 
                                        (@HeaderGuid, GETDATE(), @AppUser, @ServerSavedPath)";

                    var res = new SqlConnection(_commDbConnStr_Bread).Execute(sp_insert, newfile);

                    return Ok(newFileName); // only return the file name
                }

                return BadRequest("File saved with error");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        [HttpPost]
        [Route("f")]
        public async Task<IActionResult> UploadFileF(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Content("files not selected");
                }

                //HttpContext.Request.Headers.TryGetValue("guid", out StringValues guid);
                //HttpContext.Request.Headers.TryGetValue("subsi", out StringValues subsi);
                //HttpContext.Request.Headers.TryGetValue("usercode", out StringValues userCode);

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
                    // 20230217
                    // resize the file 
                    // massage the file size and save again
                    FileReSize(path);

                    var fileInf = new FileInfo(path);
                    var newFileName = $"{DateTime.Now.Ticks}{fileInf.Extension}";
                    var newFilePath = Path.Combine(_fileSavePath, newFileName);

                    var webAccess = Path.Combine(_webAccessPath, newFileName);

                    System.IO.File.Delete(newFilePath); // Delete the existing file if exists
                    System.IO.File.Move(path, newFilePath); // Rename the oldFileName into newFileName

                    return Ok(newFileName);
                }

                return BadRequest("File saved with error");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        /// <summary>
        ///  For single file upload by doc entry and doc type
        ///  create entry for file record, and later post file attachment to SAP doc entry
        ///  cater for the signature
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("fs")]
        public async Task<IActionResult> UploadFile_SF(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Content("file not selected");
                }

                HttpContext.Request.Headers.TryGetValue("subsi", out StringValues subsi);
                HttpContext.Request.Headers.TryGetValue("docEntry", out StringValues docEntry);
                HttpContext.Request.Headers.TryGetValue("docType", out StringValues docType);
                HttpContext.Request.Headers.TryGetValue("sapDoc", out StringValues sapDoc);
                HttpContext.Request.Headers.TryGetValue("module", out StringValues module);
                HttpContext.Request.Headers.TryGetValue("userCode", out StringValues userCode);

                // filepath 
                // url 
                // uploadDt

                if (!Directory.Exists(_fileSavePath))
                {
                    Directory.CreateDirectory(_fileSavePath);
                }

                var filepath = Path.Combine(_fileSavePath, file.FileName);
                using (var stream = new FileStream(filepath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                if (System.IO.File.Exists(filepath))
                {

                    FileReSize(filepath);

                    var fileInf = new FileInfo(filepath);
                    var newFileName = $"{DateTime.Now.Ticks}{fileInf.Extension}";
                    var newFilePath = Path.Combine(_fileSavePath, newFileName);
                    var webAccess = Path.Combine(_webAccessPath, newFileName);

                    System.IO.File.Delete(newFilePath); // Delete the existing file if exists
                    System.IO.File.Move(filepath, newFilePath); // Rename the oldFileName into newFileName

                    // prepare to insert the file info
                    var newFile = new FPROP
                    {
                        FilePath = newFilePath,
                        DocEntry = long.Parse(docEntry),
                        DocType = docType,
                        Url = webAccess,
                        UploadDt = DateTime.Now,
                        Module = module,
                        UserCode = userCode,
                        SapDoc = sapDoc,
                        UpdatedSap = null // leave for the attach web api to update this field
                    };

                    var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, subsi);
                    if (db == null)
                    {
                        return BadRequest("Invalid db info");
                    }

                    var sqlInsert = $@"Insert into {db.WEBDB}..FPROP (
                                  FilePath
                                , DocEntry
                                , DocType
                                , Url
                                , UploadDt 
                                , Module
                                , UserCode
                                , SapDoc 
                                , UpdatedSap 
                                ) values (
                                  @FilePath
                                , @DocEntry
                                , @DocType
                                , @Url
                                , GETDATE()
                                , @Module
                                , @UserCode
                                , @SapDoc 
                                , @UpdatedSap )";

                    var conn = new SqlConnection(_commDbConnStr_Bread);
                    var insertRes = conn.Execute(sqlInsert, newFile);
                    if (insertRes <= 0)
                    {
                        return BadRequest("Error insert file prop to database, please try again.");
                    }

                    return Ok(webAccess);
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

        void FileReSize(string path)
        {
            try
            {
                // 20230217
                // resize the file 
                // massage the file size and save again
                using var image = Image.Load(path);
                if (image.Width > 720)
                {
                    image.Mutate(x => x.Resize(720, 850));
                }
                image.Save(path);
            }
            catch (Exception e )
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

    }
}
