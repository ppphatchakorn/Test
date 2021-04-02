using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ChunbokAegis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;

namespace FunctionApp2
{
    public static class Function2
    {
        [FunctionName("Function2")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";


            //

            //Initialize Collections 
            DateTime startTime = DateTime.Now;

            string instanceFile = @"xdr_instances.json";
            string customerFile = @"aegis_customers.json";

            AegisAPI._allInstances = new List<ChunbokAegis.XdrInstance>();
            AegisAPI._allCustomers = new Dictionary<string, ChunbokAegis.AegisCustomer>();


            int counter = 0;
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

                    AegisAPI.GetEndpoint(instance);

                    List<XdrIncident> incidents = AegisAPI.GetIncidents(instance, "new", 0, 5);
                    log.LogInformation("Queried [" + incidents.Count + "] from XDR Instance :" + instance.xdr_instance_name);

                    foreach (XdrIncident incident in incidents)
                    {
                        //log.LogInformation("Processing Incident: " + incident.incident_id + " - " + incident.description);
                        //AegisAPI.CreateIssue(incident, "dummy@gmail.com");

                        //log.LogInformation("Updating Incident status on Cortex: " + incident.incident_id + " - under_investigation");
                        //AegisAPI.UpdateIncidentStatus(instance, incident, "under_investigation");
                        counter++;
                    }

                    i++;
                    DateTime end = DateTime.Now;
                    log.LogInformation("Process time for from XDR Instance: " + instance.xdr_instance_name + " = " + new TimeSpan(end.Ticks - start.Ticks).TotalSeconds);
                }

            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                log.LogError(e.StackTrace);
                return new OkObjectResult("This HTTP triggered function executed successfully, but unable to process.");
            }

            //Console.WriteLine(incidents.reply.incidents);


            DateTime endTime = DateTime.Now;
            TimeSpan totalTime = new TimeSpan(endTime.Ticks - startTime.Ticks);

            log.LogInformation("Total Process time: " + totalTime.TotalSeconds);


            return new OkObjectResult(responseMessage);
        }
    }
}

