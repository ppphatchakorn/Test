using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace TimerFunctionApp
{
    public static class TimerFunction
    {
        [FunctionName("TimerFunction-1")]
        public static void Run([TimerTrigger("* */5 * * *")]TimerInfo myTimer, ILogger log)
        {
            var message = $"C# Timer trigger function executed at: {DateTime.Now}";
            log.LogInformation(message);

            var client = new RestClient("https://notify-api.line.me/api/notify");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Bearer wlTaHnSQqnQU1HMf6jpb5HVCntyp8yY2q9IpekGOhJ1");
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("Message", message);
            //request.AddParameter("stickerPackageId", "1");
            //request.AddParameter("stickerId", "5");
            IRestResponse response = client.Execute(request);
            Console.WriteLine(response.Content);

        }
    }
}
