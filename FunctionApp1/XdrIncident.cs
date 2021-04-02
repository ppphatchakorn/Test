using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionApp1
{
    public class XdrIncident
    {
        public string incident_id;
        public string incident_name;
        public DateTime creation_time;
        public DateTime modification_time;
        public DateTime? detection_time;
        public string status;
        public string severity;
        public string description;
        public string assigned_user_mail;
        public string assigned_user_pretty_name;
        public int alert_count;
        public int low_severity_alert_count;
        public int med_severity_alert_count;
        public int high_severity_alert_count;
        public int user_count;
        public int host_count;
        public string notes;
        public string resolve_comment;
        public string manual_severity;
        public string manual_description;
        public string xdr_url;
        public bool starred;
        public string[] hosts;
        public string[] users;
        public string[] incident_sources;
        public string rule_based_score;
        public string manual_score;
    }
}
