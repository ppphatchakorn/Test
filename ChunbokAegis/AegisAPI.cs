using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

//version 0.2

namespace ChunbokAegis
{
    public class AegisAPI
    {
        //timeout 10s
        const int TIMEOUT = 5000;
        const LogLevel LOGLEVEL = LogLevel.Information;
        
        static ILogger log;
        public static bool IsLogged { get; private set; } = false;

        public static List<XdrInstance> _allInstances;
        public static Dictionary<string, AegisCustomer> _allCustomers;

        public static void SetLogger(ILogger log)
        {
            AegisAPI.log = log;
            IsLogged = true;
        }

        public static int ReadXdrInstanceList(string filename)
        {
            int i = 0;

            string text = File.ReadAllText(filename);
            dynamic data = JsonConvert.DeserializeObject(text);

            //Check JSON
            //Console.WriteLine("Reading XDR Instance List:");
            //Console.WriteLine(data);
            if(AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Reading XDR Instance List from file: " + filename);   
            }

            foreach (var ins in data.xdr_instances)
            {
                //Console.WriteLine(ins);
                if(AegisAPI.IsLogged)
                {
                    AegisAPI.log.Log(LOGLEVEL, ((object)ins).ToString());
                }

                var instance = new XdrInstance();

                instance.xdr_instance_name = ins.xdr_instance_name;
                instance.xdr_api_url = ins.xdr_api_url;
                instance.xdr_auth_id = ins.xdr_auth_id;
                instance.xdr_auth = ins.xdr_auth;

                _allInstances.Add(instance);
                i++;
            }

            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Total XDR Instance: " + i);
            }

            return i;
        }

        public static int ReadAegisCustomerList(string filename)
        {
            int i = 0;
            string text = File.ReadAllText(filename);
            dynamic data = JsonConvert.DeserializeObject(text);

            //Check JSON
            //Console.WriteLine("Reading Customer List:");
            //Console.WriteLine(data);
            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Reading Customer List from file: " + filename);
            }

