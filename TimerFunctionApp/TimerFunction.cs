using ChunbokAegis;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace TimerFunctionApp
{
    public static class TimerFunction
    {
        [FunctionName("TimerFunction-AegisAPI")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            var message = $"Timer API app triggered on : {DateTime.Now}";
            log.LogInformation(message);

            var lineClient = new RestClient("https://notify-api.line.me/api/notify");
            
            lineClient.Timeout = 5000;
            var lineRequest = new RestRequest(Method.POST);
            // CBK Line Group
            lineRequest.AddHeader("Authorization", "Bearer wlTaHnSQqnQU1HMf6jpb5HVCntyp8yY2q9IpekGOhJ1");
            // Personal Line Notify
            //lineRequest.AddHeader("Authorization", "Bearer mIo43lLJOSwDSeuBlR65TPYujBlHextkRN22ZWO9XjK");
            lineRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            lineRequest.AddOrUpdateParameter("Message", message);
            //IRestResponse lineResponse = lineClient.Execute(lineRequest);
            IRestResponse lineResponse;

            //Start function
            //Initialize Collections 
            DateTime startTime = DateTime.Now;

            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var rootDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));

            log.LogInformation("binDirectory = " + binDirectory);
            log.LogInformation("rootDirectory =  " + rootDirectory);

            string instanceFile = rootDirectory + @"\aegis_xdr_instances-test.json";
            string customerFile = rootDirectory + @"\aegis_customers-test.json";

            AegisAPI._allInstances = new List<XdrInstance>();
            AegisAPI._allCustomers = new Dictionary<string, AegisCustomer>();

            int instanceCounter = 0;
            int incidentCounter = 0;

            try
            {
                log.LogInformation("\r\n***\r\n Reading XDR Instance list file: " + instanceFile);
                AegisAPI.ReadXdrInstanceList(instanceFile);

                log.LogInformation("\r\n***\r\n Reading Aegis Customer list file: " + customerFile);
                AegisAPI.ReadAegisCustomerList(customerFile);

                //Incident status "under_investigation" OR "new"
                string currentStatus = "new";
                string newStatus = "under_investigation";

                log.LogInformation("Interating Through XDR Instances...");

                foreach (XdrInstance instance in AegisAPI._allInstances)
                {
                    DateTime start = DateTime.Now;
                    
                    log.LogInformation("Processing XDR Instances [" + instanceCounter + "] : " + instance.xdr_instance_name);
                    log.LogInformation("\r\n***\r\n*** Getting Endpoints from: " + instance.xdr_instance_name);
                    AegisAPI.GetEndpoint(instance);
                    //log.LogInformation("Total Endpoints: " + AegisAPI._instanceEndpoints.Count);

                    log.LogInformation("\r\n***\r\n*** Getting Incidents from: " + instance.xdr_instance_name + " with status = \"" + currentStatus + "\"");
                    List<XdrIncident> incidents = AegisAPI.GetIncidents(instance, currentStatus, 0, 50);
                    log.LogInformation("Queried [" + incidents.Count + "] from XDR Instance :" + instance.xdr_instance_name);

                    foreach (XdrIncident incident in incidents)
                    {
                        ///////******
                        // Start Processing Incident
                        log.LogInformation("\r\n***\r\n*** Processing Incident: " + incident.incident_id + " - " + incident.description);

                        // Check if an Incident contains Endpoint(s) (host), If not, skip this Incident
                        if (incident.endpoint_ids.Length == 0)
                        {
                            log.LogError("Error - incident.endpoint_ids.Length == 0 " + incident.Json);
                            //lineRequest.AddOrUpdateParameter("Message", "Unable to match Customer Data for Incident :" + incident.Json);
                            //lineResponse = lineClient.Execute(lineRequest);

                            continue;
                        }

                        // Rule for creating Request
                        // 1) Create Jira Request for each Incident
                        // 2) If the Incident is from multiple Endpoints, determine its Customer and create a Request to their Jira Project accordingly
                        // 3) If the Incident contains multiple Endpoints from the same Project, create only a single Request per Jira Project

                        // A List to temporarily store Customers whose Project has a Request created
                        List<AegisCustomer> duplicatedCustomers = new List<AegisCustomer>();

                        // Iterate through endpoint(s)
                        for (int i = 0; i < incident.endpoint_ids.Length; i++)
                        {
                            string endpoint_id = incident.endpoint_ids[i];
                            string host = incident.hosts[i];

                            // Skip if unable to find the Customer for Endpoint
                            if (!AegisAPI._instanceEndpoints.ContainsKey(endpoint_id))
                            {
                                log.LogError("ERROR - Unable to match Customer Data for Incident :" + incident.Json);
                                lineRequest.AddOrUpdateParameter("Message", "Unable to match Customer Data for Incident :" + incident.Json);
                                lineResponse = lineClient.Execute(lineRequest);
                                continue;
                            }

                            AegisCustomer customer = AegisAPI._instanceEndpoints[endpoint_id].Customer;
                            if (customer == null)
                            {
                                log.LogError("ERROR - Customer = null on Incident:" + incident.incident_id + incident.Json + "\r\nSkipping...");
                                lineRequest.AddOrUpdateParameter("Message", "customer = null on incident:" + incident.incident_id + incident.Json + "\r\nSkipping...");
                                lineResponse = lineClient.Execute(lineRequest);
                                continue;
                            }

                            if (duplicatedCustomers.Contains(customer))
                            {
                                log.LogInformation("\r\n***\r\n*** Same Request for: " + incident.incident_id + " - " + incident.description + ". Is already created on \"" + customer.customer_name + "\"");
                                continue;
                            }

                            duplicatedCustomers.Add(customer);

                            //***** Has to check status. if create not success, don't update the incident status ******
                            log.LogInformation("\r\n***\r\n*** Create Request on Jira: " + incident.incident_id + " - " + incident.description + " to Jira \"" + customer.customer_name + "\"");
                            AegisAPI.CreateRequest(customer, host, incident);

                            log.LogInformation("\r\n***\r\n*** Update Incidents status on Cortex: " + incident.incident_id + " to \"" + newStatus + "\"");
                            AegisAPI.UpdateIncidentStatus(instance, incident, newStatus);
                        }

                        incidentCounter++;
                        ///////******
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

            DateTime endTime = DateTime.Now;
            TimeSpan totalTime = new TimeSpan(endTime.Ticks - startTime.Ticks);

            log.LogInformation("TimerFunction-AegisAPI run successfully. Total Process time: " + totalTime.TotalSeconds);
            lineRequest.AddOrUpdateParameter("Message",message + "\r\nTimerFunction-AegisAPI run successfully. Total Process time: " + totalTime.TotalSeconds + "\r\nBatch total process incident: " + incidentCounter);
            lineResponse = lineClient.Execute(lineRequest);
           
            //Console.WriteLine(lineResponse.Content);
        }
    }
}
