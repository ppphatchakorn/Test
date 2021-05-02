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
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
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
            //IRestResponse lineResponse = lineClient.Execute(lineRequest);
            IRestResponse lineResponse;
            //Console.WriteLine(lineResponse.Content);

            //start

            //Initialize Collections 
            DateTime startTime = DateTime.Now;

            string instanceFile = @"xdr_instances.json";
            string customerFile = @"aegis_customers.json";

            AegisAPI._allInstances = new List<XdrInstance>();
            AegisAPI._allCustomers = new Dictionary<string, AegisCustomer>();

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

                    List<XdrIncident> incidents = AegisAPI.GetIncidents(instance, "new", 0, 50);
                    log.LogInformation("Queried [" + incidents.Count + "] from XDR Instance :" + instance.xdr_instance_name);

                    foreach (XdrIncident incident in incidents)
                    {
                        // Check if the incident contains endpoint(s) (host)
                        if (incident.endpoint_ids.Length == 0)
                        {
                            log.LogError("incident.endpoint_ids.Length == 0 " + incident.Json);
                            lineRequest.AddOrUpdateParameter("Message", "Unable to match Customer Data for Incident :" + incident.Json);
                            lineResponse = lineClient.Execute(lineRequest);
                            
                            continue;
                        }

                        // Iterate through endpoint(s)
                        List<AegisCustomer> checkDuplicateCustomer = new List<AegisCustomer>();

                        for (int i = 0; i < incident.endpoint_ids.Length; i++)
                        {
                            string endpoint_id = incident.endpoint_ids[i];
                            string host = incident.hosts[i];

                            // If unable to find
                            if (!AegisAPI._instanceEndpoints.ContainsKey(endpoint_id))
                            {
                                log.LogError("Unable to match Customer Data for Incident :" + incident.Json);
                                lineRequest.AddOrUpdateParameter("Message", "Unable to match Customer Data for Incident :" + incident.Json);
                                lineResponse = lineClient.Execute(lineRequest);

                                continue;
                            }
                            
                            log.LogInformation("Processing Incident: " + incident.incident_id + " - " + incident.description);

                            AegisCustomer customer = AegisAPI._instanceEndpoints[endpoint_id].Customer;
                            if (customer == null)
                            {
                                log.LogError("customer = null on incident:" + incident.incident_id + incident.Json + "\r\nSkipping...");
                                lineRequest.AddOrUpdateParameter("Message", "customer = null on incident:" + incident.incident_id + incident.Json + "\r\nSkipping...");
                                lineResponse = lineClient.Execute(lineRequest);
                                continue;
                            }

                            if (checkDuplicateCustomer.Contains(customer))
                                continue;

                            checkDuplicateCustomer.Add(customer);

                            //***** Has to check status. if create not success, don't update the incident status ******
                            AegisAPI.CreateIssue(customer, host, incident);

                            log.LogInformation("Updating Incident status on Cortex: " + incident.incident_id + " - under_investigation");
                            AegisAPI.UpdateIncidentStatus(instance, incident, "under_investigation");
                            
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

                lineRequest.AddOrUpdateParameter("Message", "API app encountered an error\r\n" + e.Message + "\r\n" + e.StackTrace);
                lineResponse = lineClient.Execute(lineRequest);
                return;
            }

            //Console.WriteLine(incidents.reply.incidents);


            DateTime endTime = DateTime.Now;
            TimeSpan totalTime = new TimeSpan(endTime.Ticks - startTime.Ticks);

            log.LogInformation("Total Process time: " + totalTime.TotalSeconds);

            lineRequest.AddOrUpdateParameter("Message",message + "\r\nBatch total process time: " + totalTime.TotalSeconds + "\r\nBatch total process incident: " + incidentCounter);
            lineResponse = lineClient.Execute(lineRequest);
           

            Console.WriteLine(lineResponse.Content);
           
        }
    }
}
