using Dapper;
using KTC_SalesAppWAPI.DTOs;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.Login;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SalesOrderController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        readonly IConfiguration _configuration;
        readonly string APP_JSON = "application/json";
        readonly ILogger<SalesOrderController> _logger;
        string WebHostAddrEndPoint = "";

        string LastError { get; set; } = string.Empty;

        string _commDbConnStr { get; set; } = string.Empty;

        public SalesOrderController(IConfiguration configuration, ILogger<SalesOrderController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(SO_Dto dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "WarehouseByUser":
                    {
                        return await WarehouseByUser(dto);
                    }
                case "Promotion":
                    {
                        return await Promotion(dto);
                    }
                case "ItemMaster":
                    {
                        return await ItemMaster(dto); // agency / items ion groups
                    }
                case "ItemGroup":
                    {
                        return await ItemGroup(dto); // agency / item group
                    }
                case "BusinessPartner":
                    {
                        return await BusinessPartner(dto); // c = Customer, s = agency
                    }
                case "GetCompanyID":
                    {
                        return GetCompanyID(dto);
                    }
                case "GetAgencyBrand":
                    {
                        return GetAgencyBrand(dto);
                    }
                case "SalesOrder":
                    {
                        return await SalesOrder(dto); // posting sales order
                    }
                case "SalesOrder_Lines":
                    {
                        return await SalesOrder_Lines(dto);
                    }
                case "SalesOrderUpdate":
                    {
                        return await SalesOrderUpdate(dto);
                    }

                case "LoadWhs":
                    {
                        return LoadWhs(dto);
                    }
                case "UpdateSOPoFiles":
                    {
                        return UpdateSOPoFiles(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult UpdateSOPoFiles(SO_Dto dto)
        {
            try
            {
                // perform update to the SO table for the signed SO as PO
                if (string.IsNullOrWhiteSpace(dto.SoDocSubSi))
                {
                    return BadRequest("Company name is empty");
                }
                if (dto.SoDocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.POFile))
                {
                    return BadRequest("Invalid files names");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SoDocSubSi);
                if (db == null)
                {
                    return BadRequest("Invalid company name, db info is empty");
                }

                var conn = new SqlConnection(_commDbConnStr);
                // get the existing file and and accumulate
                var query = @$"Select POFILE from {db.WEBDB}..SO with (nolock) Where docentry= @docentry";
                var existingFiles = conn.ExecuteScalar<string>(query, new { docentry = dto.SoDocEntry });
                if (string.IsNullOrWhiteSpace(existingFiles))
                {
                    existingFiles = dto.POFile;
                }
                else
                {
                    var files = existingFiles.Split(',').ToList();
                    var filtered = files.Where(f => !f.Contains("SOPO")).ToList(); // filter the old file out
                    filtered.Add(dto.POFile);
                    existingFiles = string.Join(",", filtered);                    
                }

                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();
                var sql = @$"UPDATE [{db.WEBDB}]..[SO] 
                                SET POFILE = @POFiles
                                WHERE DocEntry = @SoDocEntry";

                var result = conn.Execute(sql, new { POFiles = existingFiles, SoDocEntry = dto.SoDocEntry }, trans);
                if (result <= 0)
                {
                    trans.Rollback();
                    return BadRequest($"Error update file path for SO, subsi {db.COMPANYNAME}, " +
                        $"SO Entry : {dto.SoDocEntry}");
                }

                trans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult LoadWhs(SO_Dto dto)
        {
            try
            {
                // load sap available warehouse 
                if (string.IsNullOrWhiteSpace(dto.UserCompany))
                {
                    return BadRequest("Query company name is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.UserCompany);
                if (db == null)
                {
                    return BadRequest("Query company info is empty");
                }

                var sql = @$"SELECT * FROM [{db.SAPDB}].[dbo].[OWHS] WITH (NOLOCK) 
                             WHERE Locked = 'N'";

                using (var conn = new SqlConnection(_commDbConnStr))
                {
                    var list = conn.Query<OWHS_Ext>(sql).ToList();
                    if (list == null) return NotFound();
                    return Ok(list);
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        async Task<IActionResult> SalesOrderUpdate(SO_Dto dto)
        {
            try
            {
                if (dto.SoDocUpdate == null)
                {
                    return BadRequest("Update So Doc is null");
                }
                if (string.IsNullOrWhiteSpace(dto.SoDocCompanyID))
                {
                    return BadRequest("Update SO Doc company is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.SoDocUpdateType))
                {
                    return BadRequest("Update SO Doc type is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.SoDocUpdateTypeShort))
                {
                    return BadRequest("Update SO Doc type is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Update SO Doc query key is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.SoDocSubSi))
                {
                    return BadRequest("Invalid company subsi");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.SoDocCompanyID);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                
                // post to portal 
                using var httpclient = new HttpClient();
                var json = JsonConvert.SerializeObject(dto.SoDocUpdate);
                var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);

                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;
                var uri = new Uri($"{svrAdr}SalesOrder/{dto.SoDocCompanyID}/{dto.SoDocUpdateType}");

                httpclient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", dto.QueryKeys);

                var response = await httpclient.PostAsync(uri, stringContent);
                var isSuccessStatusCode = response.IsSuccessStatusCode;
                var lastStatusCode = response.StatusCode;

                if (isSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<SoDocResult>(content);
                    result.updateDocType = dto.SoDocUpdateType;
                    result.docType = dto.Request;

                    // add in the post success log 
                    if (dto.Line != null)
                    {
                        dto.Line.AppModule = "SO, " + dto.SoDocUpdateType;
                        dto.Line.PostResult = result.actionResult;
                        dto.Line.Details = "Success";
                        new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                    }

                    // 20211005
                    // query the sales order and it line and return to the app 

                    var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SoDocSubSi);
                    var conn = new SqlConnection(_commDbConnStr);
                    var sql_so = @$"Select * from {dbInfo.WEBDB}..SO With (nolock) Where docentry =@docentry";                    
                    var salesOrder = conn.Query<SO>(sql_so, new { docentry = result.actionResult }).FirstOrDefault();

                    if (salesOrder != null)
                    {
                        var sq_line = @$"Select * from {dbInfo.WEBDB}..SO1 with (nolock) Where docentry = @docentry ";
                        salesOrder.Lines = conn.Query<SO1>(sq_line, new { docentry = result.actionResult }).ToList();
                    }

                    result.SalesOrder = salesOrder;
                    return Ok(result);
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<PortalReplied>(content);
                    if (result == null)
                    {
                        return BadRequest("Error when posting to web portal");
                    }

                    if (dto.Line != null)
                    {
                        dto.Line.AppModule = "SO, " + dto.SoDocUpdateType;
                        dto.Line.PostResult = "Fail";
                        dto.Line.Details = result.errorMessage;
                        new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                    }

                    return BadRequest($"{result.errorMessage}\n{result.actionResult}");
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        async Task<IActionResult> SalesOrder_Lines(SO_Dto dto)
        {
            try
            {
                if (dto.SoDocEntry < 0)
                {
                    return BadRequest("Invalid SO doc entry");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Invalid SO doc query key");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryCompanyID))
                {
                    return BadRequest("Invalid SO Doc company id");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.QueryCompanyID);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }
                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;

                // get the sales order line from portal
                using (var httpclient = new HttpClient())
                {
                    var uri = new Uri($"{svrAdr}SalesOrder/{dto.QueryCompanyID}/{dto.SoDocEntry}");

                    httpclient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", dto.QueryKeys);

                    var response = await httpclient.GetAsync(uri);
                    var isSuccessStatusCode = response.IsSuccessStatusCode;
                    var lastStatusCode = response.StatusCode;

                    if (isSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var doc = JsonConvert.DeserializeObject<SoDocHeader>(content);
                        return Ok(doc);
                    }
                }

                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        /// <summary>
        /// Send in each company so line
        /// based on each line, create the header and send to portal 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        async Task<IActionResult> SalesOrder(SO_Dto dto) // intial test loreal
        {
            try
            {
                #region validation
                if (dto.SoDocLines == null)
                {
                    return BadRequest("SO Lines is empty");
                }

                if (dto.SoDocLines.Count == 0)
                {
                    return BadRequest("SO Lines is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.SoDocSubSi))
                {
                    return BadRequest("Company name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.SoDocWarehouse))
                {
                    return BadRequest("Warehouse name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.SoDocStoreCard))
                {
                    return BadRequest("Store name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.SoDocSlpName))
                {
                    return BadRequest("Sales person name is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("Sales Order - Query token key is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.SoDocUpdateType))
                {
                    return BadRequest("Sales Order - Doc post type is null or empty");
                }

                if (string.IsNullOrWhiteSpace(dto.SoDocUpdateTypeShort))
                {
                    return BadRequest("Sales Order - Doc post type short is null or empty");
                }

                if (string.IsNullOrWhiteSpace(dto.SoDocCompanyID))
                {
                    return BadRequest("Sales Order - Company ID is null or empty");
                }

                // 20220722
                // add in the PO Exp date 
                //if (dto.PoExpDate == default)
                //{
                //    return BadRequest("Sales Order - PO exp date is invalid");
                //}

                #endregion

                // get the db infor for the company
                var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.SoDocSubSi);
                if (dbInfo == null)
                {
                    return BadRequest("Company name no found in setup");
                }

                // get the warhouse object to use it code
                using (var conn = new SqlConnection(_commDbConnStr))
                {
                    var sql = @$"SELECT * FROM [{dbInfo.SAPDB}].[dbo].[OWHS] WITH (NOLOCK) 
                            WHERE WhsName = @SoDocWarehouse ";

                    var whs = conn.Query<OWHS_Ext>(sql, new { dto.SoDocWarehouse }).FirstOrDefault();
                    if (whs == null)
                    {
                        return BadRequest("Name warehouse not found");
                    }

                    // get SAP Cardcode 
                    sql = @$"SELECT * FROM [{dbInfo.SAPDB}].[dbo].[OCRD] WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard";

                    var card = conn.Query<OCRD_Ext>(sql, new { dto.SoDocStoreCard }).FirstOrDefault();
                    if (card == null)
                    {
                        return BadRequest("Store card not found");
                    }

                    // get billing address
                    sql = @$"SELECT * FROM [{dbInfo.SAPDB}].[dbo].[CRD1] WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='B'";

                    var bill_address = conn.Query<CRD1>(sql, new { dto.SoDocStoreCard }).FirstOrDefault();
                    if (bill_address == null)
                    {
                        return BadRequest("Store billing address not found");
                    }

                    // get shipment address
                    sql = @$"SELECT * FROM [{dbInfo.SAPDB}].[dbo].[CRD1] WITH (NOLOCK) 
                            WHERE CardCode =@SoDocStoreCard 
                            AND AdresType ='S'";

                    var ship_address = conn.Query<CRD1>(sql, new { dto.SoDocStoreCard }).FirstOrDefault();
                    if (ship_address == null)
                    {
                        return BadRequest("Store ship address not found");
                    }

                    // var doc total
                    var docTotal = dto.SoDocLines.Sum(l => l.linetotal);

                    // setup the doc header 
                    var newDoc = new SoDocHeader
                    {
                        docentry = 0,
                        docnum = 0,
                        docdate = $"{DateTime.Now:yyyy-MM-dd}T00:00:00",
                        docstatus = dto.SoDocUpdateTypeShort,
                        cardcode = card.CardCode,
                        cardname = card.CardName,
                        billto = bill_address.Address,
                        billtoadd = bill_address.GetAddress(),
                        shipto = ship_address.Address,
                        shiptoadd = ship_address.GetAddress(),
                        collector = dto.SoDocSlpName,
                        deldate = $"{dto.DeliveryDate:yyyy-MM-dd}T00:00:00",  //$"{DateTime.Now.AddDays(2).Date:yyyy-MM-dd}T00:00:00",
                        whscode = whs.WhsCode,
                        nodel = "N",
                        pono = dto.SoDocPurchaseOrder,
                        pofile = string.IsNullOrWhiteSpace(dto.SoDocFilesAttached) ? "" : dto.SoDocFilesAttached,
                        addhoc = "N", // always N
                        doctotal = docTotal,
                        invtotal = docTotal,
                        remarks = dto.SoDocRemarks,
                        postrem = null,
                        appr = null,
                        appruser = null,
                        apprdate = null,
                        apprlev = null,
                        apprrem = null,
                        initno = null,
                        slpcode = dto.SoDocSlpName,
                        inventry = null,
                        invno = null,
                        invamt = docTotal,
                        invamtfc = null,
                        ucreated = dto.SoDocSlpName,
                        dcreated = null,
                        umodified = dto.SoDocSlpName,
                        dmodified = null,
                        geocode = card.GlblLocNum,
                        holdrem = null,
                        apprlevel = null,
                        suppcode = null,
                        sampling = null,
                        currlevel = null,
                        refcard = null,
                        refno = null,
                        reftype = null,
                        odrtype = null,
                        odrdate = null,
                        seller = null,
                        location = null,
                        batchid = null,
                        onhold = null,
                        delrte = null,
                        docdisc = 0.000000,
                        addhocusr = null,
                        addhocamt = 0.000000,
                        confirmed = null,
                        confirmeddate = null,
                        confrmedby = null,
                        sapseller = null,
                        gs = null,
                        powerroot = null,
                        expdate = null,
                        spendate = null,                       
                    };

                    // 20220722, add in the po exp date
                    if (dto.PoExpDate != default)
                    {
                        newDoc.poexpdate = dto.PoExpDate;
                    }

                    newDoc.lines = dto.SoDocLines;


                    // 20220413
                    var svrAdr = !string.IsNullOrWhiteSpace(dbInfo.PostSvrAdressPort) ? dbInfo.PostSvrAdressPort : WebHostAddrEndPoint;
                    var uri = new Uri($"{svrAdr}{dto.Request}/{dto.SoDocCompanyID}/{dto.SoDocUpdateType}");

                    // 20231029
                    // switch picking to direct submit 
                    // follow old web api to create the invoice directly
                    // 20231028
                    bool isDirectInvoice = $"{whs.U_DIRINV}".ToLower().Equals("y");

                    if (isDirectInvoice == true)
                    {
                        dto.SoDocUpdateTypeShort = "S";
                        dto.SoDocUpdateType = "Submit";

                        newDoc.docstatus = "S";
                        uri = new Uri($"{svrAdr}{dto.Request}/{dto.SoDocCompanyID}/{dto.SoDocUpdateType}");

                        
                    }

                    // post to portal 
                    using var httpclient = new HttpClient();
                    var json = JsonConvert.SerializeObject(newDoc);
                    var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);

                    httpclient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", dto.QueryKeys);

                    var response = await httpclient.PostAsync(uri, stringContent);
                    var isSuccessStatusCode = response.IsSuccessStatusCode;
                    var lastStatusCode = response.StatusCode;

                    if (isSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<SoDocResult>(content);
                        result.updateDocType = dto.SoDocUpdateType;
                        result.docType = dto.Request;

                        // add in the post success log 
                        if (dto.Line != null)
                        {
                            dto.Line.AppModule = "SO, " + dto.SoDocUpdateType;
                            dto.Line.PostResult = result.actionResult;
                            dto.Line.Details = "Success";
                            new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                        }
                        else // try to create log
                        {
                            var newLog = new FTAPP_AppPostLog
                            {
                                AppModule = $"SO, {dto.SoDocUpdateType}",
                                UserCode = dto.SoDocSlpName,
                                CardCode = card.CardCode,
                                SubSi = dbInfo.COMPANYNAME,
                                Details = $"Success",
                                PostResult = result.actionResult,
                                AppVersion = "ServerUpdate"
                            };
                            new AppPostLogHelper().Create(_commDbConnStr, newLog);
                        }

                        // 20211005
                        // query the sales order and it line and return to the app 
                        var sql_so = @$"Select * from {dbInfo.WEBDB}..SO With (nolock) Where docentry =@docentry";
                        var salesOrder = conn.Query<SO>(sql_so, new { docentry = result.actionResult }).FirstOrDefault();

                        if (salesOrder != null)
                        {
                            var sq_line = @$"Select * from {dbInfo.WEBDB}..SO1 with (nolock) Where docentry = @docentry ";
                            salesOrder.Lines = conn.Query<SO1>(sq_line, new { docentry = result.actionResult }).ToList();
                        }

                        result.SalesOrder = salesOrder;
                        return Ok(result);
                    }
                    else
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<PortalReplied>(content);
                        if (result == null)
                        {
                            return BadRequest("Error when posting to web portal");
                        }

                        if (dto.Line != null)
                        {
                            dto.Line.AppModule = "SO, " + dto.SoDocUpdateType;
                            dto.Line.PostResult = "Fail";
                            dto.Line.Details = result.errorMessage;
                            new AppPostLogHelper().Create(_commDbConnStr, dto.Line);
                        }
                        else // try to create log
                        {
                            var newLog = new FTAPP_AppPostLog
                            {
                                AppModule = $"SO, {dto.SoDocUpdateType}",
                                UserCode = dto.SoDocSlpName,
                                CardCode = card.CardCode,
                                SubSi = dbInfo.COMPANYNAME,
                                Details = $"Fail, {result.errorMessage}",
                                PostResult = "",
                                AppVersion = "ServerUpdate"
                            };
                            new AppPostLogHelper().Create(_commDbConnStr, newLog);
                        }

                        return BadRequest($"{result.errorMessage}\n{result.actionResult}");
                    }
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        async Task<IActionResult> BusinessPartner(SO_Dto dto)
        {
            try
            {
                if (dto.QueryCardType == null)
                {
                    return BadRequest("Request card type is empty");
                }
                if (dto.QueryKeys == null)
                {
                    return BadRequest("Request keys is empty");
                }

                if (dto.QueryCompanyID == null)
                {
                    return BadRequest("Request company id is empty");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.QueryCompanyID);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

             

                using (var httpclient = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(dto.QueryItems);
                    var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);

                    // 20220413
                    var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;
                    var uri = new Uri($"{svrAdr}{dto.Request}/{dto.QueryCompanyID}/{dto.QueryCardType}");

                    httpclient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", dto.QueryKeys);

                    var response = await httpclient.PostAsync(uri, stringContent);
                    var isSuccessStatusCode = response.IsSuccessStatusCode;
                    var lastStatusCode = response.StatusCode;

                    if (isSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var items = JsonConvert.DeserializeObject<List<PortalOCRD>>(content);
                        return Ok(items);
                    }
                }

                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        async Task<IActionResult> ItemGroup(SO_Dto dto)
        {
            try
            {

                if (dto.QueryItems == null)
                {
                    return BadRequest("Request items is empty");
                }

                if (dto.QueryKeys == null)
                {
                    return BadRequest("Request keys is empty");
                }

                if (dto.QueryCompanyID == null)
                {
                    return BadRequest("Request company id is empty");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.QueryCompanyID);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using (var httpclient = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(dto.QueryItems);
                    var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);
                    // 20220413
                    var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;


                    var uri = new Uri($"{svrAdr}Master/{dto.QueryCompanyID}/{dto.Request}");

                    httpclient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", dto.QueryKeys);

                    var response = await httpclient.PostAsync(uri, stringContent);
                    var isSuccessStatusCode = response.IsSuccessStatusCode;
                    var lastStatusCode = response.StatusCode;

                    if (isSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var items = JsonConvert.DeserializeObject<List<PortalBrand>>(content);
                        return Ok(items);
                    }
                }

                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        async Task<IActionResult> WarehouseByUser(SO_Dto dto)
        {
            try
            {
                if (dto.QueryKeys == null)
                {
                    return BadRequest("Request keys is empty");
                }

                if (dto.QueryCompanyID == null)
                {
                    return BadRequest("Request company id is empty");
                }
                // 20220413
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.QueryCompanyID);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }

                using (var httpclient = new HttpClient())
                {
                    // 20220413
                    var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;
                    var uri = new Uri($"{svrAdr}Master/{dto.QueryCompanyID}/{dto.Request}");

                    httpclient.DefaultRequestHeaders.Authorization =
                          new AuthenticationHeaderValue("Bearer", dto.QueryKeys);

                    var response = await httpclient.GetAsync(uri);
                    var isSuccessStatusCode = response.IsSuccessStatusCode;
                    var lastStatusCode = response.StatusCode;

                    if (isSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var items = JsonConvert.DeserializeObject<List<OWHS_Ext>>(content);
                        return Ok(items);
                    }
                }

                return NotFound();
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        async Task<IActionResult> ItemMaster(SO_Dto dto)
        {
            try
            {
                if (dto.QueryItems == null)
                {
                    return BadRequest("Request item master is empty");

                }
                if (dto.QueryKeys == null)
                {
                    return BadRequest("Request keys is empty");
                }

                if (dto.QueryCompanyID == null)
                {
                    return BadRequest("Request company id is empty");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.QueryCompanyID);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }
                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;


                using (var httpclient = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(dto.QueryItems);
                    var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);

                    //http://10.0.0.12:82/Master/Z14/ItemMaster
                    var uri = new Uri($"{svrAdr}Master/{dto.QueryCompanyID}/{dto.Request}");

                    httpclient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", dto.QueryKeys);

                    var response = await httpclient.PostAsync(uri, stringContent);
                    var isSuccessStatusCode = response.IsSuccessStatusCode;
                    var lastStatusCode = response.StatusCode;

                    if (isSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var items = JsonConvert.DeserializeObject<List<PortalOITM>>(content);
                        return Ok(items);
                    }

                    return NotFound();
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        async Task<IActionResult> Promotion(SO_Dto dto)
        {
            try
            {
                if (dto.QueryItems == null)
                {
                    return BadRequest("Request item master is empty");
                }
                if (dto.QueryKeys == null)
                {
                    return BadRequest("Request keys is empty");
                }

                if (dto.QueryCompanyID == null)
                {
                    return BadRequest("Request company id is empty");
                }

                // 20220413
                var db = new DbNameHelper().GetDbInfoById(_commDbConnStr, dto.QueryCompanyID);
                if (db == null)
                {
                    return BadRequest("Invalid dbi");
                }
                // 20220413
                var svrAdr = !string.IsNullOrWhiteSpace(db.PostSvrAdressPort) ? db.PostSvrAdressPort : WebHostAddrEndPoint;


                using (var httpclient = new HttpClient())
                {
                    var json = JsonConvert.SerializeObject(dto.QueryItems);
                    var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);

                    //http://10.0.0.12:82/Master/Z14/Promotion                    
                    var uri = new Uri($"{svrAdr}Master/{dto.QueryCompanyID}/{dto.Request}");

                    httpclient.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", dto.QueryKeys);

                    var response = await httpclient.PostAsync(uri, stringContent);
                    var isSuccessStatusCode = response.IsSuccessStatusCode;
                    var lastStatusCode = response.StatusCode;

                    if (isSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();

                        // speacil method to parse json with empy array
                        //https://stackoverflow.com/questions/53219668/cannot-deserialize-the-current-json-object-empty-array
                        var settings = new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore,
                            Converters = { new JsonSingleOrEmptyArrayConverter<PortalPromoItem>() },
                        };

                        // 20230223
                        if (string.IsNullOrWhiteSpace(dto.IsNewPromoteList)) // follow the v 217
                        {
                            var item = JsonConvert.DeserializeObject<PortalPromoItem>(content, settings);
                            if (item == null) return NotFound();
                            return Ok(item);
                        }

                        // v 218
                        // support multiple promotion
                        var items = JsonConvert.DeserializeObject<List<PortalPromoItem>>(content, settings);
                        if (items.Count == 0) return NotFound();
                        return Ok(items);
                    }
                    return NotFound();
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        /// <summary>
        /// return the company id 
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        IActionResult GetCompanyID(SO_Dto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.UserCompany))
                {
                    return BadRequest("Request company is empty");
                }

                var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.UserCompany);
                if (dbInfo == null)
                {
                    return BadRequest($"Compnay {dto.UserCompany} not found");
                }

                return Ok(dbInfo.COMPANYID);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetAgencyBrand(SO_Dto dto)
        {
            // load all brand before load item 
            try
            {
                if (string.IsNullOrWhiteSpace(dto.UserCompany))
                {
                    return BadRequest("Request company is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.AgencyCardCode))
                {
                    return BadRequest("Request company is empty");
                }

                if (string.IsNullOrWhiteSpace(dto.WarehouseCode))
                {
                    return BadRequest("Request warehouse code is empty");
                }

                var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.UserCompany);
                if (dbInfo == null)
                {
                    return BadRequest($"Company {dto.UserCompany} not found");
                }

                dto.UserCompany = dto.UserCompany.Replace("'", "''");
                dto.AgencyName = dto.AgencyName.Replace("'", "''");
                dto.AgencyCardCode = dto.AgencyCardCode.Replace("'", "''");
                dto.WarehouseCode = dto.WarehouseCode.Replace("'", "''");

                var sql = $@"SELECT 
                            Distinct 
                              '{dto.UserCompany}' [UserCompany]
                            , '{dto.AgencyName}' [AgencyName]
                            , t0.CardCode [AgencyCard]     
                            , t1.ItmsGrpCod [ItmGrpNum]
                            , t1.ItmsGrpNam [BrandName]              
                           FROM [{dbInfo.SAPDB}].[dbo].[OITM] t0 WITH (NOLOCK) 
                                INNER JOIN 
                                [{dbInfo.SAPDB}].[dbo].[OITB] t1 WITH (NOLOCK) ON t0.ItmsGrpCod = t1.ItmsGrpCod
                                INNER JOIN 
                                [{dbInfo.SAPDB}].[dbo].[OITW] t2 WITH (NOLOCK) ON t0.ItemCode = t2.ItemCode
                          WHERE t0.CardCode = @AgencyCardCode 
                                AND t2.WhsCode = @WarehouseCode
                                AND t2.OnHand > 0";

                using var conn = new SqlConnection(_commDbConnStr);
                var brands = conn.Query<Brand>(sql, new { dto.AgencyCardCode, dto.WarehouseCode }).ToList();
                return Ok(brands);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

    }
}

// reserved code
//async Task<IActionResult> SalesOrder(SO_Dto dto)
//{
//    try
//    {
//        // req.Request = "SalesOrder" 
//        // req.SoDoc = {}
//        // req.QueryKeys  - token
//        // req.SoDocUpdateType 
//        // req.SoDocCompanyID

//        #region validation

//        if (dto.SoDoc == null)
//        {
//            return BadRequest("Sales Order - Doc object is empty");
//        }

//        if (dto.SoDoc.lines == null)
//        {
//            return BadRequest("Sales Order - Doc lines object is empty");

//        }

//        if (dto.SoDoc.lines.Count == 0)
//        {
//            return BadRequest("Sales Order - Doc lines object is empty");
//        }

//        if (string.IsNullOrWhiteSpace(dto.QueryKeys))
//        {
//            return BadRequest("Sales Order - Query token key is empty");
//        }

//        if (string.IsNullOrWhiteSpace(dto.SoDocUpdateType))
//        {
//            return BadRequest("Sales Order - Doc post type is null or empty");
//        }

//        if (string.IsNullOrWhiteSpace(dto.SoDocCompanyID))
//        {
//            return BadRequest("Sales Order - Company ID is null or empty");
//        }
//        #endregion

//        using (var httpclient = new HttpClient())
//        {
//            var json = JsonConvert.SerializeObject(dto.SoDoc);
//            var stringContent = new StringContent(json, Encoding.UTF8, APP_JSON);
//            var uri = new Uri($"{WebHostAddrEndPoint}{dto.Request}/{dto.SoDocCompanyID}/{dto.SoDocUpdateType}");

//            httpclient.DefaultRequestHeaders.Authorization =
//                    new AuthenticationHeaderValue("Bearer", dto.QueryKeys);

//            var response = await httpclient.PostAsync(uri, stringContent);
//            var isSuccessStatusCode = response.IsSuccessStatusCode;
//            var lastStatusCode = response.StatusCode;

//            if (isSuccessStatusCode)
//            {
//                //var content = await response.Content.ReadAsStringAsync();                        
//                return Ok();
//            }
//        }

//        return BadRequest("Sales Order - request no handler, please try again.");
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return BadRequest($"request not handler.\n{LastError}");
//    }
//}

//IActionResult GetAgencyBrandItem(SO_Dto dto)
//{
//    try
//    {
//        if (string.IsNullOrWhiteSpace(dto.UserCompany))
//        {
//            return BadRequest("Request company is empty");
//        }

//        if (string.IsNullOrWhiteSpace(dto.AgencyCardCode))
//        {
//            return BadRequest("Request agency name is empty");
//        }

//        if (string.IsNullOrWhiteSpace(dto.AgencyBrandCode))
//        {
//            return BadRequest("Request agency brand name is empty");
//        }

//        if (string.IsNullOrWhiteSpace(dto.WarehouseCode))
//        {
//            return BadRequest("Request warehouse code is empty");
//        }

//        var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.UserCompany);
//        if (dbInfo == null)
//        {
//            return BadRequest($"Company {dto.UserCompany} not found");
//        }

//        var sql = $@"SELECT * 
//                  , t1.OnHand [OITW_OnHand]
//                  , t1.IsCommited [OITW_IsCommited]
//                  , t1.OnOrder [OITW_OnOrder]
//                  , t1.OnHand + t1.OnOrder - t1.IsCommited [OITW_Available]                        
//                   FROM [{dbInfo.SAPDB}].[dbo].[OITM] t0 
//                        INNER JOIN 
//                        [{dbInfo.SAPDB}].[dbo].[OITW] t1 ON t0.ItemCode = t1.ItemCode
//                  WHERE t0.CardCode = @AgencyCardCode 
//                  AND t0.ItmsGrpCod = @AgencyBrandCode
//                  AND t1.WhsCode = @WarehouseCode
//                  AND t1.OnHand > 0 ";

//        dto.AgencyCardCode = dto.AgencyCardCode.Replace("'", "''");
//        dto.AgencyBrandCode = dto.AgencyBrandCode.Replace("'", "''");
//        dto.WarehouseCode = dto.WarehouseCode.Replace("'", "''");

//        using var conn = new SqlConnection(_commDbConnStr);
//        var items = conn.Query<OITM_Ext>(sql, new { dto.AgencyCardCode, dto.AgencyBrandCode, dto.WarehouseCode }).ToList();
//        if (items == null) return NotFound();
//        if (items.Count == 0) return NotFound();

//        // sub query each unit price

//        items.ForEach( x=> 
//        {
//            var sql = @$"Select t1.Price,  t1.PriceList
//                          FROM [{ dbInfo.SAPDB}].[dbo].[OCRD] t0
//                               INNER JOIN [{ dbInfo.SAPDB}].[dbo].[ITM1] t1 on t0.ListNum = t1.PriceList
//                          WHERE t0.CardCode = @CardCode 
//                          AND t1.ItemCode = @ItemCode";

//            var tempPrice = conn.Query<TempPrice>(sql, new { x.CardCode, x.ItemCode}).FirstOrDefault();
//            if (tempPrice != null)
//            {
//                x.Price = tempPrice.Price;
//                x.ListNum = tempPrice.ListNum;
//            }
//        });


//        return Ok(items);
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return BadRequest($"request not handler.\n{LastError}");
//    }
//}

//IActionResult GetWarehouses(SO_Dto dto)
//{
//    try
//    {
//        if (string.IsNullOrWhiteSpace(dto.UserCompany))
//        {
//            return BadRequest("Request company is empty");
//        }

//        dto.UserCompany = dto.UserCompany.Replace("'", "''");
//        var dbInfo = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.UserCompany);
//        if (dbInfo == null)
//        {
//            return BadRequest($"Compnay {dto.UserCompany} not found");
//        }

//        var sql = @$"SELECT * 
//                FROM [{dbInfo.SAPDB}].[dbo].[OWHS] 
//                  WHERE Locked = 'N' AND Inactive = 'N'";

//        using var conn = new SqlConnection(_commDbConnStr);
//        var whs = conn.Query<OWHS_Ext>(sql).ToList();
//        if (whs == null) return NotFound();
//        if (whs.Count == 0) return NotFound();

//        return Ok(whs);
//    }
//    catch (Exception e)
//    {
//        LastError = $"{e.Message}\n{e.StackTrace}";
//        _logger.LogError(LastError);
//        return BadRequest($"request not handler.\n{LastError}");
//    }
//}