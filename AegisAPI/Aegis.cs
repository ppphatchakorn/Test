using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ChunbokAegis
{
    public class AegisAPI
    {
        public static List<XdrInstance> _xdrInstance;
        public static Dictionary<string, AegisCustomer> _aegisCustomer;

        public static int ReadXdrInstanceList(string filename)
        {
            int i = 0;

            string text = File.ReadAllText(filename);
            dynamic data = JsonConvert.DeserializeObject(text);

            //Check JSON
            //Console.WriteLine("XDR Instance List:");
            //Console.WriteLine(data);

            foreach (var ins in data.xdr_instances)
            {
                //Console.WriteLine(ins);

                var xdrInstance = new XdrInstance();
                xdrInstance.xdr_instance_name = ins.xdr_instance_name;
                xdrInstance.xdr_api_url = ins.xdr_api_url;
                xdrInstance.xdr_auth_id = ins.xdr_auth_id;
                xdrInstance.xdr_auth = ins.xdr_auth;

                //Console.WriteLine(i + ". xdrInstance.xdr_instance_name: " + xdrInstance.xdr_instance_name);
                //Console.WriteLine(i + ". xdrInstance.xdr_api_url: " + xdrInstance.xdr_api_url);
                //Console.WriteLine(i + ". xdrInstance.xdr_auth_id: " + xdrInstance.xdr_auth_id);
                //Console.WriteLine(i + ". xdrInstance.xdr_auth: " + xdrInstance.xdr_auth);

                _xdrInstance.Add(xdrInstance);
                i++;

                //throw new Exception("Test Error int ReadXdrInstanceList()");
            }

            return i;
        }

        public static int ReadAegisCustomerList(string filename)
        {
            int i = 0;
            string text = File.ReadAllText(filename);
            dynamic data = JsonConvert.DeserializeObject(text);

            //Check JSON
            //Console.WriteLine("Customer List:");
            //Console.WriteLine(data);

            foreach (var c in data.customers)
            {
                //Console.WriteLine(c);

                var customer = new AegisCustomer();
                customer.customer_name = c.customer_name;
                customer.xdr_group_name = c.xdr_group_name;
                customer.jsm_url = c.jsm_url;
                customer.jsm_project_id = c.jsm_project_id;
                customer.jsm_issuetype_id = c.jsm_issuetype_id;
                customer.jsm_reporter_email = c.jsm_reporter_email;

                //Console.WriteLine(i + "." + customer.customer_name);
                //Console.WriteLine(i + "." + customer.xdr_group_name);
                //Console.WriteLine(i + "." + customer.jsm_url);
                //Console.WriteLine(i + "." + customer.jsm_project_id);
                //Console.WriteLine(i + "." + customer.jsm_issuetype_id);
                //Console.WriteLine(i + "." + customer.jsm_reporter_email);

                _aegisCustomer.Add(customer.xdr_group_name, customer);
                i++;

                //throw new Exception("Test Error int ReadAegisCustomerList()");
            }

            return i;
        }
    }
}
