using Dapper;
using KTC_SalesAppWAPI.DTOs.Pick_IBT;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.AppPostLog;
using KTC_SalesAppWAPI.Models.Batches;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.Pick_IBT;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PickIBTController : ControllerBase
    {
        readonly string _dbComm = "MasterConn";
        //readonly string APP_JSON = "application/json";
        readonly IConfiguration _configuration;
        readonly ILogger<PickIBTController> _logger;

        string WebHostAddrEndPoint = "";
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr { get; set; } = string.Empty;

        //static bool IsBusyNextOrder = false;
        //static bool IsBusyGetQueueSO3 = false;

        static bool IsPickHoldBusy { get; set; } = false; // 20230113
        public PickIBTController(IConfiguration configuration, ILogger<PickIBTController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString(_dbComm);
            WebHostAddrEndPoint = configuration.GetSection("AppSettings").GetSection("WebPortal_Host_EndPoint").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(Dto_Pick_IBT dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "IBT_FinalCheckOnHold":
                    {
                        return IBT_FinalCheckOnHold(dto);
                    }
                case "CheckIBTLineDraftExist":
                    {
                        return CheckIBTLineDraftExist(dto);
                    }
                case "GetQueueIBTLines":
                    {
                        return GetQueueIBTLines(dto);
                    }
                case "GetIBTSecBoxDraft":
                    {
                        return GetIBTSecBoxDraft(dto);
                    }
                case "GetSectionBatches":
                    {
                        return GetSectionBatches(dto);
                    }
                case "ClearIBTSecBoxLine":
                    {
                        return ClearIBTSecBoxLine(dto);
                    }
                case "AddIBTPickLog":
                    {
                        return AddIBTPickLog(dto);
                    }
                case "SaveIBTSecBoxDraft":
                    {
                        return SaveIBTSecBoxDraft(dto);
                    }
                case "HandlerSaveIBTLineAsDaft":
                    {
                        return HandlerSaveIBTLineAsDaft(dto);
                    }
                case "ClearIBTDraft":
                    {
                        return ClearIBTDraft(dto);
                    }
                case "ReleaseIBTInPicking":
                    {
                        return ReleaseIBTInPicking(dto);
                    }
                case "PostPicked_IBT":
                    {
                        return PostPicked_IBT(dto);
                    }
                default:
                    {
                        return BadRequest("Request no found");
                    }
            }
        }

        IActionResult PostPicked_IBT(Dto_Pick_IBT dto)
        {
            try
            {
                #region checking 
                if (string.IsNullOrWhiteSpace(dto.RequestName))
                {
                    return BadRequest("The request name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.CompanyId))
                {
                    return BadRequest("The company id is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.DocUpdateType))
                {
                    return BadRequest("The company update type is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.QueryKeys))
                {
                    return BadRequest("The query key is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.SaveAsDraft))
                {
                    return BadRequest("The save draft option is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The company name is empty");
                }

                // 20220325 remove the save draft from pick post 
                if (dto.SaveAsDraft.Equals("Y"))
                {
                    return HandlerSaveIBTLineAsDaft(dto);
                }

                // check SO current status from 
                var db0 = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db0 == null)
                {
                    return BadRequest("Not able to read db info, pls try again.");
                }
                #endregion end 

                var sql_SettingSaveTrace = "Select SetupValue from FTApp_Config Where SetupName  = 'SavePickBeforeAfter'";
                var trcaeSteupValue = new SqlConnection(_commDbConnStr).ExecuteScalar<string>(sql_SettingSaveTrace);

                // 20230129
                // add in save when received from app 
                if (!string.IsNullOrWhiteSpace(trcaeSteupValue) && trcaeSteupValue == "Y")
                {
                    SaveIBTLineFromReceived(db0, dto.PickedDoc.Lines);
                }

                // check before post to created invoice
                var ibtLines = new List<IBT1>();
                var ibtDoc = new KTC_SalesAppWAPI.Models.Pick_IBT.IBT();

                using (var connCurIbt = new SqlConnection(_commDbConnStr))
                {
                    var getCurIbt = $"SELECT * FROM {db0.WEBDB}..IBT  " +
                                $"Where DocEntry = @DocEntry";

                    ibtDoc = connCurIbt.Query<KTC_SalesAppWAPI.Models.Pick_IBT.IBT>(getCurIbt, new { dto.DocEntry }).FirstOrDefault();
                    if (ibtDoc == null)
                    {
                        return BadRequest($"Doc# {dto.DocEntry} no found, no able to perform post to intransist.\n" +
                            $"Please skip the order, and continue next.");
                    }

                    if (!$"{ibtDoc.DOCSTATUS}".Equals("Q"))
                    {
                        return BadRequest($"Doc# {dto.DocEntry} [Status] {ibtDoc.DOCSTATUS}, please process next.");
                    }

                    // query the lines 
                    var getCurIbtLines = $"SELECT * FROM {db0.WEBDB}..IBT1  " +
                                $"Where DocEntry = @DocEntry " +
                                $"and Qty1 > 0";

                    var list = connCurIbt.Query<IBT1>(getCurIbtLines, new { DocEntry = dto.DocEntry }).ToList();
                    if (list.Count == 0)
                    {
                        return BadRequest($"Doc# {dto.DocEntry} had no lines found for posting, no able to perform post to intransist.\n" +
                         $"Please skip the order, and continue next.");
                    }
                    ibtLines = new List<IBT1>(list); // ready for later use
                }

                // close the db sql connection
                // remove the box with no content 
                // filter out the box with no content 
                // v 45 / 46
                // 2021 07 24
                if (dto.Boxes == null)
                {
                    return BadRequest($"Doc# {dto.DocEntry} had no box found for posting, no able to perform post to intransist.\n" +
                      $"Please skip the order, and continue next.");
                }

                // filter the box
                var boxes = dto.Boxes.Where(b => b.Contents != null && b.Contents.Count > 0).ToList();

                // read the setting 
                // BypassBoxPickedDtMassage
                var sql_Setting = "Select SetupValue from FTApp_Config Where SetupName  = 'BypassBoxPickedDtMassage'";
                var isByPass = new SqlConnection(_commDbConnStr).ExecuteScalar<string>(sql_Setting);

                if (!string.IsNullOrWhiteSpace(isByPass) && isByPass == "N")
                {
                    //20210928 check the time of the box
                    for (int b = 0; b < boxes.Count; b++)
                    {
                        boxes[b].PickDt = BoxPickDtMassage(boxes[b]);
                    }
                }

                // 20220902
                // check the picked qty = 0
                // check the box contain the qty, if yes set the line pickedqty based on box 

                using var ibtLineConn = new SqlConnection(_commDbConnStr);
                var newIbtLines = new List<IBT1>();

                for (int x = 0; x < dto.PickedDoc.Lines.Count; x++)
                {
                    var line = dto.PickedDoc.Lines[x];
                    if (line == null) continue;

                    // only for picked qty = zero
                    for (int b = 0; b < boxes.Count; b++)
                    {
                        var box = boxes[b];
                        if (box == null) continue;
                        if (line.PICKEDQTY > 0) continue;
                        var foundContent = box.Contents.Where(x => x.ItemCode == line.ITEMCODE &&
                                                                   x.BaseLine == line.LINENUM).ToList();
                        if (foundContent.Count == 0) continue;

                        decimal sumOfQty = 0;
                        foundContent.ForEach(s =>
                        {
                            if (s.Packaging == "CS")
                            {
                                sumOfQty += line.UOMQTY * s.Qty;
                            }
                            else
                            {
                                sumOfQty += s.Qty;
                            }
                        });

                        if (sumOfQty > 0)
                        {
                            dto.PickedDoc.Lines[x].PICKEDQTY = sumOfQty;
                        }
                    }

                    // ready the ibt line 
                    var found_line = ibtLines.Where(i => i.ITEMCODE == line.ITEMCODE &&
                                                         i.LINENUM == line.LINENUM).FirstOrDefault();
                    if (found_line == null)
                    {
                        continue;
                    }

                    found_line.PICKEDQTY = line.PICKEDQTY;

                    if (line.FTAPP_Batches != null && line.FTAPP_Batches.Count > 0)
                    {
                        var batches = line.FTAPP_Batches;
                        for (int b = 0; b < batches.Count; b++)
                        {
                            var readBatches = batches[b];
                            if (readBatches == null) continue;
                            var newBatch = new IBT2
                            {
                                DOCENTRY = dto.DocEntry,
                                LINENUM = line.LINENUM,
                                LINENUM2 = b,
                                BATCHNO = readBatches.BatchNo,
                                QUANTITY = readBatches.PickedQty,
                                EXPDATE = readBatches.ExpDate,
                                MFRDATE = readBatches.MnfDate,
                                EXPIREDDATE = readBatches.ExpDate
                            };

                            if (found_line.Batches == null) found_line.Batches = new List<IBT2>();
                            // reset the App batch tp null
                            found_line.Batches.Add(newBatch);
                        }
                    }

                    newIbtLines.Add(found_line);
                }

                ibtDoc.Lines = new List<IBT1>(newIbtLines);
                dto.Boxes = boxes; // modified boxes 

                // save the box 
                var result = SaveBoxes(boxes, db0, dto.DocEntry); // save before post
                if (!result)
                {
                    return BadRequest("Error saving box and it content, pls try again [SSJ2023]");
                }

                var svrAdr = !string.IsNullOrWhiteSpace(db0.PostSvrAdressPort) ? db0.PostSvrAdressPort : WebHostAddrEndPoint;
                var client = new RestClient($"{svrAdr}InterbranchTransfer/{dto.CompanyId}/Picked");

                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", $"Bearer {dto.QueryKeys}");
                request.AddHeader("Content-Type", "application/json");
                var body = JsonConvert.SerializeObject(ibtDoc);

                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);

                if (response.IsSuccessful)
                {
                    using var conn = new SqlConnection(_commDbConnStr);
                    var query_ibt = @$"Select * from {db0.WEBDB}..IBT where docentry = @docentry";
                    var updatedIbt = conn.Query<KTC_SalesAppWAPI.Models.Pick_IBT.IBT>(query_ibt, new { docentry = dto.DocEntry }).FirstOrDefault();
                    if (updatedIbt == null)
                    {
                        return BadRequest("IBT posted, but error query updated IBT, please try again. ");
                    }

                    // delete the ibt onhold 
                    var query_deleteIBTONhold = $"Delete from {db0.WEBDB}..FTAPP_OnHold_IBT_InPicking " +
                                                $"Where HoldDocEntry = @docentry";


                    if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                    using var trans = conn.BeginTransaction();
                    try
                    {
                        conn.Execute(query_deleteIBTONhold, new { docentry = dto.DocEntry }, trans);
                    }
                    catch (Exception sqle)
                    {
                        trans.Rollback();
                        LastError = $"{sqle.Message}\n{sqle.StackTrace}";
                        _logger.LogError(LastError);
                    }

                    // prepare replied the ibt posted
                    var replied = new
                    {
                        ITDocNum = updatedIbt.TRANSITNO,
                        ReqDocNum = updatedIbt.TRANSITNO2,
                        Subsi = db0.COMPANYNAME
                    };

                    // log in the app 
                    var newLog = new FTAPP_AppPostLog
                    {
                        AppModule = "Picked to IBT",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.PickedDoc.CARDCODE}",
                        SubSi = $"{dto.Subsi}",
                        Details = $"#{dto.PickedDoc.DOCENTRY} success",
                        PostResult = $"Success",
                        AppVersion = $"ServerUpdate {dto.AppVersion}"
                    };

                    AppPostLogging(newLog);

                    return Ok(replied);
                }

                return BadRequest($"Post IBT fail, please try again. Thanks. {response.Content}");
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);

                var newLog = new FTAPP_AppPostLog
                {
                    AppModule = "Picked to IBT",
                    UserCode = $"{dto.UserCode}",
                    CardCode = $"{dto.PickedDoc.CARDCODE}",
                    SubSi = $"{dto.Subsi}",
                    Details = $"#{dto.PickedDoc.DOCENTRY}, Excep, " + LastError,
                    PostResult = $"1st Post Fail",
                    AppVersion = $"ServerUpdate {dto.AppVersion}"
                };
                AppPostLogging(newLog);

                var newReplied = new PortalReplied
                {
                    actionSuccess = false,
                    errorMessage = LastError,
                    actionResult = "Fail",
                    documentStatus = dto.PickedDoc.DOCSTATUS
                };

                return BadRequest(newReplied);
            }
        }

        bool SaveBoxes(List<FTAPP_Box> boxes, DbInfo db, int docEntry)
        {
            if (boxes == null) return true;
            if (boxes.Count == 0) return true;

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var checkDuplicate_sql = $"Select top 2 Boxid from  {db.WEBDB}..FTAPP_IBTBox with (nolock) " +
                                       " Where BaseEntry = @DocEntry ";

                var boxListExist = conn.Query<FTAPP_Box>(checkDuplicate_sql, new { DocEntry = docEntry }, trans).ToList();
                if (boxListExist.Count > 0) // if found box delete
                {
                    // remove the old save boxes 
                    var sql_removeOldBox = $"Delete from  {db.WEBDB}..FTAPP_IBTBox " +
                                       " Where BaseEntry = @DocEntry ";

                    conn.Execute(sql_removeOldBox, new { DocEntry = docEntry }, trans, commandTimeout: 0);
                }

                // check box 1
                checkDuplicate_sql = $"Select top 2 boxguid from  {db.WEBDB}..FTAPP_IBTBox1 with (nolock) " +
                                       " Where BaseEntry = @DocEntry ";

                var box1List = conn.Query<FTAPP_Box>(checkDuplicate_sql, new { DocEntry = docEntry }, trans).ToList();
                if (box1List.Count > 0)
                {
                    var sql_removeOldBox1 = $"Delete from {db.WEBDB}..FTAPP_IBTBox1 " +
                                       " Where BaseEntry= @DocEntry ";

                    conn.Execute(sql_removeOldBox1, new { DocEntry = docEntry }, trans, commandTimeout: 0);
                }

                // 20220509
                var insert_box = @$"INSERT INTO {db.WEBDB}..FTAPP_IBTBox (
                                          BoxId
                                        , PickerCode
                                        , PickerName
                                        , PickDt
                                        , BaseEntry 
                                        , BoxGuid
                                        , TimeStampSeq 
                                        , AppVersion
                                        , BoxSize, LabelConsistTotalBoxes, PickMode
                                        ) VALUES ( 
                                          @BoxId
                                        , @PickerCode
                                        , @PickerName
                                        , @PickDt
                                        , @BaseEntry 
                                        , @BoxGuid
                                        , @TimeStampSeq 
                                        , @AppVersion
                                        , @BoxSize, @LabelConsistTotalBoxes, @PickMode
                                        )";

                conn.Execute(insert_box, boxes.Distinct(), trans, commandTimeout: 0);

                // massage the box 
                var boxContents = new List<FTAPP_Box1>();
                boxes.ForEach(b =>
                {
                    if (b != null && b.Contents != null)
                    {
                        boxContents.AddRange(b.Contents);
                    }
                });


                if (boxContents.Count == 0)
                {
                    trans.Commit();
                    return true;
                }

                var insert_boxContent = @$"INSERT INTO {db.WEBDB}..FTAPP_IBTBox1 (
                                        ItemCode
                                        , ItemName
                                        , Qty
                                        , Packaging
                                        , BoxGuid
                                        , ContentGuid
                                        , BaseEntry
                                        , BaseLine
                                    ) VALUES (
                                        @ItemCode
                                        , @ItemName
                                        , @Qty
                                        , @Packaging
                                        , @BoxGuid
                                        , @ContentGuid
                                        , @BaseEntry
                                        , @BaseLine
                                    )";

                conn.Execute(insert_boxContent, boxContents.Distinct(), trans, commandTimeout: 0);
                trans.Commit();
                return true;
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return false;
            }
        }

        bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) return false;
            strInput = strInput.Trim();

            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    LastError = $"{jex.Message}\n{jex.StackTrace}";
                    _logger.LogError(LastError);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    LastError = $"{ex.Message}\n{ex.StackTrace}";
                    _logger.LogError(LastError);
                    return false;
                }
            }
            return false;
        }

        DateTime BoxPickDtMassage(FTAPP_Box box)
        {
            try
            {
                var dt = box.PickDt;
                if (dt == default) return DateTime.Now;

                const string _Am = "AM";
                const string _Pm = "PM";
                var hr = dt.Hour;

                var pickAmPm = dt.ToString("tt", CultureInfo.InvariantCulture);
                var svrAmPm = DateTime.Now.ToString("tt", CultureInfo.InvariantCulture);

                switch (hr)
                {
                    case 1:
                    case 13:
                        {
                            return new DateTime(dt.Year, dt.Month, dt.Day, 13, dt.Minute, dt.Second);
                            //if (hr == 1 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 13, dt.Minute, dt.Second);
                            //if (hr == 13 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 1, dt.Minute, dt.Second);
                            //return dt;
                        }
                    case 2:
                    case 14:
                        {
                            return new DateTime(dt.Year, dt.Month, dt.Day, 14, dt.Minute, dt.Second);
                            //if (hr == 2 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 14, dt.Minute, dt.Second);
                            //if (hr == 14 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 2, dt.Minute, dt.Second);
                            //return dt;
                        }
                    case 3:
                    case 15:
                        {
                            return new DateTime(dt.Year, dt.Month, dt.Day, 15, dt.Minute, dt.Second);
                            //if (hr == 3 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 15, dt.Minute, dt.Second);
                            //if (hr == 15 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 3, dt.Minute, dt.Second);
                            //return dt;
                        }
                    case 4:
                    case 16:
                        {
                            return new DateTime(dt.Year, dt.Month, dt.Day, 16, dt.Minute, dt.Second);
                            //if (hr == 4 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 16, dt.Minute, dt.Second);
                            //if (hr == 16 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 4, dt.Minute, dt.Second);
                            //return dt;
                        }
                    case 5:
                        {
                            return new DateTime(dt.Year, dt.Month, dt.Day, 17, dt.Minute, dt.Second);
                            //if (hr == 5 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 17, dt.Minute, dt.Second);
                            //if (hr == 17 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 5, dt.Minute, dt.Second);
                            //return dt;
                        }
                    case 6:
                    case 18:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 18, dt.Minute, dt.Second);
                            if (hr == 6 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 18, dt.Minute, dt.Second);
                            if (hr == 18 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 6, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 7:
                    case 19:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 19, dt.Minute, dt.Second);
                            if (hr == 7 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 19, dt.Minute, dt.Second);
                            if (hr == 19 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 7, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 8:
                    case 20:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 8, dt.Minute, dt.Second);
                            if (hr == 8 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 20, dt.Minute, dt.Second);
                            if (hr == 20 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 8, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 9:
                    case 21:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 9, dt.Minute, dt.Second);
                            if (hr == 9 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 21, dt.Minute, dt.Second);
                            if (hr == 21 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 9, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 10:
                    case 22:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 10, dt.Minute, dt.Second);
                            if (hr == 10 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 22, dt.Minute, dt.Second);
                            if (hr == 22 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 10, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 11:
                    case 23:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 11, dt.Minute, dt.Second);
                            if (hr == 11 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 23, dt.Minute, dt.Second);
                            if (hr == 23 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 11, dt.Minute, dt.Second);
                            return dt;
                        }
                    case 0:
                    case 12:
                        {
                            //return new DateTime(dt.Year, dt.Month, dt.Day, 12, dt.Minute, dt.Second);
                            if (hr == 0 && svrAmPm == _Pm) return new DateTime(dt.Year, dt.Month, dt.Day, 12, dt.Minute, dt.Second);
                            if (hr == 12 && svrAmPm == _Am) return new DateTime(dt.Year, dt.Month, dt.Day, 0, dt.Minute, dt.Second);
                            return dt;
                        }
                    default:
                        return dt;
                }
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return box.PickDt;
            }
        }

        void SaveIBTLineFromReceived(DbInfo db, List<SO1> lines)
        {
            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var sql_insert = $@"INSERT INTO {db.WEBDB}..FTAPP_IBT1_OnReceived ( 
                                     DOCENTRY
                                   , LINENUM
                                   , ITEMCODE
                                   , ITEMNAME
                                   , CODEBARS
                                   , UOMQTY
                                   , STOCKQTY
                                   , PRICE
                                   , QUANTITY
                                   , QUANTITYCS
                                   , QTY
                                   , DISC
                                   , SUPP
                                   , DISCSUM
                                   , LINETOTAL
                                   , PENTRY
                                   , PLINE
                                   , PTYPE
                                   , SUGGESTQTY
                                   , DOCNUM
                                   , BORNE
                                   , SUPPSUM
                                   , INVQTY
                                   , INVPRICE
                                   , INVTOTAL
                                   , ITEMCOST
                                   , DIM1
                                   , DIM2
                                   , DIM3
                                   , MBID
                                   , SUPPCODE
                                   , QUANTITYPC
                                   , REFNO
                                   , REFITEM
                                   , UOM
                                   , BATCHID
                                   , COKEPROMO
                                   , SUPPCATNUM
                                   , TAXCODE
                                   , PRICE2
                                   , NONIM
                                   , PROMOCOUNT
                                   , NPENTRY
                                   , NPID
                                   , NPLINE
                                   , PROMOPACKAGE
                                   , PICKEDQTY
                                   , REFLINE
                                   , REFORDER
                                   , REFUOM
                                   , TPBRANCH 
                                   , U_MustCase
                                ) VALUES (
                                    @DOCENTRY
                                   ,@LINENUM
                                   ,@ITEMCODE
                                   ,@ITEMNAME
                                   ,@CODEBARS
                                   ,@UOMQTY
                                   ,@STOCKQTY
                                   ,@PRICE
                                   ,@QUANTITY
                                   ,@QUANTITYCS
                                   ,@QTY
                                   ,@DISC
                                   ,@SUPP
                                   ,@DISCSUM
                                   ,@LINETOTAL
                                   ,@PENTRY
                                   ,@PLINE
                                   ,@PTYPE
                                   ,@SUGGESTQTY
                                   ,@DOCNUM
                                   ,@BORNE
                                   ,@SUPPSUM
                                   ,@INVQTY
                                   ,@INVPRICE
                                   ,@INVTOTAL
                                   ,@ITEMCOST
                                   ,@DIM1
                                   ,@DIM2
                                   ,@DIM3
                                   ,@MBID
                                   ,@SUPPCODE
                                   ,@QUANTITYPC
                                   ,@REFNO
                                   ,@REFITEM
                                   ,@UOM
                                   ,@BATCHID
                                   ,@COKEPROMO
                                   ,@SUPPCATNUM
                                   ,@TAXCODE
                                   ,@PRICE2
                                   ,@NONIM
                                   ,@PROMOCOUNT
                                   ,@NPENTRY
                                   ,@NPID
                                   ,@NPLINE
                                   ,@PROMOPACKAGE
                                   ,@PICKEDQTY
                                   ,@REFLINE
                                   ,@REFORDER
                                   ,@REFUOM
                                   ,@TPBRANCH 
                                   ,@U_MustCase
                            )";
                var result = conn.Execute(sql_insert, lines, trans);
                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                LastError = $"{ex.Message}\n{ex.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        IActionResult ReleaseIBTInPicking(Dto_Pick_IBT dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Company / subsi name empty");
            }
            if (string.IsNullOrWhiteSpace(dto.UserCode))
            {
                return BadRequest("User code is empty");
            }
            if (string.IsNullOrWhiteSpace(dto.UserName))
            {
                return BadRequest("User code is empty");
            }
            if (dto.DocEntry <= 0)
            {
                return BadRequest("Invalid doc entry");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("db infor return empty");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // delete the onhold entry
                var release_sql = @$"Delete from {db.WEBDB}..FTAPP_OnHold_IBT_InPicking
                                        Where HoldDocEntry = @HoldDocEntry ";
                var result = conn.Execute(release_sql,
                     new
                     {
                         HoldDocEntry = dto.DocEntry,
                     }, trans);

                if (result >= 0)
                {
                    trans.Commit();
                    // set release onhold success
                    var successLog = new FTAPP_AppPostLog
                    {
                        AppModule = "IBT Picked, release onhold",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.CardCode}",
                        SubSi = dto.Subsi,
                        Details = $"#{dto.DocEntry}, No Picked Qty, Release onhold",
                        PostResult = "Success",
                        AppVersion = $"ServerUpdate, {dto.AppVersion}"
                    };
                    AppPostLogging(successLog);
                    return Ok();
                }

                trans.Rollback();
                return BadRequest("Release on hold doc error, please try again.");
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ClearIBTDraft(Dto_Pick_IBT dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.DocEntry <= 0)
            {
                return BadRequest("Invalid doc num");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, $"{dto.Subsi}");
            if (db == null)
            {
                return BadRequest("Invalid subsi no able to continue without subsi name");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using (var trans = conn.BeginTransaction())
            {
                try
                {
                    // to clear the FTAPP_Box, FTAPP_Box and SecBox SecBox1
                    var deleteSql = @$"Delete from {db.WEBDB}..FTAPP_IBTBox_DRAFT Where BaseEntry=@DocEntry";
                    var res = conn.Execute(deleteSql, new { dto.DocEntry }, trans);

                    deleteSql = @$"Delete  from {db.WEBDB}..FTAPP_IBTBox1_DRAFT Where BaseEntry=@DocEntry";
                    res = conn.Execute(deleteSql, new { dto.DocEntry }, trans);

                    deleteSql = @$"Delete  from {db.WEBDB}..FTAPP_IBTSecBox_DRAFT Where BaseEntry=@DocEntry";
                    res = conn.Execute(deleteSql, new { dto.DocEntry }, trans);

                    deleteSql = @$"Delete  from {db.WEBDB}..FTAPP_IBTSecBox1_DRAFT Where BaseEntry=@DocEntry";
                    res = conn.Execute(deleteSql, new { dto.DocEntry }, trans);

                    deleteSql = @$"Delete  from {db.WEBDB}..FTAPP_IBT1_DRAFT Where DocEntry=@DocEntry";
                    res = conn.Execute(deleteSql, new { dto.DocEntry }, trans);

                    // clear batch draft
                    deleteSql = @$"Delete  from {db.WEBDB}..FTAPP_IBTBatch_Draft Where DocEntry=@DocEntry";
                    res = conn.Execute(deleteSql, new { dto.DocEntry }, trans);

                    // 20250417
                    // clear batch draft
                    deleteSql = @$"Delete  from {db.WEBDB}..FTAPP_IBTSecBatch_Draft Where DocEntry=@DocEntry";
                    res = conn.Execute(deleteSql, new { dto.DocEntry }, trans);

                    trans.Commit();
                    // create log for clear log
                    // 20210819 

                    var excepLog1 = new FTAPP_AppPostLog
                    {
                        AppModule = "Picked to Invoiced",
                        UserCode = $"{dto.UserCode}",
                        CardCode = $"{dto.CardCode}",
                        SubSi = $"{dto.Subsi}",
                        Details = $"{dto.DocEntry}",
                        PostResult = "Cleared IBT Picked",
                        AppVersion = $"ServerUpdate, {dto.AppVersion}"
                    };

                    AppPostLogging(excepLog1);
                    return Ok();
                }
                catch (Exception e)
                {
                    trans.Rollback();
                    LastError = $"{ e.Message}\n{e.StackTrace}";
                    _logger.LogError(LastError);
                    return BadRequest($"request not handler.\n{LastError}");
                }
            }
        }

        void AppPostLogging(FTAPP_AppPostLog log)
        {
            try
            {
                new AppPostLogHelper().Create(_commDbConnStr, log);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        IActionResult HandlerSaveIBTLineAsDaft(Dto_Pick_IBT dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The company name is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Db info reading error");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                conn.Open();
                using var transaction = conn.BeginTransaction();

                try
                {
                    var sp_Delete = @$"Delete from {db.WEBDB}..FTAPP_IBT1_DRAFT
                                     where DocEntry = @DocEntry;
                                     
                                     Delete from {db.WEBDB}..FTAPP_IBTBox_DRAFT
                                     where BaseEntry = @DocEntry ;

                                    Delete from {db.WEBDB}..FTAPP_IBTBox1_DRAFT
                                    where BaseEntry = @DocEntry ;

                                    Delete from {db.WEBDB}..FTAPP_IBTBatch_Draft 
                                    Where DocEntry = @docEntry; ";

                    conn.Execute(sp_Delete, new { dto.DocEntry }, transaction);

                    #region insert so1 draft
                    // insert back 
                    var insert_draft = @$"INSERT INTO {db.WEBDB}..FTAPP_IBT1_DRAFT (
                                              DOCENTRY
                                            , LINENUM
                                            , ITEMCODE
                                            , ITEMNAME
                                            , CODEBARS
                                            , UOMQTY
                                            , STOCKQTY
                                            , PRICE
                                            , QUANTITY
                                            , QUANTITYCS
                                            , QTY
                                            , DISC
                                            , SUPP
                                            , DISCSUM
                                            , LINETOTAL
                                            , PENTRY
                                            , PLINE
                                            , PTYPE
                                            , SUGGESTQTY
                                            , DOCNUM
                                            , BORNE
                                            , SUPPSUM
                                            , INVQTY
                                            , INVPRICE
                                            , INVTOTAL
                                            , ITEMCOST
                                            , DIM1
                                            , DIM2
                                            , DIM3
                                            , MBID
                                            , SUPPCODE
                                            , QUANTITYPC
                                            , REFNO
                                            , REFITEM
                                            , UOM
                                            , BATCHID
                                            , COKEPROMO
                                            , SUPPCATNUM
                                            , TAXCODE
                                            , PRICE2
                                            , NONIM
                                            , PROMOCOUNT
                                            , NPENTRY
                                            , NPID
                                            , NPLINE
                                            , PROMOPACKAGE
                                            , PICKEDQTY                                            
                                            , PickedPcs
                                            , PickedCase
                                            , NeededCase
                                            , NeededPcs
                                            , ContentDesc
                                            , SubSi 
                                            , REFLINE
                                            , IsMissing
                                            , IsMissingCs
                                            , IsMissingPc
                                            , IsAvailableForPick
                                            , AgencyName
                                            , AgencyCode
                                            , QUANTITYPC_Orig
                                            , QUANTITYCS_Orig
                                            , ManBtchNum 
                                            , IsSwitchToPcs
                                            , U_MustCase 
                                            ) VALUES ( 
                                             @DOCENTRY
                                            ,@LINENUM
                                            ,@ITEMCODE
                                            ,@ITEMNAME
                                            ,@CODEBARS
                                            ,@UOMQTY
                                            ,@STOCKQTY
                                            ,@PRICE
                                            ,@QUANTITY
                                            ,@QUANTITYCS
                                            ,@QTY
                                            ,@DISC
                                            ,@SUPP
                                            ,@DISCSUM
                                            ,@LINETOTAL
                                            ,@PENTRY
                                            ,@PLINE
                                            ,@PTYPE
                                            ,@SUGGESTQTY
                                            ,@DOCNUM
                                            ,@BORNE
                                            ,@SUPPSUM
                                            ,@INVQTY
                                            ,@INVPRICE
                                            ,@INVTOTAL
                                            ,@ITEMCOST
                                            ,@DIM1
                                            ,@DIM2
                                            ,@DIM3
                                            ,@MBID
                                            ,@SUPPCODE
                                            ,@QUANTITYPC
                                            ,@REFNO
                                            ,@REFITEM
                                            ,@UOM
                                            ,@BATCHID
                                            ,@COKEPROMO
                                            ,@SUPPCATNUM
                                            ,@TAXCODE
                                            ,@PRICE2
                                            ,@NONIM
                                            ,@PROMOCOUNT
                                            ,@NPENTRY
                                            ,@NPID
                                            ,@NPLINE
                                            ,@PROMOPACKAGE
                                            ,@PICKEDQTY                                            
                                            ,@PickedPcs
                                            ,@PickedCase
                                            ,@NeededCase
                                            ,@NeededPcs
                                            ,@ContentDesc
                                            ,@SubSi
                                            ,@REFLINE
                                            ,@IsMissing
                                            ,@IsMissingCs
                                            ,@IsMissingPc
                                            ,@IsAvailableForPick
                                            ,@AgencyName
                                            ,@AgencyCode
                                            ,@QUANTITYPC_Orig
                                            ,@QUANTITYCS_Orig
                                            ,@ManBtchNum
                                            , @IsSwitchToPcs
                                            , @U_MustCase
                                            )";
                    var insert_res = conn.Execute(insert_draft, dto.PickedDoc.Lines.Distinct().ToList(), transaction);
                    #endregion

                    #region insert the boxes as draft
                    if (dto.Boxes != null && dto.Boxes.Count > 0)
                    {
                        // insert box 
                        insert_draft = @$"INSERT INTO {db.WEBDB}..FTAPP_IBTBox_DRAFT (
                                     BoxId
                                   , PickerCode
                                   , PickerName
                                   , PickDt                                   
                                   , BaseEntry
                                   , BoxGuid
                                   , IsLooseBox
                                   , CreatedDt
                                   , TimeStampSeq 
                                   , AppVersion
                                   , BoxSize  , LabelConsistTotalBoxes, PickMode
                                    ) VALUES (
                                     @BoxId                                    
                                   , @PickerCode
                                   , @PickerName
                                   , @PickDt                                   
                                   , @BaseEntry
                                   , @BoxGuid
                                   , @IsLooseBox
                                   , GETDATE()
                                   , @TimeStampSeq 
                                   , @AppVersion
                                   , @BoxSize , @LabelConsistTotalBoxes, @PickMode
                                    )";

                        insert_res = conn.Execute(insert_draft, dto.Boxes, transaction);
                    }

                    #endregion

                    // insert the box content 
                    #region insert box content as draft
                    dto.Boxes.ForEach(c =>
                    {
                        if (c.Contents != null)
                        {
                            insert_draft = @$"INSERT INTO {db.WEBDB}..FTAPP_IBTBox1_DRAFT
                                            ( ItemCode
                                            , ItemName
                                            , Qty
                                            , Packaging
                                            , BoxGuid
                                            , ContentGuid
                                            , BaseEntry
                                            , BaseLine
                                            ) values (
                                             @ItemCode
                                            ,@ItemName
                                            ,@Qty
                                            ,@Packaging
                                            ,@BoxGuid
                                            ,@ContentGuid
                                            ,@BaseEntry
                                            ,@BaseLine) ";

                            insert_res = conn.Execute(insert_draft, c.Contents, transaction);
                        }
                    });
                    #endregion for box content draft

                    // if batch exist
                    // insert batch 
                    for (int b = 0; b < dto.PickedDoc.Lines.Count; b++)
                    {
                        var line = dto.PickedDoc.Lines[b];
                        if (line == null) continue;
                        if (line.FTAPP_Batches == null) continue;
                        if (line.FTAPP_Batches.Count == 0) continue;

                        InsertIBTLineBatch(db, conn, transaction, line.FTAPP_Batches, dto.DocEntry, "FTAPP_IBTBatch_Draft");
                    }
                    
                    transaction.Commit();
                }

                catch (Exception e)
                {
                    transaction.Rollback();
                    LastError = $"{e.Message}\n{e.StackTrace}";
                    _logger.LogError(LastError);
                    return BadRequest($"request not handler.\n{LastError}");
                }

                //var replied = new SoDocResult
                //{
                //    actionSuccess = true,
                //    errorMessage = "",
                //    actionResult = $"{dto.DocEntry}",
                //    documentStatus = "draft",
                //    updateDocType = "draft",
                //    docType = "draft"
                //};

                var replied = new
                {
                    ITDocNum = 0,
                    ReqDocNum = "",
                    Subsi = ""
                };

                return Ok(replied);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        void InsertIBTLineBatch(DbInfo db, SqlConnection conn,
                            SqlTransaction trans, List<FTAPP_Batch> list, int docEntry, string tableName)
        {
            try
            {
                var insert_sql = $@"INSERT INTO {db.WEBDB}..{tableName} (
                                         DocEntry
                                       , BaseLine
                                       , LineNum
                                       , ItemCode
                                       , ItemName
                                       , WhsCode
                                       , WhsName
                                       , BatchNo
                                       , BatchQty
                                       , CsQty
                                       , PcQty
                                       , OBTQ_Abs
                                       , OBTN_Abs
                                       , UomQty
                                       , PickedQty
                                       , PickedCsQty
                                       , PickedPcQty     
                                       , BoxId
                                       , AppVersion
                                       , TransDt, PickingMode
                                ) VALUES ( 
                                           @DocEntry
                                          ,@BaseLine
                                          ,@LineNum
                                          ,@ItemCode
                                          ,@ItemName
                                          ,@WhsCode
                                          ,@WhsName
                                          ,@BatchNo
                                          ,@BatchQty
                                          ,@CsQty
                                          ,@PcQty
                                          ,@OBTQ_Abs
                                          ,@OBTN_Abs
                                          ,@UomQty
                                          ,@PickedQty
                                          ,@PickedCsQty
                                          ,@PickedPcQty
                                          ,@BoxId
                                          ,@AppVersion
                                          ,GETDATE() , @PickingMode ) ";
                conn.Execute(insert_sql, list, trans);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
            }
        }

        IActionResult SaveIBTSecBoxDraft(Dto_Pick_IBT dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi");
            }
            if (dto.DocEntry <= 0)
            {
                return BadRequest("Invalid doc entry");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("Db info reading error");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using (var trans = conn.BeginTransaction())
            {
                try
                {
                    var deleteBoxContent = @$"Delete from {db.WEBDB}..FTAPP_IBTSecBox1_Draft WHERE  BaseEntry = @DocEntry; 
                                              Delete from {db.WEBDB}..FTAPP_IBTSecBox_Draft WHERE BaseEntry = @DocEntry;
                                              Delete from {db.WEBDB}..FTAPP_IBTSecBatch_Draft WHERE DocEntry = @DocEntry; ";

                    var result = conn.Execute(deleteBoxContent,
                        new
                        {
                            dto.DocEntry
                        }, trans);

                    #region insert the boxes as draft
                    // insert box 
                    var insert_draft = @$"INSERT INTO {db.WEBDB}..FTAPP_IBTSecBox_DRAFT (
                                     BoxId
                                   , PickerCode
                                   , PickerName
                                   , PickDt                                   
                                   , BaseEntry
                                   , BoxGuid
                                   , IsLooseBox
                                   , CreatedDt
                                   , TimeStampSeq 
                                   , AppVersion
                                   , BoxSize , LabelConsistTotalBoxes, PickMode
                                    ) VALUES (
                                     @BoxId                                    
                                   , @PickerCode
                                   , @PickerName
                                   , @PickDt                                   
                                   , @BaseEntry
                                   , @BoxGuid
                                   , @IsLooseBox
                                   , GETDATE()
                                   , @TimeStampSeq 
                                   , @AppVersion
                                   , @BoxSize, @LabelConsistTotalBoxes, @PickMode
                                    )";

                    var insert_res = conn.Execute(insert_draft, dto.Boxes, trans);
                    #endregion

                    // insert the box content 
                    #region insert box content as draft
                    dto.Boxes.ForEach(c =>
                    {
                        if (c.Contents != null)
                        {
                            insert_draft = @$"INSERT INTO {db.WEBDB}..FTAPP_IBTSecBox1_DRAFT
                                            ( ItemCode
                                            , ItemName
                                            , Qty
                                            , Packaging
                                            , BoxGuid
                                            , ContentGuid
                                            , BaseEntry
                                            , BaseLine
                                            ) values (
                                             @ItemCode
                                            ,@ItemName
                                            ,@Qty
                                            ,@Packaging
                                            ,@BoxGuid
                                            ,@ContentGuid
                                            ,@BaseEntry
                                            ,@BaseLine) ";

                            insert_res = conn.Execute(insert_draft, c.Contents, trans);
                        }
                    });
                    #endregion for box content draft

                    // insert the batch draft for ibt
                    // 20250417
                    if (dto.Batches != null)
                    {
                        var insertBatch = @$"INSERT INTO {db.WEBDB}..FTAPP_IBTSecBatch_Draft (
                                                DocEntry
                                               ,BaseLine
                                               ,LineNum
                                               ,ItemCode
                                               ,ItemName
                                               ,WhsCode
                                               ,WhsName
                                               ,BatchNo
                                               ,BatchQty
                                               ,CsQty
                                               ,PcQty
                                               ,OBTQ_Abs
                                               ,OBTN_Abs
                                               ,UomQty
                                               ,PickedQty
                                               ,PickedCsQty
                                               ,PickedPcQty
                                               ,BoxId
                                               ,AppVersion
                                               ,TransDt , PickingMode
                                                ) values (
                                                      @DocEntry
                                                     ,@BaseLine
                                                     ,@LineNum
                                                     ,@ItemCode
                                                     ,@ItemName
                                                     ,@WhsCode
                                                     ,@WhsName
                                                     ,@BatchNo
                                                     ,@BatchQty
                                                     ,@CsQty
                                                     ,@PcQty
                                                     ,@OBTQ_Abs
                                                     ,@OBTN_Abs
                                                     ,@UomQty
                                                     ,@PickedQty
                                                     ,@PickedCsQty
                                                     ,@PickedPcQty
                                                     ,@BoxId
                                                     ,@AppVersion
                                                     , GETDATE(), @PickingMode )";

                        var insert_res_batch = conn.Execute(insertBatch, dto.Batches, trans);                        
                    }

                    trans.Commit();
                    return Ok();
                }
                catch (Exception e)
                {
                    trans.Rollback();
                    LastError = $"{ e.Message}\n{e.StackTrace}";
                    _logger.LogError(LastError);
                    return BadRequest($"request not handler.\n{LastError}");
                }
            }
        }

        IActionResult AddIBTPickLog(Dto_Pick_IBT dto)
        {
            if (dto.Log == null)
            {
                return BadRequest("log is empty");
            }
            if (string.IsNullOrWhiteSpace(dto.Log.Subsi))
            {
                return BadRequest("the subsi is empty");
            }
            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Log.Subsi);
            if (db == null)
            {
                return BadRequest("The db info reading error, please try again.");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var insert_sql = @$"INSERT INTO {db.WEBDB}..FTAPP_IBTPickLog (
                                        TransDt
                                       ,UserCode
                                       ,UserName
                                       ,AuthUserCode
                                       ,AuthUserName
                                       ,ItemCode
                                       ,ItemName
                                       ,CodeBars
                                       ,NeededQty
                                       ,NeededQtyInPc
                                       ,NeededQtyInCs
                                       ,PickedQty
                                       ,PickedQtyInPcs
                                       ,PickedQtyInCs
                                       ,Uom
                                       ,ReportAs
                                       ,WhsName
                                       ,Subsi
                                       ,Branch
                                       ,BranchCode
                                       ,AgencyName
                                       ,AgencyCode
                                       ,BaseEntry
                                       ,BaseLine
                                       ,DocNum
                                       ,StickerNum
                                       ,AppVersion , LabelConsistTotalBoxes
                                      ) VALUES (
                                           GETDATE()
                                         , @UserCode
                                         , @UserName
                                         , @AuthUserCode
                                         , @AuthUserName
                                         , @ItemCode
                                         , @ItemName
                                         , @CodeBars
                                         , @NeededQty
                                         , @NeededQtyInPc
                                         , @NeededQtyInCs
                                         , @PickedQty
                                         , @PickedQtyInPcs
                                         , @PickedQtyInCs
                                         , @Uom
                                         , @ReportAs
                                         , @WhsName
                                         , @Subsi
                                         , @Branch
                                         , @BranchCode
                                         , @AgencyName
                                         , @AgencyCode
                                         , @BaseEntry
                                         , @BaseLine
                                         , @DocNum
                                         , @StickerNum 
                                         , @AppVersion, @LabelConsistTotalBoxes
                                )";


                conn.Execute(insert_sql, dto.Log, trans);
                trans.Commit();
                return Ok();
            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult ClearIBTSecBoxLine(Dto_Pick_IBT dto)
        {
            // to clear the sec box 
            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Sub si invalid");
            }
            if (dto.DocEntry < 0)
            {
                return BadRequest("doc entry invalid");
            }
            if (dto.LineNum < 0)
            {
                return BadRequest("line number invalid");
            }
            if (string.IsNullOrWhiteSpace(dto.ItemCode))
            {
                return BadRequest("item code invalid");
            }

            var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (db == null)
            {
                return BadRequest("db info invalid");
            }

            using var conn = new SqlConnection(_commDbConnStr);
            conn.Open();
            using (var trans = conn.BeginTransaction())
            {
                try
                {
                    // delete the box content
                    var delete_sp = 
                                        @$"Delete from {db.WEBDB}..FTAPP_IBTSecBox1_Draft WHERE BaseEntry = @DocEntry;
                                           Delete from {db.WEBDB}..FTAPP_IBTSecBox_Draft  WHERE BaseEntry = @DocEntry; 
                                           Delete from {db.WEBDB}..FTAPP_IBTSecBatch_Draft  WHERE DocEntry = @DocEntry; ";

                    var result = conn.Execute(delete_sp,
                        new
                        {
                            dto.DocEntry
                        }, trans);                                

                    trans.Commit();
                    return Ok();
                }
                catch (Exception e)
                {
                    trans.Rollback();
                    LastError = $"{ e.Message}\n{e.StackTrace}";
                    _logger.LogError(LastError);
                    return BadRequest($"request not handler.\n{LastError}");
                }
            }
        }

        // 20250417
        IActionResult GetSectionBatches (Dto_Pick_IBT dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi invalid");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Doc entry invalid");
                }
                if (dto.LineNum < 0)
                {
                    return BadRequest("Doc line num invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.ItemCode))
                {
                    return BadRequest("item code invalid");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Subsi name for db info invalid");
                }

                var sql = @$"SELECT distinct * 
                            FROM {db.WEBDB}..FTAPP_IBTSecBatch_Draft WITH (NOLOCK)
                            WHERE DocEntry = @DocEntry 
                                AND ItemCode  = @ItemCode                            
                                AND BaseLine  = @LineNum";

                using var conn = new SqlConnection(_commDbConnStr);
                var batches = conn.Query<FTAPP_Batch>(sql, new
                {
                    DocEntry = dto.DocEntry,
                    ItemCode = dto.ItemCode,
                    LineNum = dto.LineNum
                }).ToList();

                if (batches.Count == 0) return NotFound();
                return Ok(batches);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult GetIBTSecBoxDraft(Dto_Pick_IBT dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Subsi invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("User code invalid");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Doc entry invalid");
                }
                if (dto.LineNum < 0)
                {
                    return BadRequest("Doc line num invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.ItemCode))
                {
                    return BadRequest("item code invalid");
                }
                if (string.IsNullOrWhiteSpace(dto.Packaging))
                {
                    return BadRequest("packaging invalid");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Subsi name for db info invalid");
                }

                var sql = @$"SELECT distinct * 
                            FROM {db.WEBDB}..FTAPP_IBTSecBox_Draft 
                            WHERE BaseEntry = @DocEntry 
                            AND PickerCode = @PickerCode";

                using var conn = new SqlConnection(_commDbConnStr);
                var boxes = conn.Query<FTAPP_Box>(sql, new
                {
                    dto.DocEntry,
                    PickerCode = dto.UserCode
                }).ToList();

                if (boxes == null) return NotFound();
                if (boxes.Count == 0) return NotFound();

                var returnBoxes = new List<FTAPP_Box>();
                for (int id = 0; id < boxes.Count; id++)
                {
                    var sql_boxContent = $@"SELECT distinct * FROM {db.WEBDB}..FTAPP_IBTSecBox1_Draft 
                                         WHERE BoxGuid = @BoxGuid";

                    var contents = conn.Query<FTAPP_Box1>(sql_boxContent, new
                    {
                        BoxGuid = boxes[id].BoxGuid
                    }).ToList();

                    if (contents == null) continue;
                    if (contents.Count == 0) continue;

                    boxes[id].Contents = contents;
                    returnBoxes.Add(boxes[id]);
                }

                if (returnBoxes.Count == 0) return NotFound();
                return Ok(returnBoxes);
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }

        }

        IActionResult GetQueueIBTLines(Dto_Pick_IBT dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The company name is empty");
                }
                if (string.IsNullOrWhiteSpace(dto.WhsCode))
                {
                    return BadRequest("The warehouse code is empty");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("The compay info is empty");
                }

                using var conn = new SqlConnection(_commDbConnStr);
                var sql_ibt = @$"SELECT * FROM {db.WEBDB}..IBT With (nolock) Where DocEntry = @DocEntry";
                var ibt = conn.Query<SO>(sql_ibt, new { DocEntry = dto.DocEntry }).FirstOrDefault();
                if (ibt == null)
                {
                    return BadRequest($"The docentry {dto.DocEntry} no found");
                }

                // 20230514
                // hide for IBT picking 
                // 20230216
                // query predefine bin and exp dates
                // if any 
                //var query_soPick1 = @$"select ID
                //                        ,  DOCENTRY
                //                        ,  LINENUM
                //                        ,  ITEMCODE
                //                        ,  REFITEM
                //                        ,  PICKLISTNO
                //                        ,  BIN
                //                        ,  EXPIRED
                //                        ,  QUANTITY
                //                        ,  WEIGHT
                //                from {db.WEBDB}..SOPICK1 
                //                Where DOCENTRY = @DocEntry ";

                //var soPick1s = conn.Query<SOPICK1>(query_soPick1, new { DocEntry = dto.DocEntry }).ToList();

                // query each lines
                //var sql = "exec sp_SelectQueueIBTLine @webDB,  @docEntry, @whsCode ";

                var sql = "exec sp_SelectQueueIBTLine @webDB,  @docEntry, @whsCode ";
                var lines = conn.Query<SO1>(sql, new
                {
                    webDb = db.WEBDB,
                    docEntry = dto.DocEntry,
                    whsCode = dto.WhsCode
                }).Distinct().ToList();

                // 20220412
                // if found not line 
                if (lines.Count == 0) // mean no store
                {
                    //var dto1 = new Dto_Pick
                    //{
                    //    Subsi = dto.Subsi,
                    //    DocEntry = (int)ibt.DOCENTRY,
                    //    CardCode = ibt.CARDCODE,
                    //    UserCode = "SVR",
                    //    AppVersion = "Latest"
                    //};

                    ////SetSOOutStock(dto1);

                    var replied1 = new { Lines = new List<SO1>() }; // trigger app to auto start next
                    return Ok(replied1);
                }



                var Orderedlines = lines.OrderBy(n => n.ITEMNAME).Distinct().ToList();

                var newlines = new List<SO1>();
                for (int i = 0; i < Orderedlines.Count; i++)
                {
                    var line = Orderedlines[i];
                    if (line == null) continue;

                    if (line.QUANTITY == 0) continue;
                    var uomQty = line.UOMQTY == 0 ? 1 : line.UOMQTY; // avoid zero devision

                    if (uomQty == 1)
                    {
                        if($"{line.U_MustCase}".ToLower().Equals("y"))
                        {
                            line.QUANTITYPC = 0;
                            line.QUANTITYCS = line.QUANTITY;
                            line.UOM = "CS";                            
                        }
                        else
                        {
                            line.QUANTITYPC = line.QUANTITY;
                            line.QUANTITYCS = 0;
                            line.UOM = "PC";                            
                        }

                        goto ByPassLineSetup;
                    }

                    // recalculate the cs and pc
                    line.QUANTITYCS = (int)Math.Floor(line.QUANTITY / uomQty);
                    line.QUANTITYPC = (int)(line.QUANTITY % uomQty);

                    // determine the UOM
                    if (line.QUANTITYPC == 0)
                    {
                        line.UOM = "CS";
                    }
                    else if (line.QUANTITYCS == 0)
                    {
                        line.UOM = "PC";
                    }
                    else if (line.QUANTITYCS > 0 && line.QUANTITYPC > 0) // both had value
                    {
                        line.UOM = "";
                    }
                    else if (line.QUANTITYCS > 0 && line.QUANTITYPC == 0)
                    {
                        line.UOM = "CS";
                    }
                    else if (line.QUANTITYCS == 0 && line.QUANTITYPC > 0)
                    {
                        line.UOM = "PC";
                    }

                    ByPassLineSetup:
                    // load the barcode table if any 
                    var sp_loadBarCodes = @"exec sp_SelectOBCD @erpDb, @itemCode";
                    line.BarCodes = conn.Query<OBCD_Ext>(sp_loadBarCodes,
                        new
                        {
                            erpDb = db.SAPDB,
                            itemCode = line.ITEMCODE
                        }).ToList();

                    // add more barcode 
                    //add itemcode as barcode
                    var itemCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = line.ITEMCODE,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };

                    line.BarCodes.Add(itemCode);

                    // copy the item master barcode 
                    var IMCode = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = line.CODEBARS,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    line.BarCodes.Add(IMCode);

                    // the suppcatnum
                    var supcatnum = new OBCD_Ext
                    {
                        BcdEntry = -1,
                        BcdCode = line.SUPPCATNUM,
                        BcdName = "EA",
                        UomEntry = -1,
                        DataSource = "O",
                        UserSign = 1,
                        LogInstanc = 0,
                        UserSign2 = 1,
                        UpdateDate = DateTime.Now,
                        CreateDate = DateTime.Now
                    };
                    line.BarCodes.Add(supcatnum);
                    newlines.Add(line);

                    // 20230216
                    // load in the SO pick 1 data                     
                    //if (soPick1s.Count > 0)
                    //{
                    //    line.SoPick1s = soPick1s
                    //        .Where(x => x.ITEMCODE == line.ITEMCODE &&
                    //                    x.LINENUM == line.LINENUM).ToList();
                    //}

                }

                // load the box content as well
                // query the box 
                sql = @$"SELECT * FROM {db.WEBDB}..FTAPP_IBTBox_DRAFT
                        Where BaseEntry = @DocEntry";

                var boxes = conn.Query<FTAPP_Box>(sql, new { dto.DocEntry }).ToList();
                if (boxes != null && boxes.Count > 0)
                {
                    for (int b = 0; b < boxes.Count; b++)
                    {
                        var sql_bContent = $@"SELECT * FROM {db.WEBDB}..FTAPP_IBTBox1_DRAFT
                                                  Where BaseEntry = @DocEntry 
                                                  AND BoxGuid = @BoxGuid";

                        boxes[b].Contents = conn.Query<FTAPP_Box1>(sql_bContent,
                            new
                            {
                                DocEntry = dto.DocEntry,
                                BoxGuid = boxes[b].BoxGuid
                            }).ToList();
                    }
                }

                var replied = new { Lines = newlines, Boxes = boxes };
                return Ok(replied);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult CheckIBTLineDraftExist(Dto_Pick_IBT dto)
        {
            // 20230102
            // remove the with no lock from the draft select statement. 

            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("The subsi name is empty");
                }
                if (dto.DocEntry < 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("The db info reading error, please try again.");
                }

                // check got save draft
                var sql_DraftExist = @$"SELECT distinct 
                                               DOCENTRY
                                              , LINENUM
                                              , ITEMCODE
                                              , ITEMNAME
                                              , CODEBARS
                                              , UOMQTY
                                              , STOCKQTY
                                              , PRICE
                                              , QUANTITY
                                              , QUANTITYCS
                                              , QTY
                                              , DISC
                                              , SUPP
                                              , DISCSUM
                                              , LINETOTAL
                                              , PENTRY
                                              , PLINE
                                              , PTYPE
                                              , SUGGESTQTY
                                              , DOCNUM
                                              , BORNE
                                              , SUPPSUM
                                              , INVQTY
                                              , INVPRICE
                                              , INVTOTAL
                                              , ITEMCOST
                                              , DIM1
                                              , DIM2
                                              , DIM3
                                              , MBID
                                              , SUPPCODE
                                              , QUANTITYPC
                                              , REFNO
                                              , REFITEM
                                              , UOM
                                              , BATCHID
                                              , COKEPROMO
                                              , SUPPCATNUM
                                              , TAXCODE
                                              , PRICE2
                                              , NONIM
                                              , PROMOCOUNT
                                              , NPENTRY
                                              , NPID
                                              , NPLINE
                                              , PROMOPACKAGE
                                              , PICKEDQTY
                                              , REFLINE
                                              , PickedPcs
                                              , PickedCase
                                              , NeededCase
                                              , NeededPcs
                                              , ContentDesc
                                              , SubSi
                                              , IsMissing
                                              , IsMissingCs
                                              , IsMissingPc
                                              , IsAvailableForPick
                                              , AgencyName
                                              , AgencyCode
                                              , QUANTITYPC_Orig
                                              , QUANTITYCS_Orig
                                              , ManBtchNum
                                              , 1 [IsIBTLine]
                                              , IsSwitchToPcs
                                            FROM [{db.WEBDB}].[dbo].[FTAPP_IBT1_DRAFT]
                                        Where 
                                            DOCENTRY= @DocEntry order by ITEMNAME asc";

                using (var conn = new SqlConnection(_commDbConnStr))
                {
                    // read the distinct line only
                    // double line ignore
                    var results = conn.Query<SO1>(sql_DraftExist, new { dto.DocEntry }).Distinct().ToList();

                    if (results == null) return NotFound();
                    if (results.Count == 0) return NotFound();

                    // hide
                    // may not aplicabel for IBT pick
                    //var query_soPick1 = @$"select ID
                    //                    ,  DOCENTRY
                    //                    ,  LINENUM
                    //                    ,  ITEMCODE
                    //                    ,  REFITEM
                    //                    ,  PICKLISTNO
                    //                    ,  BIN
                    //                    ,  EXPIRED
                    //                    ,  QUANTITY
                    //                    ,  WEIGHT
                    //            from {db.WEBDB}..SOPICK1 
                    //            Where DOCENTRY = @DocEntry ";

                    //var soPick1s = conn.Query<SOPICK1>(query_soPick1, new { DocEntry = dto.DocEntry }).ToList();

                    // massge the so line 
                    //var newlines = new List<SO1>(); // IBT
                    for (int i = 0; i < results.Count; i++)
                    {
                        //var line = results[i];
                        if (results[i] == null) continue;

                        if (results[i].QUANTITY == 0) continue;

                        // 20250417                        
                        // load the batch 
                        // if batch manage
                        if (results[i].ManBtchNum == "Y")
                        {
                            var queryBatch = @$"select * from {db.WEBDB}..FTAPP_IBTBatch_Draft 
                                            Where docentry = @docentry
                                            and baseline = @baseline
                                            and itemCode = @itemCode ";

                            results[i].FTAPP_Batches = conn.Query<FTAPP_Batch>(queryBatch, new
                            {
                                docentry = results[i].DOCENTRY,
                                baseline = results[i].LINENUM,
                                itemcode = results[i].ITEMCODE
                            }).ToList();
                        }


                        // avoid devision by zero
                        var uomQty = results[i].UOMQTY == 0 ? 1 : results[i].UOMQTY;
                        if (uomQty == 1)
                        {
                            results[i].QUANTITYPC = results[i].QUANTITY;
                            results[i].QUANTITYCS = 0;
                            results[i].UOM = "PC";
                            goto ContinueLoad;
                        }

                        // recalculate the cs and pc
                        results[i].QUANTITYCS = (int)Math.Floor(results[i].QUANTITY / uomQty);
                        results[i].QUANTITYPC = (int)(results[i].QUANTITY % uomQty);


                        // determine the UOM
                        if (results[i].QUANTITYPC == 0)
                        {
                            results[i].UOM = "CS";
                        }
                        else if (results[i].QUANTITYCS == 0)
                        {
                            results[i].UOM = "PC";
                        }
                        else if (results[i].QUANTITYCS > 0 && results[i].QUANTITYPC > 0) // both had value
                        {
                            results[i].UOM = "";
                        }
                        else if (results[i].QUANTITYCS > 0 && results[i].QUANTITYPC == 0)
                        {
                            results[i].UOM = "CS";
                        }
                        else if (results[i].QUANTITYCS == 0 && results[i].QUANTITYPC > 0)
                        {
                            results[i].UOM = "PC";
                        }

                    ContinueLoad:

                        // load the barcode table if any 
                        var sp_loadBarCodes = @"exec sp_SelectOBCD @erpDb, @itemCode";
                        results[i].BarCodes = conn.Query<OBCD_Ext>(sp_loadBarCodes,
                                new { erpDb = db.SAPDB, itemCode = results[i].ITEMCODE }).ToList();

                        // load the draft batch is any
                        // 20211121T1637
                        var query_linebatches = @$"Select * from {db.WEBDB}..FTAPP_IBTBatch_Draft
                                                Where DocEntry = @DocEntry and
                                                      BaseLine = @BaseLine and 
                                                      ItemCode = @ItemCode
                                                order by boxid ";

                        results[i].FTAPP_Batches = conn.Query<FTAPP_Batch>(query_linebatches, new
                        {
                            DocEntry = results[i].DOCENTRY,
                            BaseLine = results[i].LINENUM,
                            ItemCode = results[i].ITEMCODE
                        }).ToList();

                        // 20230216
                        // load in the SO pick 1 data
                        // // hide, may not applicable for IBT 
                        //if (soPick1s.Count > 0)
                        //{
                        //    results[i].SoPick1s = soPick1s
                        //        .Where(x => x.ITEMCODE == results[i].ITEMCODE &&
                        //                    x.LINENUM == results[i].LINENUM).ToList();
                        //}
                    }

                    // query the box 
                    var sql = @$"SELECT * FROM {db.WEBDB}..FTAPP_IBTBox_DRAFT
                                Where BaseEntry = @DocEntry";

                    var boxes = conn.Query<FTAPP_Box>(sql, new { dto.DocEntry }).ToList();
                    if (boxes != null && boxes.Count > 0)
                    {
                        for (int b = 0; b < boxes.Count; b++)
                        {
                            var sql_bContent = $@"SELECT * FROM {db.WEBDB}..FTAPP_IBTBox1_DRAFT
                                                  Where BaseEntry = @DocEntry 
                                                  AND BoxGuid = @BoxGuid";

                            boxes[b].Contents = conn.Query<FTAPP_Box1>(sql_bContent,
                                new
                                {
                                    DocEntry = dto.DocEntry,
                                    BoxGuid = boxes[b].BoxGuid
                                }).ToList();
                        }
                    }

                    var replied = new { DraftLines = results, Boxes = boxes };
                    return Ok(replied);
                }
            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

        IActionResult IBT_FinalCheckOnHold(Dto_Pick_IBT dto)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(dto.Subsi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (dto.DocEntry <= 0)
                {
                    return BadRequest("Invalid doc entry");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
                if (db == null)
                {
                    return BadRequest("Invalid subsi db");
                }

                // start of the transaction scope
                using var conn = new SqlConnection(_commDbConnStr);

                // check the doc is in transist
                var checkDocSql = @$"Select * from {db.WEBDB}..IBT Where DocEntry = @docEntry";
                var Ibt = conn.Query<KTC_SalesAppWAPI.Models.Pick_IBT.IBT>(checkDocSql, new { docEntry = dto.DocEntry }).FirstOrDefault();

                if (Ibt == null)
                {
                    return BadRequest($"The IBT doc {Ibt.DOCENTRY} no exit, please try again.");
                }

                if (!$"{Ibt.DOCSTATUS}".ToLower().Equals("q"))
                {
                    return BadRequest($"The IBT doc {Ibt.DOCENTRY} no in queue status, no picking allowed.");
                }

                var checkExistInOnHold = @$"Select * from {db.WEBDB}..FTAPP_OnHold_IBT_InPicking 
                                            Where HoldDocEntry = @docEntry ";

                var onHoldIbt = conn.Query<FTAPP_OnHold_IBT_InPicking>(checkExistInOnHold, new { docEntry = dto.DocEntry }).FirstOrDefault();
                if (onHoldIbt != null)
                {
                    return Ok();
                }

                // if no onhold 

                // put the doc onhold
                var newOnhold = new FTAPP_OnHold_IBT_InPicking
                {
                    HoldDocEntry = dto.DocEntry,
                    HoldByUserCode = dto.UserCode,
                    HoldByUserName = dto.UserName,
                    HoldStartDt = DateTime.Now,
                    HoldReason = "Picking"
                };



                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    var insertSql = $@"Insert into {db.WEBDB}..FTAPP_OnHold_IBT_InPicking (
                                     HoldDocEntry 
                                   , HoldByUserCode
                                   , HoldByUserName
                                   , HoldStartDt
                                   , HoldReason 
                                ) values (
                                      @HoldDocEntry 
                                    , @HoldByUserCode
                                    , @HoldByUserName
                                    , GETDATE()
                                    , @HoldReason 
                                )";
                    conn.Execute(insertSql, newOnhold, trans);
                    trans.Commit();
                    return Ok();
                }
                catch (Exception e)
                {
                    trans.Rollback();

                    LastError = $"{ e.Message}\n{e.StackTrace}";
                    _logger.LogError(LastError);
                    return BadRequest($"request not handler.\n{LastError}");
                }


            }
            catch (Exception e)
            {
                LastError = $"{ e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }
    }
}
