using ChunbokAegis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
//using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace CleanerFunctionApp
{
    public static class CleanerFunction
    {
        //static List<XdrInstance> _xdrInstance;
        //static Dictionary<string, AegisCustomer> _aegisCustomer;

        [FunctionName("HTTPFunction-Cleaner")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string revision = "rev-4_" + DateTime.Now;

            log.LogInformation("C# HTTP trigger function processed a request.");

            //Initialize Collections 
            DateTime startTime = DateTime.Now;

            
            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var rootDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));

            log.LogInformation("binDirectory = " + binDirectory);
            log.LogInformation("rootDirectory =  " + rootDirectory);

            string instanceFile = rootDirectory + @"\xdr_instances.json";
            string customerFile = rootDirectory + @"\aegis_customers.json";

            AegisAPI.SetLogger(log);
            AegisAPI._allInstances = new List<ChunbokAegis.XdrInstance>();
            AegisAPI._allCustomers = new Dictionary<string, ChunbokAegis.AegisCustomer>();

            //query POST parameter (not relevant)
            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully." + revision;
            //query POST parameter (not relevant)

            try
            {
                log.LogInformation("Reading XDR Instance list file: " + instanceFile);
                AegisAPI.ReadXdrInstanceList(instanceFile);
                //log.LogInformation("totalXdrInstance: " + AegisAPI._allInstances.Count);

                log.LogInformation("Reading Aegis Customer list file: " + customerFile);
                AegisAPI.ReadAegisCustomerList(customerFile);
                //log.LogInformation("totalAegisCustomer: " + AegisAPI._allCustomers.Count);

                //log.LogInformation("Interating Through XDR Instances...");

                foreach (XdrInstance instance in AegisAPI._allInstances)
                {
                    DateTime start = DateTime.Now;
                    int i = 0;

                    log.LogInformation("Getting Endpoints from : [" + i + "] " + instance.xdr_instance_name);
                    AegisAPI.GetEndpoint(instance);
                    //log.LogInformation("Total Endpoints : " + AegisAPI._instanceEndpoints.Count);

                    List<XdrIncident> incidents = AegisAPI.GetIncidents(instance, "under_investigation", 0, 100);
                    log.LogInformation("Queried [" + incidents.Count + "] from XDR Instance :" + instance.xdr_instance_name);

                    foreach (XdrIncident incident in incidents)
                    {
                        //log.LogInformation("Processing Incident: " + incident.incident_id + " - " + incident.description);
                        //AegisAPI.CreateIssue(incident);

                        //log.LogInformation("Updating Incident status on Cortex: " + incident.incident_id + " - under_investigation");
                        //AegisAPI.UpdateIncidentStatus(instance, incident, "new");
                    }

                    //Console.WriteLine(". xdrInstance.xdr_instance_name: " + xdrInstance.xdr_instance_name);
                    //Console.WriteLine(". xdrInstance.xdr_api_url: " + xdrInstance.xdr_api_url);
                    //Console.WriteLine(". xdrInstance.xdr_auth_id: " + xdrInstance.xdr_auth_id);
                    //Console.WriteLine(". xdrInstance.xdr_auth: " + xdrInstance.xdr_auth);

                    i++;
                    DateTime end = DateTime.Now;
                    log.LogInformation("Process time for from XDR Instance: " + instance.xdr_instance_name + " = " + new TimeSpan(end.Ticks - start.Ticks).TotalSeconds);
                }

            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                log.LogError(e.StackTrace);
                //log.LogError(e.InnerException.Message);

                return new OkObjectResult("This HTTP triggered function executed successfully, but unable to process." + revision);
            }

            //Console.WriteLine(incidents.reply.incidents);


            DateTime endTime = DateTime.Now;
            TimeSpan totalTime = new TimeSpan(endTime.Ticks - startTime.Ticks);

            log.LogInformation("Total Process time: " + totalTime.TotalSeconds);

            return new OkObjectResult(responseMessage);
        }
    }
}
