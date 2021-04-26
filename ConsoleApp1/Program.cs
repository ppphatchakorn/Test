using ChunbokAegis;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            ILoggerFactory loggerFactory = new LoggerFactory();
            //.AddConsole()
    //.AddDebug();
            ILogger log = loggerFactory.CreateLogger<Program>();
            log.LogDebug("Helllll yeah");
            //log.Lo("Helllll yeah");
            log.LogInformation(
              "This is a test of the emergency broadcast system.");
            //logger.log

            //Initialize Collections 
            DateTime startTime = DateTime.Now;

            string instanceFile = @"xdr_instances.json";
            string customerFile = @"aegis_customers.json";

            AegisAPI._allInstances = new List<ChunbokAegis.XdrInstance>();
            AegisAPI._allCustomers = new Dictionary<string, ChunbokAegis.AegisCustomer>();

            log.LogInformation("Reading XDR Instance list file: " + instanceFile);
            AegisAPI.ReadXdrInstanceList(instanceFile);
            log.LogInformation("totalXdrInstance: " + AegisAPI._allInstances.Count);

            return;
            try

            {
                //log.LogInformation("Reading XDR Instance list file: " + instanceFile);
                AegisAPI.ReadXdrInstanceList(instanceFile);
                //log.LogInformation("totalXdrInstance: " + AegisAPI._allInstances.Count);

                //log.LogInformation("Reading Aegis Customer list file: " + customerFile);
                AegisAPI.ReadAegisCustomerList(customerFile);
                //log.LogInformation("totalAegisCustomer: " + AegisAPI._allCustomers.Count);

                //log.LogInformation("Interating Through XDR Instances...");

                foreach (XdrInstance instance in AegisAPI._allInstances)
                {
                    DateTime start = DateTime.Now;
                    int i = 0;

                    //log.LogInformation("Processing XDR Instances [" + i + "] : " + instance.xdr_instance_name);

                    //log.LogInformation("Getting Endpoints from : " + instance.xdr_instance_name);
                    AegisAPI.GetEndpoint(instance);
                    //log.LogInformation("Total Endpoints : " + AegisAPI._instanceEndpoints.Count);

                    List<XdrIncident> incidents = AegisAPI.GetIncidents(instance, "under_investigation", 0, 100);
                    //log.LogInformation("Queried [" + incidents.Count + "] from XDR Instance :" + instance.xdr_instance_name);

                    foreach (XdrIncident incident in incidents)
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
                    //log.LogInformation("Process time for from XDR Instance: " + instance.xdr_instance_name + " = " + new TimeSpan(end.Ticks - start.Ticks).TotalSeconds);
                }

            }
            catch (Exception e)
            {
                //log.LogError(e.Message);
                //log.LogError(e.StackTrace);
                //log.LogError(e.InnerException.Message);

                //return new OkObjectResult("This HTTP triggered function executed successfully, but unable to process.");
            }

            //Console.WriteLine(incidents.reply.incidents);


            DateTime endTime = DateTime.Now;
            TimeSpan totalTime = new TimeSpan(endTime.Ticks - startTime.Ticks);

            //log.LogInformation("Total Process time: " + totalTime.TotalSeconds);

            //return new OkObjectResult(responseMessage);
        }
    }
}
