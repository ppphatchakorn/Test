using System;
using System.Collections.Generic;
using ChunbokAegis;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace TimerFunctionApp
{
    public static class TimerFunction
    {
        [FunctionName("TimerFunction-1")]
        public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            var message = $"Timer API app triggered on : {DateTime.Now}";
            log.LogInformation(message);

            var lineClient = new RestClient("https://notify-api.line.me/api/notify");
            
            lineClient.Timeout = -1;
            var lineRequest = new RestRequest(Method.POST);
            // CBK Line Group
            lineRequest.AddHeader("Authorization", "Bearer wlTaHnSQqnQU1HMf6jpb5HVCntyp8yY2q9IpekGOhJ1");
            // Personal Line Notify
            //lineRequest.AddHeader("Authorization", "Bearer mIo43lLJOSwDSeuBlR65TPYujBlHextkRN22ZWO9XjK");
            lineRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            lineRequest.AddOrUpdateParameter("Message", message);
            //request.AddParameter("stickerPackageId", "1");
            //request.AddParameter("stickerId", "5");
            IRestResponse lineResponse = lineClient.Execute(lineRequest);
            Console.WriteLine(lineResponse.Content);

            //start

            //Initialize Collections 
            DateTime startTime = DateTime.Now;

            string instanceFile = @"xdr_instances.json";
            string customerFile = @"aegis_customers.json";

            AegisAPI._allInstances = new List<ChunbokAegis.XdrInstance>();
            AegisAPI._allCustomers = new Dictionary<string, ChunbokAegis.AegisCustomer>();

            int instanceCounter = 0;
            int incidentCounter = 0;
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
                    

                    log.LogInformation("Processing XDR Instances [" + instanceCounter + "] : " + instance.xdr_instance_name);

                    AegisAPI.GetEndpoint(instance);

                    List<XdrIncident> incidents = AegisAPI.GetIncidents(instance, "new", 0, 10);
                    log.LogInformation("Queried [" + incidents.Count + "] from XDR Instance :" + instance.xdr_instance_name);

                    foreach (XdrIncident incident in incidents)
                    {
                        if (incident.endpoint_ids.Length != 0)
                        {
                            if (AegisAPI._instanceEndpoints.ContainsKey(incident.endpoint_ids[0]))
                            {
                                log.LogInformation("Processing Incident: " + incident.incident_id + " - " + incident.description);
                                AegisCustomer customer = AegisAPI._instanceEndpoints[incident.endpoint_ids[0]].Customer;
                                AegisAPI.CreateIssue(customer, incident);

                                log.LogInformation("Updating Incident status on Cortex: " + incident.incident_id + " - under_investigation");
                                AegisAPI.UpdateIncidentStatus(instance, incident, "under_investigation");
                            }
                            else
                            {
                                log.LogError("Unable to match Customer Data for Incident :" + incident.Json);
                            }
                        }
                        else
                        {
                            log.LogError("");
                        }

                        incidentCounter++;
                    }

                    instanceCounter++;
                    DateTime end = DateTime.Now;
                    log.LogInformation("Process time for from XDR Instance: " + instance.xdr_instance_name + " = " + new TimeSpan(end.Ticks - start.Ticks).TotalSeconds);
                }

            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                log.LogError(e.StackTrace);


                lineRequest.AddOrUpdateParameter("Message", "API app encountered an error\r\n" + e.Message);
                lineResponse = lineClient.Execute(lineRequest);
                return;
            }

            //Console.WriteLine(incidents.reply.incidents);


            DateTime endTime = DateTime.Now;
            TimeSpan totalTime = new TimeSpan(endTime.Ticks - startTime.Ticks);

            log.LogInformation("Total Process time: " + totalTime.TotalSeconds);

            lineRequest.AddOrUpdateParameter("Message","Batch total process time: " + totalTime.TotalSeconds + "\r\nBatch total process incident: " + incidentCounter);
            lineResponse = lineClient.Execute(lineRequest);
           

            Console.WriteLine(lineResponse.Content);


        }
    }
}
