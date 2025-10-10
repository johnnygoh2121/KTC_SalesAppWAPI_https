using Dapper;
using KTC_SalesAppWAPI.DTOs.Delivery;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Helpers.Delivery;
using KTC_SalesAppWAPI.Models.AppConfig;
using KTC_SalesAppWAPI.Models.COG;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Delivery;
using KTC_SalesAppWAPI.Models.Login;
using KTC_SalesAppWAPI.Models.Pick;
using KTC_SalesAppWAPI.Models.SalesOrder;
using KTC_SalesAppWAPI.Models.TrcukInspection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.Delivery
{
    [Route("[controller]")]
    [ApiController]
    public class TruckController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<TruckController> _logger;
        string _commDbConnStr_bread = "";
        string _commDbConnStr = "";
        string _localAttchPath = "";
        string LastError = "";

        public TruckController(IConfiguration configuration, ILogger<TruckController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_bread = _configuration.GetConnectionString("MasterConn_Bread");
            _localAttchPath = configuration.GetSection("WebAttachmentPath").Value;
        }

        [HttpPost]
        public IActionResult PostAsync(DTO_Truck dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "CheckTruck":
                    {

                        return CheckTruck(dto);
                    }

                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult CheckTruck(DTO_Truck dto)
        {

            if (string.IsNullOrWhiteSpace(dto.Subsi))
            {
                return BadRequest("Invalid subsi company, please try again. Thanks.");
            }

            var dbs = new DbNameHelper().GetDbInfo(_commDbConnStr, dto.Subsi);
            if (dbs == null)
            {
                return BadRequest("Invalid subsi");
            }

            var foundTruck_sp = @$"SELECT * 
                                        FROM {dbs.WEBDB}..TRUCK1 with (nolock) 
                                        WHERE TRUCKNO = @PlateNo";

            using var conn = new SqlConnection(_commDbConnStr);
            var foundTruck = conn.Query<TRUCK1>(foundTruck_sp, new { PlateNo = dto.PlateNo }).FirstOrDefault();
            if (foundTruck == null)
            {
                return BadRequest($"{dbs.COMPANYNAME}, Invalid truck no {dto.PlateNo}, Please try save again. Thanks. ");
            }

            // decry the data from phone
            string _TheKey = "888imrich888"; // screct key for decryt 
            var crythelper = new MD5EnDecrytor();
            var op = crythelper.Decrypt(dto.Oldp, true, _TheKey);
            var np = crythelper.Decrypt(dto.Newp, true, _TheKey);

            if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                if (op == foundTruck.PASS)
                {

                    foundTruck.PASS = np;
                    var update_pass_sp = @$"UPDATE {dbs.WEBDB}..TRUCK1 
                                            SET PASS = @PASS 
                                            WHERE LINENUM = @LINENUM
                                             AND CARDCODE = @CARDCODE 
                                             AND TRUCKNO = @TRUCKNO ";

                    var res = conn.Execute(update_pass_sp, foundTruck, trans);
                    if (res <= 0)
                    {
                        trans.Rollback();
                        LastError = $"{dbs.COMPANYNAME}, Error PASS for Truck {dto.PlateNo}";
                        _logger.LogError(LastError);
                        return BadRequest($"request not handler.\n{LastError}");
                    }

                    // upfate the PASS column
                    trans.Commit();
                    return Ok();
                }


                if (op == foundTruck.PASS2)
                {
                    foundTruck.PASS2 = np;
                    var update_pass_sp = @$"UPDATE {dbs.WEBDB}..TRUCK1 
                                            SET PASS2 = @PASS2 
                                            WHERE LINENUM = @LINENUM
                                             AND CARDCODE = @CARDCODE 
                                             AND TRUCKNO = @TRUCKNO ";

                    var res = conn.Execute(update_pass_sp, foundTruck, trans);
                    if (res <= 0)
                    {
                        trans.Rollback();
                        LastError = $"{dbs.COMPANYNAME}, Error PASS for Truck {dto.PlateNo}";
                        _logger.LogError(LastError);
                        return BadRequest($"request not handler.\n{LastError}");
                    }

                    // upfate the PASS column
                    trans.Commit();
                    return Ok();
                }

                trans.Rollback();
                return BadRequest("Invalid old password value, no change applied. Please try again. Thanks !");

            }
            catch (Exception e)
            {
                trans.Rollback();
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest($"request not handler.\n{LastError}");
            }
        }

    }
}
