using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FilesUploadController : ControllerBase
    {
        readonly IConfiguration _configuration;
        ILogger _logger;

        string LastError = string.Empty;
        string _webPortal_Host_EndPoint = "";
        string _fileSavePath = "";

        public FilesUploadController(IConfiguration configuration,
            ILogger<FilesUploadController> logger)
        {
            _configuration = configuration;
            _fileSavePath = _configuration.GetSection("FileSavedPath").Value;
            _webPortal_Host_EndPoint = _configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
            _logger = logger;
        }

        // app api received
        // and post to portal api

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Content("file not selected");
                }

                // read the header from request                                         
                HttpContext.Request.Headers.TryGetValue("moduleType", out StringValues moduleType);
                HttpContext.Request.Headers.TryGetValue("token", out StringValues token);

                if (!Directory.Exists(_fileSavePath))
                {
                    Directory.CreateDirectory(_fileSavePath);
                }

                // copy to my folder 
                var path = Path.Combine(_fileSavePath, file.FileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // massage the file size and save again
                //FileReSize(path);

                // posting to web portal to get saved file name 
                var client = new RestClient($"{_webPortal_Host_EndPoint}UploadFile/{moduleType}");
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {token}");
                request.AddFile("File", path);
                IRestResponse response = client.Execute(request);

                var result = JsonConvert.DeserializeObject<AttachmentResult>(response.Content);
                if (result != null)
                {
                    return Ok(result);
                }

                return BadRequest("Upload file fail");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        // for later used         
        // comment the file resize 
        //void FileReSize(string path)
        //{
        //    try
        //    {
        //        // 20230228 modified, test and deployed
        //        var fileInfo = new FileInfo(path);
        //        if (fileInfo == null) return;
        //        if (!fileInfo.Exists) return;

        //        List<string> imageExtensions = new List<string> { ".JPG", ".JPEG", ".JPE", ".BMP", ".GIF", ".PNG" };
        //        var fileExt = $"{fileInfo.Extension}".ToUpper();

        //        var found = imageExtensions.FirstOrDefault(x => x.Contains(fileExt));
        //        if (found != null)
        //        {                    
        //            // resize the file 
        //            // massage the file size and save again
        //            using var image = Image.Load(path);

        //            if (image.Width > 550 || image.Height > 700)
        //            {   
        //                image.Mutate(x => x.Resize(new ResizeOptions
        //                {
        //                    Mode = ResizeMode.Max,
        //                    Size = new Size(550, 700)
        //                }));
        //            }
        //            image.Save(path);
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        LastError = $"{e.Message}\n{e.StackTrace}";
        //        _logger.LogError(LastError);
        //    }
        //}

    }
}
