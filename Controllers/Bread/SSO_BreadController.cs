using Dapper;
using KTC_SalesAppWAPI.DTOs.Bread;
using KTC_SalesAppWAPI.Helpers;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Login;
using KTC_SalesAppWAPI.Models.SalesOrder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace KTC_SalesAppWAPI.Controllers.Bread
{
    [Route("[controller]")]
    [ApiController]
    public class SSO_BreadController : ControllerBase
    {
        readonly IConfiguration _configuration;
        readonly ILogger<SSO_BreadController> _logger;
        string LastError { get; set; } = string.Empty;
        string _commDbConnStr;
        string _commDbConnStr_Bread;

        public SSO_BreadController(IConfiguration configuration, ILogger<SSO_BreadController> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _commDbConnStr = _configuration.GetConnectionString("MasterConn");
            _commDbConnStr_Bread = _configuration.GetConnectionString("MasterConn_Bread");
        }

        [HttpPost]
        public IActionResult Post(Dto_BreadLogin dto)
        {
            var request = $"{dto.Request}";
            switch (request)
            {
                case "LoadErpUserCustomer_Bread":
                    {
                        return LoadErpUserCustomer_Bread(dto);
                    }
                case "LoadErpUserCustomer_WildCard":
                    {
                        return LoadErpUserCustomer_WildCard(dto);
                    }
                default:
                    {
                        return BadRequest("no recognised request");
                    }
            }
        }

        IActionResult LoadErpUserCustomer_WildCard (Dto_BreadLogin dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.CardType))
                {
                    return BadRequest("Invalid card type");
                }
                if (string.IsNullOrWhiteSpace(dto.WildCode))
                {
                    return BadRequest("Invalid WildCode");
                }

                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sp_query = @"exec sp_LoadErpUserCustomerWildCard_Bread @webDb, @userCode, @cardType, @wildCode";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var custList = conn.Query<OCRD_Ext>(sp_query, new
                {
                    webDb = db.WEBDB,
                    userCode = dto.UserCode,
                    cardType = dto.CardType,
                    wildCode = dto.WildCode
                }).ToList();

                if (custList.Count == 0) return NotFound();

                // massage the GPS location
                ProcessCardGLN(custList, conn, db);
                return Ok(custList);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }

        IActionResult LoadErpUserCustomer_Bread(Dto_BreadLogin dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.SubSi))
                {
                    return BadRequest("Invalid subsi");
                }
                if (string.IsNullOrWhiteSpace(dto.UserCode))
                {
                    return BadRequest("Invalid user code");
                }
                if (string.IsNullOrWhiteSpace(dto.CardType))
                {
                    return BadRequest("Invalid card type");
                }
                var db = new DbNameHelper().GetDbInfo(_commDbConnStr_Bread, dto.SubSi);
                if (db == null)
                {
                    return BadRequest("Invalid db info");
                }

                var sp_query = @"exec sp_LoadErpUserCustomer_Bread @webDb, @userCode, @cardType";
                var conn = new SqlConnection(_commDbConnStr_Bread);
                var custList = conn.Query<OCRD_Ext>(sp_query, new
                {
                    webDb = db.WEBDB,
                    userCode = dto.UserCode,
                    cardType = dto.CardType
                }).ToList();

                if (custList.Count == 0) return NotFound();

                // massage the GPS location
                ProcessCardGLN(custList, conn, db);
                return Ok(custList);
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return BadRequest(LastError);
            }
        }


        // for massage the information for each customer
        List<OCRD_Ext> ProcessCardGLN(List<OCRD_Ext> list, SqlConnection conn, DbInfo db)
        {
            try
            {
                if (list == null) return list;

                for (int x = 0; x < list.Count; x++)
                {
                    if (string.IsNullOrWhiteSpace(list[x].GlblLocNum)) continue; // cont next

                    var glnArray = list[x].GlblLocNum.Split(',');
                    if (glnArray?.Length < 2) continue; // cont. next

                    list[x].Latitude = SafeGetDouble(glnArray[0]); // actual code
                    list[x].Longitude = SafeGetDouble(glnArray[1]);

                    // get the bill address 
                    // get the s address type 
                    // get shipment address
                    var sql = @$"SELECT * FROM [{db.SAPDB}].[dbo].[CRD1] WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='B'";

                    var bill_address = conn.Query<CRD1>(sql, new { SoDocStoreCard = list[x].CardCode }).FirstOrDefault();
                    if (bill_address != null)
                    {
                        list[x].Address = bill_address.GetAddress();
                    }

                    // get the s address type 
                    // get shipment address
                    sql = @$"SELECT * FROM [{db.SAPDB}].[dbo].[CRD1] WITH (NOLOCK) 
                            WHERE CardCode = @SoDocStoreCard 
                            AND AdresType ='S'";

                    var ship_address = conn.Query<CRD1>(sql, new { SoDocStoreCard = list[x].CardCode }).FirstOrDefault();
                    if (ship_address != null)
                    {
                        list[x].ShipAdd = ship_address.GetAddress();
                    }
                }

                return list;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                _logger.LogError(LastError);
                return list;
            }
        }


        double SafeGetDouble(string _value)
        {
            try
            {
                var isNumeric = double.TryParse(_value, out double result);
                if (isNumeric) return result;
                return -1;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                return -1;
            }
        }

    }
}