            foreach (var c in data.customers)
            {
                if (AegisAPI.IsLogged)
                {
                    AegisAPI.log.Log(LOGLEVEL, ((object)c).ToString());
                }

                var customer = new AegisCustomer();
                customer.customer_name = c.customer_name;
                customer.xdr_group_name = c.xdr_group_name;
                customer.jsm_url = c.jsm_url;
                customer.jsm_username = c.jsm_username;
                customer.jsm_password = c.jsm_password;
                customer.jsm_serviceDeskId = c.jsm_serviceDeskId;
                customer.jsm_requestTypeId = c.jsm_requestTypeId;
                customer.jsm_reporter_email = c.jsm_reporter_email;

                _allCustomers.Add(customer.xdr_group_name, customer);
                i++;

                //throw new Exception("Test Error int ReadAegisCustomerList()");
            }

            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Total Customer : " + i);
            }

            return i;
        }

        //get key-value pairs of hostname(endpoint_id)<->customer(group_name)
        public static Dictionary<string, XdrEndpoint> _instanceEndpoints;
        public static int GetEndpoint(XdrInstance instance)
        {
            int i = 0;
            string url = instance.xdr_api_url + "/public_api/v1/endpoints/get_endpoint/";
            var client = new RestClient(url);
            client.Timeout = AegisAPI.TIMEOUT;
            var request = new RestRequest(Method.POST);
            request.AddHeader("x-xdr-auth-id", instance.xdr_auth_id);
            request.AddHeader("Authorization", instance.xdr_auth);
            request.AddHeader("Content-Type", "application/json");

            //Get Endpoint
            JObject json =  new JObject(
                                new JProperty("request_data",
                                    new JObject()));

            //request.AddParameter("application/json", "{\"request_data\":{}}", ParameterType.RequestBody);
            request.AddParameter("application/json", json.ToString(), ParameterType.RequestBody);

            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Calling GetEndpoint to: " + url);
                string reqParams = null;

                foreach (var p in request.Parameters)
                {
                    reqParams += "\"" + p.Name + "\" : \"" + p.Value + "\"\r\n";
                }

                AegisAPI.log.Log(LOGLEVEL, "Request Parameter:");
                AegisAPI.log.Log(LOGLEVEL, reqParams);
                AegisAPI.log.Log(LOGLEVEL, "Request Body:");
                if (request.Body != null)
                    AegisAPI.log.Log(LOGLEVEL, request.Body.Name + ":" + request.Body.Value + "\r\n");
            }

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("ERROR Calling GetEndpoint returned status :" + (int)response.StatusCode + "-" + response.StatusCode);
            }

            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Calling GetEndpoint returned status :" + (int)response.StatusCode + " - " + response.StatusCode);
            }

            dynamic data = JsonConvert.DeserializeObject(response.Content);
            //Console.WriteLine("Reading Endpoints from Cortex XDR Instance:" + instance.xdr_api_url);
            //Console.WriteLine(data);

            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Retrieved Endpoints JSON from Cortex XDR Instance...");
                //AegisAPI.log.Log(LOGLEVEL, ((object)data).ToString());
            }

            _instanceEndpoints = new Dictionary<string, XdrEndpoint>();
            
            foreach (var e in data.reply.endpoints)
            {
                //Console.WriteLine(e);
                if (AegisAPI.IsLogged)
                {
                    AegisAPI.log.Log(LOGLEVEL, "Parsing Endpoint JSON object - " + i);
                    AegisAPI.log.Log(LOGLEVEL, ((object)e).ToString());
                }

                XdrEndpoint endpoint = new XdrEndpoint();
                endpoint.Json = e.ToString();
                endpoint.endpoint_id = e.endpoint_id;
                endpoint.endpoint_name = e.endpoint_name;

                List<string> tempArray = new List<string>();
                foreach (var node in e.group_name)
                    tempArray.Add(node.ToString());
                endpoint.group_name = tempArray.ToArray();

                if (endpoint.group_name.Length > 0)
                    endpoint.Customer = _allCustomers[endpoint.group_name[0]];

                _instanceEndpoints.Add(endpoint.endpoint_id, endpoint);

                // check if there is only one user_group in the endpoint
                // if not, throw an exception

                //Console.WriteLine(i + "." + endpoint.endpoint_id);
                //Console.WriteLine(i + "." + endpoint.endpoint_name);
                if (AegisAPI.IsLogged)
                {
                    AegisAPI.log.Log(LOGLEVEL, "Read Endpoint: " +  i + "." + endpoint.endpoint_id + ":" + endpoint.endpoint_name);
                }

                if (endpoint.Customer != null)
                {
                    //Console.WriteLine(i + "." + endpoint.Customer.customer_name + " " + endpoint.Customer.jsm_reporter_email);
                    if (AegisAPI.IsLogged)
                    {
                        AegisAPI.log.Log(LOGLEVEL, "  - Customer Found: \"" + endpoint.Customer.customer_name + "\" - " + endpoint.Customer.jsm_reporter_email + "\r\n");
                    }
                }
                else
                {
                    //Console.WriteLine("NO_CUSTOMER");
                    if (AegisAPI.IsLogged)
                    {
                        AegisAPI.log.Log(LOGLEVEL, i + "  - Customer NOT FOUND for this Endpoint\r\n");
                    }
                }
                //Console.WriteLine(i + "." + endpoint.Customer ?? endpoint.Customer.customer_name ?? "NO_CUSTOMER" + " " + endpoint.Customer.jsm_reporter_email);
                i++;
            }

            //Console.WriteLine(response.Content);

            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Total Endpoint for \"" + instance.xdr_instance_name + "\" : " + i);
            }

            return i;
        }

        public static List<XdrIncident> GetIncidents(XdrInstance instance, string status, int search_from, int search_to)
        {
            List<XdrIncident> xdrIncidents = new List<XdrIncident>();

            // Processing JSON body from CortexXDR 
            string url = instance.xdr_api_url + "public_api/v1/incidents/get_incidents/";
            var client = new RestClient(url);
            client.Timeout = AegisAPI.TIMEOUT;

            var request = new RestRequest(Method.POST);
            request.AddHeader("x-xdr-auth-id", instance.xdr_auth_id);
            request.AddHeader("Authorization", instance.xdr_auth);
            request.AddHeader("Content-Type", "application/json");

            //request.AddParameter("application/json", "{\"request_data\":{\"filters\":[{\"field\": \"status\",\"operator\": \"eq\",\"value\": \"new\"}],\"search_from\": 0,\"search_to\": 100,\"sort\": {\"field\": \"creation_time\",\"keyword\": \"asc\"}}}", ParameterType.RequestBody);
            //request.AddParameter("application/json", "{\"request_data\":{\"filters\":[{\"field\": \"status\",\"operator\": \"eq\",\"value\": \"" + status + "\"}],\"search_from\": " + search_from + ",\"search_to\": " + search_to + ",\"sort\": {\"field\": \"creation_time\",\"keyword\": \"asc\"}}}", ParameterType.RequestBody);

            JObject json = new JObject(
                                new JProperty("request_data",
                                    new JObject(
                                        new JProperty("filters",
                                            new JArray(
                                                new JObject(
                                                    new JProperty("field", "status"),
                                                    new JProperty("operator", "eq"),
                                                    new JProperty("value", status)))),
                                            new JProperty("search_from", search_from),
                                            new JProperty("search_to", search_to),
                                            new JProperty("sort",
                                                new JObject(
                                                    new JProperty("field", "creation_time"),
                                                    new JProperty("keyword", "asc")
                                                )))));

            request.AddParameter("application/json", json.ToString(), ParameterType.RequestBody);

            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Calling GetIncidents to: " + url);
                string reqParams = null;

                foreach (var p in request.Parameters)
                {
                    reqParams += "\"" + p.Name + "\" : \"" + p.Value + "\"\r\n";
                }

                AegisAPI.log.Log(LOGLEVEL, "Request Parameter:");
                AegisAPI.log.Log(LOGLEVEL, reqParams);
                AegisAPI.log.Log(LOGLEVEL, "Request Body:");
                if (request.Body != null)
                    AegisAPI.log.Log(LOGLEVEL, request.Body.Name + ":" + request.Body.Value + "\r\n");
            }

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("ERROR Calling GetIncidents returned status :" + (int)response.StatusCode + "-" + response.StatusCode);
            }

            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Calling GetIncidents returned status :" + (int)response.StatusCode + "-" + response.StatusCode);
            }

            dynamic data = JsonConvert.DeserializeObject(response.Content);
            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Retrieving Incidents JSON from Cortex XDR Instance...");
                //AegisAPI.log.Log(LOGLEVEL, ((object)data).ToString());
            }

            foreach (var inc in data.reply.incidents)
            {
                //Console.WriteLine(e);
                if (AegisAPI.IsLogged)
                {
                    AegisAPI.log.Log(LOGLEVEL, "\r\nParsing Incident JSON object...");
                    AegisAPI.log.Log(LOGLEVEL, ((object)inc).ToString());
                }

                string tickStr;
                long tick;

                XdrIncident incident = new XdrIncident();
                incident.Json = inc.ToString();

                incident.incident_id = inc.incident_id;
                incident.incident_name = inc.incident_name;
                
                tickStr = inc.creation_time;
                if (!long.TryParse(tickStr, out tick))
                    throw new Exception("ERROR parsing Incident creation_time: " + inc.creation_time + "\r\n" + inc);
                else
                {
                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(tick);
                    incident.creation_time = dateTimeOffset.UtcDateTime;
                }

                tickStr = inc.modification_time;
                if (!long.TryParse(tickStr, out tick))
                    throw new Exception("ERROR parsing Incident modification_time: " + inc.modification_time + "\r\n" + inc);
                else
                    incident.modification_time = new DateTime(tick);

                tickStr = inc.detection_time;
                if (!long.TryParse(tickStr, out tick))
                    incident.detection_time = null;
                else
                    incident.detection_time = new DateTime(tick);

                //incident.status = inc.status; // always new

                incident.severity = inc.severity;
                incident.description = inc.description; // to JSM Summary
                incident.assigned_user_mail = inc.assigned_user_mail;
                incident.assigned_user_pretty_name = inc.assigned_user_pretty_name;
                incident.alert_count = inc.alert_count;
                incident.low_severity_alert_count = inc.low_severity_alert_count;
                incident.med_severity_alert_count = inc.med_severity_alert_count;
                incident.high_severity_alert_count = inc.high_severity_alert_count;
                incident.user_count = inc.user_count;
                incident.host_count = inc.host_count;
                incident.notes = inc.notes;
                incident.resolve_comment = inc.resolve_comment;
                incident.manual_severity = inc.manual_severity;
                incident.manual_description = inc.manual_description;
                incident.xdr_url = inc.xdr_url;
                incident.starred = inc.starred;

                //if hosts != 1, throw this incident into a temp Project
                //parse - hosts = endpoint_name + endpoint_id

                List<string> tempArray_1 = new List<string>();
                List<string> tempArray_2 = new List<string>();

                foreach (var node in inc.hosts)
                {
                    string tmp = node.ToString();
                    int idx = tmp.IndexOf(":");
                    tempArray_1.Add(tmp.Substring(0, idx));
                    tempArray_2.Add(tmp.Substring(idx + 1, tmp.Length - idx - 1));
                }
                incident.hosts = tempArray_1.ToArray();
                incident.endpoint_ids = tempArray_2.ToArray();
                tempArray_1.Clear();
                tempArray_2.Clear();

                foreach (var node in inc.users)
                    tempArray_1.Add(node.ToString());
                incident.users = tempArray_1.ToArray();
                tempArray_1.Clear();

                foreach (var node in inc.incident_sources)
                    tempArray_1.Add(node.ToString());
                incident.incident_sources = tempArray_1.ToArray();
                tempArray_1.Clear();

                incident.rule_based_score = inc.rule_based_score;
                incident.manual_score = inc.manual_score;

                xdrIncidents.Add(incident);
            }

            return xdrIncidents;
        }


        public static void CreateRequest(AegisCustomer customer, string host, XdrIncident incident)
        {
            // All desired field
            string summary;
            Regex regex = new Regex("\'(.*?)\'");
            var match = regex.Match(incident.description);
            if (match.Success)
                summary = match.Groups[1].Value;
            else
                summary = incident.description;
            
            //string host = incident.hosts[0];
            string source = incident.incident_sources[0];
            string datetime_ISO8601 = incident.creation_time.ToString("o");

            string url = customer.jsm_url + "rest/servicedeskapi/request";
            var client = new RestClient(url);
            client.Timeout = AegisAPI.TIMEOUT;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");

            string auth = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(customer.jsm_username + ":" + customer.jsm_password));
            request.AddHeader("Authorization", "Basic " + auth);

            //request.AddParameter("application/json", "{\"serviceDeskId\": \"3\",\"requestTypeId\": \"61\",\"requestFieldValues\": {\"summary\": \"Report a very critical Security Incident!!! via REST\",\"description\": \"test description\",\"priority\": {\"name\": \"High\"},\"customfield_10055\": \"1984-07-07T07:07:07.777+0700\",\"customfield_10056\": \"xdr_url\",\"customfield_10057\": \"description\",\"customfield_10058\": \"incident_id\",\"customfield_10059\": {\"value\": \"XDR Agent\"},\"customfield_10062\": \"host\"},\"raiseOnBehalfOf\": \"dummy@gmail.com\"}", ParameterType.RequestBody);
            //request.AddParameter("application/json", "{\"serviceDeskId\": \"" + customer.jsm_serviceDeskId + "\",\"requestTypeId\": \"" + customer.jsm_requestTypeId + "\",\"requestFieldValues\": {\"summary\": \"" + summary + "\",\"description\": \"" + incident.description + "\",\"priority\": {\"name\": \"Medium\"},\"customfield_10055\": \"" + datetime_ISO8601 + "\",\"customfield_10056\": \"" + incident.xdr_url + "\",\"customfield_10057\": \"" + incident.description + "\",\"customfield_10058\": \"" + incident.incident_id + "\",\"customfield_10059\": {\"value\": \"" + source + "\"},\"customfield_10062\": \"" + host + "\"},\"raiseOnBehalfOf\": \"" + customer.jsm_reporter_email + "\"\r\n}", ParameterType.RequestBody);

            JObject json = new JObject(
                                new JProperty("serviceDeskId", customer.jsm_serviceDeskId),
                                new JProperty("requestTypeId", customer.jsm_requestTypeId),
                                new JProperty("requestFieldValues",
                                    new JObject(
                                        new JProperty("summary", summary),
                                        new JProperty("description", incident.description),
                                        new JProperty("priority",
                                            new JObject(
                                                new JProperty("name", "Medium"))),
                                        new JProperty("customfield_10055", datetime_ISO8601),
                                        new JProperty("customfield_10056", incident.xdr_url),
                                        new JProperty("customfield_10057", incident.description),
                                        new JProperty("customfield_10058", incident.incident_id),
                                        new JProperty("customfield_10059",
                                            new JObject(
                                                new JProperty("value", source))),
                                        new JProperty("customfield_10062", host))),
                                new JProperty("raiseOnBehalfOf", customer.jsm_reporter_email));
            request.AddParameter("application/json", json.ToString(), ParameterType.RequestBody);

            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "CreateRequest by Incident :" + incident.incident_id);
                string reqParams = null;

                foreach (var p in request.Parameters)
                {
                    reqParams += "\"" + p.Name + "\" : \"" + p.Value + "\"\r\n";
                }

                AegisAPI.log.Log(LOGLEVEL, "Request Parameter:");
                AegisAPI.log.Log(LOGLEVEL, reqParams);
                AegisAPI.log.Log(LOGLEVEL, "Request Body:");
                if (request.Body != null)
                    AegisAPI.log.Log(LOGLEVEL, request.Body.Name + ":" + request.Body.Value + "\r\n");
            }

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception("ERROR Calling CreateRequest returned status :" + (int)response.StatusCode + "-" + response.StatusCode);
            }

            //IRestResponse response = client.Execute(request);

            //if (response.StatusCode != HttpStatusCode.OK)
            //{
            //    throw new Exception("CreateRequest returned status :" + response.StatusCode);
            //}

            AegisAPI.log.Log(LOGLEVEL, "*** Calling CreateRequest sucessfully, status " + (int)response.StatusCode + " - " + response.StatusCode + " ***");
            AegisAPI.log.Log(LOGLEVEL, response.Content);
            //Console.WriteLine(response.Content);

        }

        //use only "new" and "under_investigation"
        //{"reply": true}
        public static void UpdateIncidentStatus(XdrInstance instance, XdrIncident incident, string status)
        {
            var client = new RestClient(instance.xdr_api_url + "public_api/v1/incidents/update_incident/");

            client.Timeout = AegisAPI.TIMEOUT;
            var request = new RestRequest(Method.POST);
            request.AddHeader("x-xdr-auth-id", instance.xdr_auth_id);
            request.AddHeader("Authorization", instance.xdr_auth);
            request.AddHeader("Content-Type", "application/json");

            //request.AddParameter("application/json", "{\"request_data\":{\"incident_id\":\"" + incident.incident_id + "\",\"update_data\":{\"status\":\"" + status + "\"}}}", ParameterType.RequestBody);
            JObject json = new JObject(
                                new JProperty("request_data",
                                    new JObject(
                                        new JProperty("incident_id", incident.incident_id),
                                        new JProperty("update_data",
                                            new JObject(
                                                new JProperty("status", status))))));
            request.AddParameter("application/json", json.ToString(), ParameterType.RequestBody);

            //if (AegisAPI.IsLogged)
            //{
            //    AegisAPI.log.Log(LOGLEVEL, "Updating Instance: " + instance.xdr_instance_name + " Incident :" + incident.incident_id + " - " + status);
            //}
            //Console.WriteLine("Updating Instance: " + instance.xdr_instance_name + " Incident :" + incident.incident_id + " - " + status);

            /////
            if (AegisAPI.IsLogged)
            {
                AegisAPI.log.Log(LOGLEVEL, "Updating Instance: " + instance.xdr_instance_name + " Incident :" + incident.incident_id + " - " + status);
                string reqParams = null;

                foreach (var p in request.Parameters)
                {
                    reqParams += "\"" + p.Name + "\" : \"" + p.Value + "\"\r\n";
                }

                AegisAPI.log.Log(LOGLEVEL, "Request Parameter:");
                AegisAPI.log.Log(LOGLEVEL, reqParams);
                AegisAPI.log.Log(LOGLEVEL, "Request Body:");
                if (request.Body != null)
                    AegisAPI.log.Log(LOGLEVEL, request.Body.Name + ":" + request.Body.Value + "\r\n");
            }

            IRestResponse response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("ERROR Calling UpdateIncidents returned status :" + (int)response.StatusCode + "-" + response.StatusCode);
            }

            //IRestResponse response = client.Execute(request);
            //if (response.StatusCode != HttpStatusCode.OK)
            //{
            //    throw new Exception("GetIncidents from \"" + instance.xdr_instance_name + "\" returned status :" + response.StatusCode);
            //}

            //Console.WriteLine(response.Content);

            return;
        }
    }
}
