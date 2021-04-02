using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
using RestSharp;

using System.Collections.Generic;
using Newtonsoft.Json;
using ChunbokAegis;

namespace FunctionApp1
{
    public static class Function1
    {
        //static List<XdrInstance> _xdrInstance;
        //static Dictionary<string, AegisCustomer> _aegisCustomer;

        [FunctionName("HTTPFunction-1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            log.LogInformation("C# HTTP trigger function processed a request.");

            //Initialize Collections 
            DateTime startTime = DateTime.Now;

            string instanceFile = @"xdr_instances.json";
            string customerFile = @"aegis_customers.json";

            AegisAPI._allInstances = new List<ChunbokAegis.XdrInstance>();
            AegisAPI._allCustomers = new Dictionary<string, ChunbokAegis.AegisCustomer>();
            


            //query POST parameter (not relevant)
            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";
            //query POST parameter (not relevant)


            try

            {
                log.LogInformation("Reading XDR Instance list file: " + instanceFile);
                AegisAPI.ReadXdrInstanceList(instanceFile);
                log.LogInformation("totalXdrInstance: " + AegisAPI._allInstances.Count);
                
                log.LogInformation("Reading Aegis Customer list file: " + customerFile);
                AegisAPI.ReadAegisCustomerList(customerFile);
                log.LogInformation("totalAegisCustomer: " + AegisAPI._allCustomers.Count);

                log.LogInformation("Interating Through XDR Instances...");

                foreach (XdrInstance instance in AegisAPI._allInstances)
                {
                    DateTime start = DateTime.Now;
                    int i = 0;

                    log.LogInformation("Processing XDR Instances [" + i + "] : " + instance.xdr_instance_name);

                    log.LogInformation("Getting Endpoints from : " + instance.xdr_instance_name);
                    AegisAPI.GetEndpoint(instance);
                    log.LogInformation("Total Endpoints : " + AegisAPI._instanceEndpoints.Count);

                    List<XdrIncident> incidents = AegisAPI.GetIncidents(instance, "under_investigation", 0, 100);
                    log.LogInformation("Queried [" + incidents.Count + "] from XDR Instance :" + instance.xdr_instance_name);

                    foreach(XdrIncident incident in incidents)
                    {
                        //log.LogInformation("Processing Incident: " + incident.incident_id + " - " + incident.description);
                        //AegisAPI.CreateIssue(incident);

                        //log.LogInformation("Updating Incident status on Cortex: " + incident.incident_id + " - under_investigation");
                        AegisAPI.UpdateIncidentStatus(instance, incident, "new");
                    }

                    //Console.WriteLine(". xdrInstance.xdr_instance_name: " + xdrInstance.xdr_instance_name);
                    //Console.WriteLine(". xdrInstance.xdr_api_url: " + xdrInstance.xdr_api_url);
                    //Console.WriteLine(". xdrInstance.xdr_auth_id: " + xdrInstance.xdr_auth_id);
                    //Console.WriteLine(". xdrInstance.xdr_auth: " + xdrInstance.xdr_auth);

                    i++;
                    DateTime end = DateTime.Now;
                    log.LogInformation("Process time for from XDR Instance: " + instance.xdr_instance_name + " = " + new TimeSpan(end.Ticks - start.Ticks).TotalSeconds);
                }

            } catch (Exception e)
            {
                log.LogError(e.Message);
                log.LogError(e.StackTrace);
                //log.LogError(e.InnerException.Message);

                return new OkObjectResult("This HTTP triggered function executed successfully, but unable to process.");
            }
            
            //Console.WriteLine(incidents.reply.incidents);

            
            DateTime endTime = DateTime.Now;
            TimeSpan totalTime = new TimeSpan(endTime.Ticks - startTime.Ticks);

            log.LogInformation("Total Process time: " + totalTime.TotalSeconds);

            return new OkObjectResult(responseMessage);
        }

        //static int ReadXdrInstanceList(string filename)
        //{
        //    int i = 0;

        //        string text = File.ReadAllText(filename);
        //        dynamic data = JsonConvert.DeserializeObject(text);

        //        //Check JSON
        //        //Console.WriteLine("XDR Instance List:");
        //    //Console.WriteLine(data);

        //    foreach (var ins in data.xdr_instances)
        //    {
        //        //Console.WriteLine(ins);

        //        var xdrInstance = new XdrInstance();
        //        xdrInstance.xdr_instance_name = ins.xdr_instance_name;
        //        xdrInstance.xdr_api_url = ins.xdr_api_url;
        //        xdrInstance.xdr_auth_id = ins.xdr_auth_id;
        //        xdrInstance.xdr_auth = ins.xdr_auth;

        //        //Console.WriteLine(i + ". xdrInstance.xdr_instance_name: " + xdrInstance.xdr_instance_name);
        //        //Console.WriteLine(i + ". xdrInstance.xdr_api_url: " + xdrInstance.xdr_api_url);
        //        //Console.WriteLine(i + ". xdrInstance.xdr_auth_id: " + xdrInstance.xdr_auth_id);
        //        //Console.WriteLine(i + ". xdrInstance.xdr_auth: " + xdrInstance.xdr_auth);

        //        _xdrInstance.Add(xdrInstance);
        //        i++;

        //        //throw new Exception("Test Error int ReadXdrInstanceList()");
        //    }
            
        //    return i;
        //}

        //static List<XdrIncident> ReadIncidents(XdrInstance xdrInstance)
        //{
        //    List<XdrIncident> xdrIncidents = new List<XdrIncident>();

        //    // Processing JSON body from CortexXDR 
        //    var client = new RestClient(xdrInstance.xdr_api_url + "public_api/v1/incidents/get_incidents/");
        //    client.Timeout = -1;
        //    var request = new RestRequest(Method.POST);

        //    //request.AddHeader("Authorization", "\"" + xdrInstance.xdr_auth + "\"");
        //    //request.AddHeader("Content-Type", "application/json");


        //    // From Postman
        //    //var client = new RestClient("https://api-sknh.xdr.sg.paloaltonetworks.com/public_api/v1/incidents/get_incidents/");
        //    //client.Timeout = -1;
        //    //var request = new RestRequest(Method.POST);
        //    //request.AddHeader("x-xdr-auth-id", "2");
        //    request.AddHeader("x-xdr-auth-id", xdrInstance.xdr_auth_id.ToString());
        //    //request.AddHeader("Authorization", "Q7XiEKEDDUMv0jVN9B9YAg83FYPWHcVM8j6v5uwukjq1wIHfcmw8Yud6UmqWNkGEsR8rqewhcPm38bftwC87JNQApOu2PxcLuMw5iah1Cznbn5jRTKYK1HfCcjgGHtVn");
        //    request.AddHeader("Authorization", xdrInstance.xdr_auth);
        //    request.AddHeader("Content-Type", "application/json");
        //    //request.AddHeader("Cookie", "XSRF-TOKEN=Q7XiEKEDDUMv0jVN9B9YAg83FYPWHcVM8j6v5uwukjq1wIHfcmw8Yud6UmqWNkGEsR8rqewhcPm38bftwC87JNQApOu2PxcLuMw5iah1Cznbn5jRTKYK1HfCcjgGHtVn");
        //    request.AddParameter("application/json", "{\r\n    \"request_data\": {\r\n        \"filters\": [\r\n            {\r\n                \"field\": \"status\",\r\n                \"operator\": \"eq\",\r\n                \"value\": \"new\"\r\n            }\r\n        ],\r\n        \"search_from\": 0,\r\n        \"search_to\": 100,\r\n        \"sort\": {\r\n            \"field\": \"creation_time\",\r\n            \"keyword\": \"asc\"\r\n        }\r\n    }\r\n}", ParameterType.RequestBody);
        //    IRestResponse response = client.Execute(request);
        //    //Console.WriteLine(response.Content);
        //    Console.WriteLine("Incidents JSON:" + response.Content);
        //    // End from Postman




        //    dynamic incidents = JsonConvert.DeserializeObject(response.Content);

        //    int i = 0;
        //    foreach (var inc in incidents.reply.incidents)
        //    {
        //        //Console.WriteLine("Incident[" + i + "] JSON:");
        //        //Console.WriteLine(inc);

        //        XdrIncident incident = new XdrIncident();
        //        xdrIncidents.Add(incident);

        //        var incident_id = inc.incident_id;
        //        var incident_name = inc.incident_name;
        //        var creation_time = inc.creation_time;
        //        var modification_time = inc.modification_time;
        //        var detection_time = inc.detection_time;
        //        var status = inc.status;
        //        var severity = inc.severity;
        //        var description = inc.description;
        //        var assigned_user_mail = inc.assigned_user_mail;
        //        var assigned_user_pretty_name = inc.assigned_user_pretty_name;
        //        var alert_count = inc.alert_count;
        //        var low_severity_alert_count = inc.low_severity_alert_count;
        //        var med_severity_alert_count = inc.med_severity_alert_count;
        //        var high_severity_alert_count = inc.high_severity_alert_count;
        //        var user_count = inc.user_count;
        //        var host_count = inc.host_count;
        //        var notes = inc.notes;
        //        var resolve_comment = inc.resolve_comment;
        //        var manual_severity = inc.manual_severity;
        //        var manual_description = inc.manual_description;
        //        var xdr_url = inc.xdr_url;
        //        var starred = inc.starred;
        //        var hosts = inc.hosts;
        //        var users = inc.users;
        //        var incident_sources = inc.incident_sources;
        //        var rule_based_score = inc.rule_based_score;
        //        var manual_score = inc.manual_score;


        //        //    //Console.WriteLine("incident_id: " + incident_id);

        //        //    //

        //        //    // Issue Creation

        //        //    ////var JsmClient = new RestClient("https://dataexpress.atlassian.net/rest/api/3/issue");
        //        //    //var JsmClient = new RestClient("https://varakorncbk.atlassian.net/rest/api/3/issue");
        //        //    //JsmClient.Timeout = -1;
        //        //    //var JsmRequest = new RestRequest(Method.POST);
        //        //    //JsmRequest.AddHeader("Authorization", "Basic cGF3YXJhdEBkYXRhZXhwcmVzcy5jby50aDpvVEl5OHJqMERyVWVSVTdlbEQ0VUVENUM=");
        //        //    //JsmRequest.AddHeader("Content-Type", "application/json");
        //        //    ///*
        //        //    //JsmRequest.AddParameter(
        //        //    //        "application/json", "{ \r\n  \"update\": {},\r\n  \"fields\": {\r\n    \"summary\": \"" + description + "\",\r\n    \"issuetype\": {\r\n      \"id\": \"10019\"\r\n    },\r\n    \"project\": {\r\n      \"id\": \"10022\"\r\n    },\r\n    \"description\": {\r\n      \"type\": \"doc\",\r\n      \"version\": 1,\r\n      \"content\": [\r\n        {\r\n          \"type\": \"paragraph\",\r\n          \"content\": [\r\n            {\r\n              \"text\": \"Order entry fails when selecting supplier.\",\r\n              \"type\": \"text\"\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    },\r\n    \"reporter\": {\r\n      \"id\": \"557058:fef45c9c-3595-4b8d-b3c1-37e3eed4cdd1\"\r\n    }\r\n  }\r\n}", ParameterType.RequestBody);
        //        //    //        */
        //        //    //string i_text = incident.ToString();
        //        //    //JsmRequest.AddParameter("application/json", "{ \r\n  \"update\": {},\r\n  \"fields\": {\r\n    \"summary\": \"" + description + "\",\r\n    \"issuetype\": {\r\n      \"id\": \"10009\"\r\n    },\r\n    \"project\": {\r\n      \"id\": \"10000\"\r\n    },\r\n    \"description\": {\r\n      \"type\": \"doc\",\r\n      \"version\": 1,\r\n      \"content\": [\r\n        {\r\n          \"type\": \"paragraph\",\r\n          \"content\": [\r\n            {\r\n              \"text\": \"" + "yyyyyy" + "\",\r\n              \"type\": \"text\"\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    }\r\n  }\r\n}", ParameterType.RequestBody);
        //        //    //IRestResponse JsmResponse = JsmClient.Execute(JsmRequest);
        //        //    //Console.WriteLine(JsmResponse.Content);

        //        //    //dynamic jsmJson = JsonConvert.DeserializeObject(JsmResponse.Content);
        //        //    //Console.WriteLine(jsmJson);

        //        //    // Update Cortex Status

        //        i++;
        //    }

        //    return xdrIncidents;
        //}

        //public static void UpdateIncidentStatus(string incident_id, string newStatus)
        //{
        //    var client = new RestClient("https://api-sknh.xdr.sg.paloaltonetworks.com/public_api/v1/incidents/update_incident");
        //    client.Timeout = -1;
        //    var request = new RestRequest(Method.POST);
        //    request.AddHeader("x-xdr-auth-id", "3");
        //    request.AddHeader("Authorization", "WvMRwflDwTiJCv10063fIKSIyunL6I7j1QMp3umZhLy4uUNnb31R06Ia0X8EI0TQF6QlzDGBqXv0XGGSZJTs7SeTvwAKBub0JZRKU7VjulkRAD4yR8tzCiu7D2hVcQxR");
        //    request.AddHeader("Content-Type", "application/json");
        //    request.AddHeader("Cookie", "app-proxy-prod-sg=ed0f459ed29cdae8665108d5b3f1f4bb4ef4f8cda515fd24d8b5bbd23031b095; XSRF-TOKEN=WvMRwflDwTiJCv10063fIKSIyunL6I7j1QMp3umZhLy4uUNnb31R06Ia0X8EI0TQF6QlzDGBqXv0XGGSZJTs7SeTvwAKBub0JZRKU7VjulkRAD4yR8tzCiu7D2hVcQxR");
        //    request.AddParameter("application/json", "{ \r\n   \"request_data\":{ \r\n      \"incident_id\":\"" + incident_id + "\",\r\n      \"update_data\":{ \r\n         \"status\":\"under_investigation\"\r\n\r\n      }\r\n   }\r\n}", ParameterType.RequestBody);
        //    IRestResponse response = client.Execute(request);
        //    Console.WriteLine(response.Content);

        //    return;
        //}

        //public static Dictionary<string, string> _endPoint;
        //public static string GetEndpoint()
        //{
        //    var client = new RestClient("https://api-sknh.xdr.sg.paloaltonetworks.com/public_api/v1/endpoints/get_endpoint");
        //    client.Timeout = -1;
        //    var request = new RestRequest(Method.POST);
        //    request.AddHeader("x-xdr-auth-id", "3");
        //    request.AddHeader("Authorization", "WvMRwflDwTiJCv10063fIKSIyunL6I7j1QMp3umZhLy4uUNnb31R06Ia0X8EI0TQF6QlzDGBqXv0XGGSZJTs7SeTvwAKBub0JZRKU7VjulkRAD4yR8tzCiu7D2hVcQxR");
        //    request.AddHeader("Content-Type", "application/json");
        //    request.AddHeader("Cookie", "app-proxy-prod-sg=ed0f459ed29cdae8665108d5b3f1f4bb4ef4f8cda515fd24d8b5bbd23031b095; XSRF-TOKEN=WvMRwflDwTiJCv10063fIKSIyunL6I7j1QMp3umZhLy4uUNnb31R06Ia0X8EI0TQF6QlzDGBqXv0XGGSZJTs7SeTvwAKBub0JZRKU7VjulkRAD4yR8tzCiu7D2hVcQxR; csrf_token=30066098ae9941c1b733e20f6b3ac128");
        //    request.AddParameter("application/json", "{ \r\n   \"request_data\":{}\r\n}", ParameterType.RequestBody);
        //    IRestResponse response = client.Execute(request);

        //    _endPoint = new Dictionary<string, string>();

        //    //Console.WriteLine(response.Content);

        //    return null;
        //}

        //public static void CreateIssue(XdrIncident incident)
        //{
        //    var client = new RestClient("https://dataexpress.atlassian.net/rest/api/3/issue");
        //    client.Timeout = -1;
        //    var request = new RestRequest(Method.POST);
        //    request.AddHeader("Authorization", "Basic cGF3YXJhdEBkYXRhZXhwcmVzcy5jby50aDpvVEl5OHJqMERyVWVSVTdlbEQ0VUVENUM=");
        //    request.AddHeader("Content-Type", "application/json");
        //    request.AddParameter(
        //        "application/json", "{ \r\n  \"update\": {},\r\n  \"fields\": {\r\n    \"summary\": \"" + incident.description + "\",\r\n    \"issuetype\": {\r\n      \"id\": \"10019\"\r\n    },\r\n    \"project\": {\r\n      \"id\": \"10022\"\r\n    },\r\n    \"description\": {\r\n      \"type\": \"doc\",\r\n      \"version\": 1,\r\n      \"content\": [\r\n        {\r\n          \"type\": \"paragraph\",\r\n          \"content\": [\r\n            {\r\n              \"text\": \"Order entry fails when selecting supplier.\",\r\n              \"type\": \"text\"\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    },\r\n    \"reporter\": {\r\n      \"id\": \"557058:fef45c9c-3595-4b8d-b3c1-37e3eed4cdd1\"\r\n    }\r\n  }\r\n}", ParameterType.RequestBody);
        //    IRestResponse response = client.Execute(request);
        //    //Console.WriteLine(response.Content);
        //    return;
        //}

        //public static int ReadAegisCustomerList(string filename)
        //{
        //    int i = 0;
        //    string text = File.ReadAllText(filename);
        //    dynamic data = JsonConvert.DeserializeObject(text);

        //    //Check JSON
        //    //Console.WriteLine("Customer List:");
        //    //Console.WriteLine(data);

        //    foreach(var c in data.customers)
        //    {
        //        //Console.WriteLine(c);

        //        var customer = new AegisCustomer();
        //        customer.customer_name = c.customer_name;
        //        customer.xdr_group_name = c.xdr_group_name;
        //        customer.jsm_url = c.jsm_url;
        //        customer.jsm_project_id = c.jsm_project_id;
        //        customer.jsm_issuetype_id = c.jsm_issuetype_id;
        //        customer.jsm_reporter_email = c.jsm_reporter_email;

        //        //Console.WriteLine(i + "." + customer.customer_name);
        //        //Console.WriteLine(i + "." + customer.xdr_group_name);
        //        //Console.WriteLine(i + "." + customer.jsm_url);
        //        //Console.WriteLine(i + "." + customer.jsm_project_id);
        //        //Console.WriteLine(i + "." + customer.jsm_issuetype_id);
        //        //Console.WriteLine(i + "." + customer.jsm_reporter_email);

        //        _aegisCustomer.Add(customer.xdr_group_name, customer);
        //        i++;

        //        //throw new Exception("Test Error int ReadAegisCustomerList()");
        //    }

        //    return i;
        //}
    }
}

