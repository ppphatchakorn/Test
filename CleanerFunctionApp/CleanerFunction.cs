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
        tingAssembly().Location);
            var rootDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));

            log.LogInformation("binDirectory = " + binDirectory);
            log.LogInformation("rootDirectory =  " + rootDirectory);

            string instanceFile = rootDirectory + @"\aegis_xdr_instances.json";
            string customerFile = rootDirectory + @"\aegis_customers.json";

            AegisAPI.SetLogger(log);
            AegisAPI._allInstances = new List<ChunbokAegis.XdrInstance>();
            AegisAPI._allCustomers = new Dictionary<string, ChunbokAegis.AegisCustomer>();

            //query POST parameter (not relevant)
            //string name = req.Query["name"];

            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

            //string responseMessage = string.IsNullOrEmpty(name)
            //    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //    : $"Hello, {name}. This HTTP triggered function executed successfully." + revision;
            //query POST parameter (not relevant)

            try
            {
                log.LogInformation("\r\n***\r\n Reading XDR Instance list file: " + instanceFile);
                AegisAPI.ReadXdrInstanceList(instanceFile);

                log.LogInformation("\r\n***\r\n Reading Aegis Customer list file: " + customerFile);
                AegisAPI.ReadAegisCustomerList(customerFile);

                //Incident status "under_investigation" OR "new"
                string currentStatus = "new";
                string newStatus = "new";

                foreach (XdrInstance instance in AegisAPI._allInstances)
                {
                    DateTime start = DateTime.Now;
                    //int i = 0;

                    log.LogInformation("\r\n***\r\n*** Getting Endpoints from: " + instance.xdr_instance_name);
                    AegisAPI.GetEndpoint(instance);
                    //log.LogInformation("Total Endpoints: " + AegisAPI._instanceEndpoints.Count);

                    log.LogInformation("\r\n***\r\n*** Getting Incidents from: " + instance.xdr_instance_name + " with status = \"" + currentStatus + "\"");
                    List<XdrIncident> incidents = AegisAPI.GetIncidents(instance, currentStatus, 0, 100);
                    ///log.LogInformation("Queried [" + incidents.Count + "] from XDR Instance :" + instance.xdr_instance_name);

                    foreach (XdrIncident incident in incidents)
                    {
                        //OLD code
                        //log.LogInformation("\r\n***\r\n*** Create Request on Jira: " + incident.incident_id + " - " + incident.description);
                        ////AegisAPI.CreateRequest(incident);

                        //log.LogInformation("\r\n***\r\n*** Update Incidents status on Cortex: " + incident.incident_id + " to \"" + newStatus + "\"");
                        //AegisAPI.UpdateIncidentStatus(instance, incident, newStatus);
                        //end OLD code

                        //////////**********
                        // Start Processing Incident
                        log.LogInformation("\r\n***\r\n*** Processing Incident: " + incident.incident_id + " - " + incident.description);

                        // Check if an Incident contains Endpoint(s) (host), If not, skip this Incident
                        if (incident.endpoint_ids.Length == 0)
                        {
                            log.LogError("ERROR - incident.endpoint_ids.Length == 0 " + incident.Json);
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
                                //lineRequest.AddOrUpdateParameter("Message", "Unable to match Customer Data for Incident :" + incident.Json);
                                //lineResponse = lineClient.Execute(lineRequest);
                                continue;
                            }

                            AegisCustomer customer = AegisAPI._instanceEndpoints[endpoint_id].Customer;
                            if (customer == null)
                            {
                                log.LogError("ERROR - Customer = null on Incident:" + incident.incident_id + incident.Json + "\r\nSkipping...");
                                //lineRequest.AddOrUpdateParameter("Message", "customer = null on incident:" + incident.incident_id + incident.Json + "\r\nSkipping...");
                                //lineResponse = lineClient.Execute(lineRequest);
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
                            //AegisAPI.CreateRequest(incident);
                            AegisAPI.CreateRequest(customer, host, incident);

                            log.LogInformation("\r\n***\r\n*** Update Incidents status on Cortex: " + incident.incident_id + " to \"" + newStatus + "\"");
                            AegisAPI.UpdateIncidentStatus(instance, incident, newStatus);
                        }

                        //incidentCounter++;

                        //////////**********
                    }

                    //i++;
                    DateTime end = DateTime.Now;
                    log.LogInformation("Process time for from XDR Instance: " + instance.xdr_instance_name + " = " + new TimeSpan(end.Ticks - start.Ticks).TotalSeconds);
                }

            }
            catch (Exception e)
            {
                log.LogError("**** ERROR FOUND - Excepetion catched ****");
                log.LogError(e.Message);
                log.LogError(e.StackTrace);

                log.LogError("*** Code Excecuted with ERROR.");
                return new OkObjectResult("This HTTP triggered function executed with ERROR. " + revision);
            }

            DateTime endTime = DateTime.Now;
            TimeSpan totalTime = new TimeSpan(endTime.Ticks - startTime.Ticks);

            log.LogInformation("*** Code Excecuted sucessfully.\r\n*** Total Process time: " + totalTime.TotalSeconds);

            //return new OkObjectResult(responseMessage);
            return new OkObjectResult("This HTTP triggered function executed successfully. " + revision);
        }
    }
}
