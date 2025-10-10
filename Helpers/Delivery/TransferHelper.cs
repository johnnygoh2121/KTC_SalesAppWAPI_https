using Dapper;
using KTC_SalesAppWAPI.Models.CommonDb;
using KTC_SalesAppWAPI.Models.Transfer;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace KTC_SalesAppWAPI.Helpers.Delivery
{
    public class TransferHelper
    {
        public string LastError { get; set; }
        DbInfo Db { get; set; }

        public TransferHelper(DbInfo db)
        {
            Db = db;
        }

        public bool CreateTransfer(FTAPP_Transfer transfer, List<FTAPP_Transfer1> transfer1s,
            List<FTAPP_Transfer2> transfers2, SqlConnection updateConn, SqlTransaction updateTrans, 
            out string errCreateTransfer)
        {
            errCreateTransfer = string.Empty;
            try
            {
                // insert the box 
                if (transfers2 != null && transfers2.Count > 0)
                {
                    var insert_boxes_sql = @$"insert into {Db.WEBDB}..FTAPP_Transfer2 (
                                         BoxId
                                        ,InvNo
                                        ,TransDt
                                        ,GroupGuid 
                                    ) values (
                                         @BoxId
                                        ,@InvNo
                                        ,GETDATE()
                                        ,@GroupGuid  ) ";

                    var res = updateConn.Execute(insert_boxes_sql, transfers2, updateTrans);
                    if (res <= 0)
                    {
                        return false;
                    }
                }
               
                // insert the transfer
                var insert_transfer = @$"insert into {Db.WEBDB}..FTAPP_Transfer (
                                              ReceiverCode
                                            , ReceiverName
                                            , LocationCode
                                            , LocationName
                                            , TransDt
                                            , GroupGuid
                                            , DocStatus  
                                            , DriverName 
                                            , Module 
                                            , DLBEntry                                        
                                            ) values (
                                               @ReceiverCode
                                              ,@ReceiverName
                                              ,@LocationCode
                                              ,@LocationName
                                              ,GETDATE()
                                              ,@GroupGuid
                                              ,@DocStatus
                                              ,@DriverName 
                                              ,@Module
                                              ,@DLBEntry )";

                var res1 = updateConn.Execute(insert_transfer, transfer, updateTrans);
                if (res1 <= 0)
                {
                    return false;
                }

                // insert the invoice | transfer 1
                // insert the lines 
                var insert_lines = $@"insert into {Db.WEBDB}..FTAPP_Transfer1 ( 
                                          InvNo
                                        , TransDt
                                        , GroupGuid 
                                    ) values (  
                                          @InvNo
                                         , GETDATE()
                                         , @GroupGuid 
                                    ) ";

                var res2 = updateConn.Execute(insert_lines, transfer1s, updateTrans);
                if (res2 <= 0)
                {
                    return false;
                }

                errCreateTransfer = "";
                LastError = "";
                return true;
            }
            catch (Exception e)
            {
                LastError = $"{e.Message}\n{e.StackTrace}";
                errCreateTransfer = LastError;
                return false;
            }
        }
    }
}
