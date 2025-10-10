using Newtonsoft.Json;
using RestSharp;
using System;

namespace KTC_SalesAppWAPI.Helpers
{
    public class AppNotification
    {
        public string Error { get; set; }

        public bool SendNotification(string title,
                                    string message, string clientToken, string svrKey, string svrUrl)
        {
            try
            {



                var client = new RestClient(svrUrl);
                client.Timeout = -1;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Authorization", svrKey);
                request.AddHeader("Content-Type", "application/json");

                var dBody = new
                {
                    to = clientToken,
                    notification = new
                    {
                        body = message,
                        title = title
                    }
                };

                var jBody = JsonConvert.SerializeObject(dBody);

                request.AddParameter("application/json", jBody, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                Console.WriteLine(response.Content);

                if (response.IsSuccessful)
                {
                    return true;
                }

                Error = $"Fail reponse with content\n{response.Content} ";
                return false;
            }
            catch (Exception e)
            {
                Error = $"{e.Message}\n{e.StackTrace}";
                return false;
            }

        }
    }
}
