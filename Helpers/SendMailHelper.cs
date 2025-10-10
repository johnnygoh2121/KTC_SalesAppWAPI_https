using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using KTC_SalesAppWAPI.Controllers;
using Microsoft.Extensions.Logging;

namespace KTC_SalesAppWAPI.Helpers
{
    public class SendMailHelper
    {
        public string LasteErrorMessage { get; set; }

        public ILogger<WhsReturnController> Log;

        public SendMailHelper(ILogger<WhsReturnController> log) => Log = log;

        public void CreateMessageWithAttachment(string host, string from, string fromPw, string[] to, string subject,
                string body, string filePath, string serverPort, bool isTest = false)
        {
            try
            {
                if (to == null)
                {
                    LasteErrorMessage = "To address empty, report sent skipped";
                    Log.LogError(LasteErrorMessage);
                    return;
                }
                if (to.Length == 0)
                {
                    LasteErrorMessage = "To address empty, report sent skipped";
                    Log.LogError(LasteErrorMessage);
                    return;
                }

                var msg = new MailMessage();
                for (int addr = 0; addr < to.Length; addr++)
                {
                    var receiverName = GetName(to[addr]);
                    msg.Bcc.Add(new MailAddress(to[addr], receiverName));
                }

                //msg.To.Add(new MailAddress(to[to.Length - 1], GetName (to[to.Length - 1])));
                msg.From = new MailAddress(from, GetName(from));
                msg.Subject = (isTest) ? "[DiCn Test - Please ignore this email] " + subject : subject;
                msg.Body = body;
                msg.IsBodyHtml = false;

                var client = new SmtpClient();
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(from, fromPw);
                client.Port = int.Parse(serverPort); // You can use Port 25 if 587 is blocked (mine is!)
                client.Host = host;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.EnableSsl = true;

                Attachment data = null;
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    data = new Attachment(filePath, MediaTypeNames.Application.Octet);
                    var disposition = data.ContentDisposition;
                    disposition.CreationDate = File.GetCreationTime(filePath);
                    disposition.ModificationDate = File.GetLastWriteTime(filePath);
                    disposition.ReadDate = File.GetLastAccessTime(filePath);
                    msg.Attachments.Add(data);
                }

                try
                {
                    client.Send(msg);
                    data?.Dispose();
                }
                catch (Exception ex)
                {
                    Log.LogError($"{ex.Message}/n{ex.StackTrace}");
                }
            }
            catch (Exception e)
            {   
                Log.LogError($"{e.Message}/n{e.StackTrace}");
            }
        }

        string GetName(string email)
        {
            var addr = new MailAddress(email);
            return addr.User;
        }

    }
}
