using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Json.Net;
using System.Collections.Generic;

namespace FunctionApp1
{
    public static class Function1
    {
        public static List<CortexInstance> _cortexInstances;

        [FunctionName("HTTPFunction-1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            _cortexInstances = new List<CortexInstance>();
            
            //Instance 
            _cortexInstances.Add(new CortexInstance() { 
                SiteURL = "https://api-sknh.xdr.sg.paloaltonetworks.com/", 
                XdrAuthId = "3", 
                ApiKey = "WvMRwflDwTiJCv10063fIKSIyunL6I7j1QMp3umZhLy4uUNnb31R06Ia0X8EI0TQF6QlzDGBqXv0XGGSZJTs7SeTvwAKBub0JZRKU7VjulkRAD4yR8tzCiu7D2hVcQxR"
            });

            DateTime startTime = DateTime.Now;

            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            var client = new RestClient("https://api-sknh.xdr.sg.paloaltonetworks.com/public_api/v1/incidents/get_incidents/");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("x-xdr-auth-id", "3");
            request.AddHeader("Authorization", "WvMRwflDwTiJCv10063fIKSIyunL6I7j1QMp3umZhLy4uUNnb31R06Ia0X8EI0TQF6QlzDGBqXv0XGGSZJTs7SeTvwAKBub0JZRKU7VjulkRAD4yR8tzCiu7D2hVcQxR");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Cookie", "app-proxy-prod-sg=ed0f459ed29cdae8665108d5b3f1f4bb4ef4f8cda515fd24d8b5bbd23031b095; XSRF-TOKEN=WvMRwflDwTiJCv10063fIKSIyunL6I7j1QMp3umZhLy4uUNnb31R06Ia0X8EI0TQF6QlzDGBqXv0XGGSZJTs7SeTvwAKBub0JZRKU7VjulkRAD4yR8tzCiu7D2hVcQxR");
            request.AddParameter("application/json",
                "{\r\n" +
                "\"request_data\": {\r\n" +
                "        \"filters\": [\r\n" +
                "            {\r\n" +
                "                \"field\": \"status\",\r\n" +
                "                \"operator\": \"eq\",\r\n" + "" +
                "                \"value\": \"new\"\r\n" +
                "            }\r\n" +
                "        ],\r\n" +
                "        \"search_from\": 0,\r\n" +
                "        \"search_to\": 100,\r\n" +
                "        \"sort\": {\r\n" +
                "            \"field\": \"creation_time\",\r\n" +
                "            \"keyword\": \"desc\"\r\n" +
                "        }\r\n    }\r\n}"
                , ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);

            //Console.WriteLine("Incident JSON:" + response.Content);

            dynamic incidents = JsonConvert.DeserializeObject(response.Content);

            //Console.WriteLine(incidents.reply.incidents);

            foreach(var i in incidents.reply.incidents)
            {
                Console.WriteLine("Incidents: ");
                Console.WriteLine(i);

                var incident_id = i.incident_id;
                var incident_name = i.incident_name;
                var creation_time = i.creation_time;
                var modification_time = i.modification_time;
                var detection_time = i.detection_time;
                var status = i.status;
                var severity = i.severity;
                var description = i.description;
                var assigned_user_mail = i.assigned_user_mail;
                var assigned_user_pretty_name = i.assigned_user_pretty_name;
                var alert_count = i.alert_count;
                var low_severity_alert_count = i.low_severity_alert_count;
                var med_severity_alert_count = i.med_severity_alert_count;
                var high_severity_alert_count = i.high_severity_alert_count;
                var user_count = i.user_count;
                var host_count = i.host_count;
                var notes = i.notes;
                var resolve_comment = i.resolve_comment;
                var manual_severity = i.manual_severity;
                var manual_description = i.manual_description;
                var xdr_url = i.xdr_url;
                var starred = i.starred;
                var hosts = i.hosts;
                var users = i.users;
                var incident_sources = i.incident_sources;
                var rule_based_score = i.rule_based_score;
                var manual_score = i.manual_score;


                //Console.WriteLine("incident_id: " + incident_id);

                //

                // Issue Creation

                //var JsmClient = new RestClient("https://dataexpress.atlassian.net/rest/api/3/issue");
                var JsmClient = new RestClient("https://varakorncbk.atlassian.net/rest/api/3/issue");
                JsmClient.Timeout = -1;
                var JsmRequest = new RestRequest(Method.POST);
                JsmRequest.AddHeader("Authorization", "Basic cGF3YXJhdEBkYXRhZXhwcmVzcy5jby50aDpvVEl5OHJqMERyVWVSVTdlbEQ0VUVENUM=");
                JsmRequest.AddHeader("Content-Type", "application/json");
                /*
                JsmRequest.AddParameter(
                        "application/json", "{ \r\n  \"update\": {},\r\n  \"fields\": {\r\n    \"summary\": \"" + description + "\",\r\n    \"issuetype\": {\r\n      \"id\": \"10019\"\r\n    },\r\n    \"project\": {\r\n      \"id\": \"10022\"\r\n    },\r\n    \"description\": {\r\n      \"type\": \"doc\",\r\n      \"version\": 1,\r\n      \"content\": [\r\n        {\r\n          \"type\": \"paragraph\",\r\n          \"content\": [\r\n            {\r\n              \"text\": \"Order entry fails when selecting supplier.\",\r\n              \"type\": \"text\"\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    },\r\n    \"reporter\": {\r\n      \"id\": \"557058:fef45c9c-3595-4b8d-b3c1-37e3eed4cdd1\"\r\n    }\r\n  }\r\n}", ParameterType.RequestBody);
                        */
                string i_text = i.ToString();
                JsmRequest.AddParameter("application/json", "{ \r\n  \"update\": {},\r\n  \"fields\": {\r\n    \"summary\": \"" + description + "\",\r\n    \"issuetype\": {\r\n      \"id\": \"10009\"\r\n    },\r\n    \"project\": {\r\n      \"id\": \"10000\"\r\n    },\r\n    \"description\": {\r\n      \"type\": \"doc\",\r\n      \"version\": 1,\r\n      \"content\": [\r\n        {\r\n          \"type\": \"paragraph\",\r\n          \"content\": [\r\n            {\r\n              \"text\": \"" + "yyyyyy" +  "\",\r\n              \"type\": \"text\"\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    }\r\n  }\r\n}", ParameterType.RequestBody);
                IRestResponse JsmResponse = JsmClient.Execute(JsmRequest);
                Console.WriteLine(JsmResponse.Content);

                dynamic jsmJson = JsonConvert.DeserializeObject(JsmResponse.Content);
                Console.WriteLine(jsmJson);

                // Update Cortex Status


            }

            DateTime endTime = DateTime.Now;
            
            Console.WriteLine("Process time: ");

            return new OkObjectResult(responseMessage);
        }

        public static void UpdateIncidentStatus(string incident_id, string newStatus)
        {
            var client = new RestClient("https://api-sknh.xdr.sg.paloaltonetworks.com/public_api/v1/incidents/update_incident");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("x-xdr-auth-id", "3");
            request.AddHeader("Authorization", "WvMRwflDwTiJCv10063fIKSIyunL6I7j1QMp3umZhLy4uUNnb31R06Ia0X8EI0TQF6QlzDGBqXv0XGGSZJTs7SeTvwAKBub0JZRKU7VjulkRAD4yR8tzCiu7D2hVcQxR");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Cookie", "app-proxy-prod-sg=ed0f459ed29cdae8665108d5b3f1f4bb4ef4f8cda515fd24d8b5bbd23031b095; XSRF-TOKEN=WvMRwflDwTiJCv10063fIKSIyunL6I7j1QMp3umZhLy4uUNnb31R06Ia0X8EI0TQF6QlzDGBqXv0XGGSZJTs7SeTvwAKBub0JZRKU7VjulkRAD4yR8tzCiu7D2hVcQxR");
            request.AddParameter("application/json", "{ \r\n   \"request_data\":{ \r\n      \"incident_id\":\"" + incident_id + "\",\r\n      \"update_data\":{ \r\n         \"status\":\"under_investigation\"\r\n\r\n      }\r\n   }\r\n}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            Console.WriteLine(response.Content);

            return;
        }

        public static Dictionary<string, string> _endPoint;
        public static string GetEndpoint()
        {
            var client = new RestClient("https://api-sknh.xdr.sg.paloaltonetworks.com/public_api/v1/endpoints/get_endpoint");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("x-xdr-auth-id", "3");
            request.AddHeader("Authorization", "WvMRwflDwTiJCv10063fIKSIyunL6I7j1QMp3umZhLy4uUNnb31R06Ia0X8EI0TQF6QlzDGBqXv0XGGSZJTs7SeTvwAKBub0JZRKU7VjulkRAD4yR8tzCiu7D2hVcQxR");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Cookie", "app-proxy-prod-sg=ed0f459ed29cdae8665108d5b3f1f4bb4ef4f8cda515fd24d8b5bbd23031b095; XSRF-TOKEN=WvMRwflDwTiJCv10063fIKSIyunL6I7j1QMp3umZhLy4uUNnb31R06Ia0X8EI0TQF6QlzDGBqXv0XGGSZJTs7SeTvwAKBub0JZRKU7VjulkRAD4yR8tzCiu7D2hVcQxR; csrf_token=30066098ae9941c1b733e20f6b3ac128");
            request.AddParameter("application/json", "{ \r\n   \"request_data\":{}\r\n}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);

            _endPoint = new Dictionary<string, string>();

            //Console.WriteLine(response.Content);

            return null;
        }

        public static void CreateIssue(string description)
        {
            var client = new RestClient("https://dataexpress.atlassian.net/rest/api/3/issue");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", "Basic cGF3YXJhdEBkYXRhZXhwcmVzcy5jby50aDpvVEl5OHJqMERyVWVSVTdlbEQ0VUVENUM=");
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter(
                "application/json", "{ \r\n  \"update\": {},\r\n  \"fields\": {\r\n    \"summary\": \"" + description + "\",\r\n    \"issuetype\": {\r\n      \"id\": \"10019\"\r\n    },\r\n    \"project\": {\r\n      \"id\": \"10022\"\r\n    },\r\n    \"description\": {\r\n      \"type\": \"doc\",\r\n      \"version\": 1,\r\n      \"content\": [\r\n        {\r\n          \"type\": \"paragraph\",\r\n          \"content\": [\r\n            {\r\n              \"text\": \"Order entry fails when selecting supplier.\",\r\n              \"type\": \"text\"\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    },\r\n    \"reporter\": {\r\n      \"id\": \"557058:fef45c9c-3595-4b8d-b3c1-37e3eed4cdd1\"\r\n    }\r\n  }\r\n}", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            //Console.WriteLine(response.Content);
            return;
        }
    }
}

