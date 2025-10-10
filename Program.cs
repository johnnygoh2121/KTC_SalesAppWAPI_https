using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using SAPbobsCOM;
using Serilog;
using Serilog.Formatting.Compact;
using System;
using System.Collections.Generic;

namespace KTC_SalesAppWAPI
{
    public class Program
    {
        // for maintain sap company connection
        // 20220426
        public static Dictionary<string, Company> SapCompanies { get; set; } = new Dictionary<string, Company>();
        public static Dictionary<string, bool> UserTransToken_CreateDLB { get; set; } = new Dictionary<string, bool>();
        public static Dictionary<string, bool> UserTransToken_PostPick { get; set; } = new Dictionary<string, bool>();
        public static Dictionary<string, bool> UserTransToken_SelectPick { get; set; } = new Dictionary<string, bool>();
        public static Dictionary<string, bool> UserTransToken_GetSOToPick { get; set; } = new Dictionary<string, bool>();
        public static Dictionary<string, bool> UserTransToken_BreadCreateInv { get; set; } = new Dictionary<string, bool>();
        public static Dictionary<string, bool> UserTransToken_BreadCreateTransfer { get; set; } = new Dictionary<string, bool>();
        public static Dictionary<string, bool> UserTransToken_BreadCreateCn { get; set; } = new Dictionary<string, bool>();

        public static void Main(string[] args)
        {
            // before seri log add in
            //CreateHostBuilder(args).Build().Run();

            // after install serilog sink to file 
            //Log.Logger = new LoggerConfiguration()
            //    .Enrich.FromLogContext()
            //    .WriteTo.Console()
            //    .CreateLogger();
            //CreateHostBuilder(args).Build().Run();

            try
            {
                Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                        .WriteTo.Debug(outputTemplate: DateTime.Now.ToString()).WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, $"{e.Message}");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            try
            {
                return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                   {
                       webBuilder.UseStartup<Startup>();
                   });
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, $"{e.Message}");
                return null;
            }
        }


        // after install serilog sink file features
        //public static IHostBuilder CreateHostBuilder(string[] args) =>
        //    Host.CreateDefaultBuilder(args)
        //    .UseSerilog()
        //        .ConfigureWebHostDefaults(webBuilder =>
        //        {
        //            webBuilder.UseStartup<Startup>();
        //        });

        // before seri log add in 
        //public static IHostBuilder CreateHostBuilder(string[] args) =>
        //    Host.CreateDefaultBuilder(args)
        //        .ConfigureWebHostDefaults(webBuilder =>
        //        {
        //            webBuilder.UseStartup<Startup>();
        //        });
    }
}
