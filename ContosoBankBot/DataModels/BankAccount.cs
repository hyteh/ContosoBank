using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ContosoBankBot.DataModels
{
    public class BankAccount
    {
        [JsonProperty(PropertyName = "Id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "PartitionKey")]
        public string partitionKey { get; set; } //User or account

        [JsonProperty(PropertyName = "username")]
        public string username { get; set; } //Username 

        /*User*/
        [JsonProperty(PropertyName = "password")]
        public string password { get; set; }

        [JsonProperty(PropertyName = "Name")]
        public string Name { get; set; }

        /*Account*/
        [JsonProperty(PropertyName = "accountNo")]
        public int accountNo { get; set; }

        [JsonProperty(PropertyName = "balance")]
        public double balance { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime date { get; set; }
    }
}

