using Newtonsoft.Json.Linq;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //Ref https://www.newtonsoft.com/json/help/html/CreatingLINQtoJSON.htm
            JObject rss;

            //Get Incident
            rss = new JObject(
                    new JProperty("request_data",
                        new JObject(
                            new JProperty("filters",
                                new JArray(
                                    new JObject(
                                        new JProperty("field", "yy"),
                                        new JProperty("operator", "yy"),
                                        new JProperty("value", "zz")))),
                                new JProperty("search_from", 0),
                                new JProperty("search_to", 100  ),
                                new JProperty("sort",
                                    new JObject(
                                        new JProperty("field", "cc"),
                                        new JProperty("keyword", "dd")
                                    )))));
                                                                    

            Console.WriteLine(rss.ToString());

            //Get Endpoint
            rss = new JObject(
                    new JProperty("request_data",
                        new JObject()));

            Console.WriteLine(rss.ToString());

            //Update Incident
            rss = new JObject(
                    new JProperty("request_data",
                        new JObject(
                            new JProperty("incident_id", ""),
                            new JProperty("update_data",
                                new JObject(
                                    new JProperty("status", ""))))));

            Console.WriteLine(rss.ToString());

            //Create Request
            rss = new JObject(
                    new JProperty("serviceDeskId", ""),
                    new JProperty("requestTypeId", ""),
                    new JProperty("requestFieldValues",
                        new JObject(
                            new JProperty("summary", ""),
                            new JProperty("description", ""),
                            new JProperty("priority",
                                new JObject(
                                    new JProperty("name", ""))),
                            new JProperty("customfield_10055", ""),
                            new JProperty("customfield_10056", ""),
                            new JProperty("customfield_10057", ""),
                            new JProperty("customfield_10058", ""),
                            new JProperty("customfield_10059",
                                new JObject(
                                    new JProperty("value", ""))),
                            new JProperty("customfield_10062", ""))),
                    new JProperty("raiseOnBehalfOf", "imsobad@gmail.com"));

            Console.WriteLine(rss.ToString());


            string auth = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("pawarat@dataexpress.co.th" + ":" + "oTIy8rj0DrUeRU7elD4UED5C"));
            Console.WriteLine(auth);
        }
    }
}
