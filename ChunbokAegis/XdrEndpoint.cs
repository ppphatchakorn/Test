using System;
using System.Collections.Generic;
using System.Text;

namespace ChunbokAegis
{
    public class XdrEndpoint
    {
        public string Json;
        public AegisCustomer Customer;

        public string endpoint_id;
        // perhaps case-insensitive
        public string endpoint_name;
        // to get endpoint (should be a single value)
        public string[] group_name;
        
    }
}
