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

        [JsonProperty(PropertyName = "owner")]
        public string owner { get; set; }

        [JsonProperty(PropertyName = "accountNo")]
        public int accountNo { get; set; }

        [JsonProperty(PropertyName = "balance")]
        public double balance { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime date { get; set; }


    }
}