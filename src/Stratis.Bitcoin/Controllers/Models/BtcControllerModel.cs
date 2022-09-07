using Newtonsoft.Json;

namespace Stratis.Bitcoin.Controllers.Models
{
    public class AddressBalance
    {
        [JsonProperty(PropertyName = "satoshi")]
        public long Satoshi { get; set; }

    }

    public class RequestSendToAddress
    {
        [JsonProperty(PropertyName = "wif")]
        public string Wif { get; set; }

        [JsonProperty(PropertyName = "fromAddress")]
        public string fromAddress { get; set; }

        [JsonProperty(PropertyName = "toAddress")]
        public string ToAddress { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public long Amount { get; set; }

        [JsonProperty(PropertyName = "subtractFeesFromRecipients")]
        public bool SubtractFeesFromRecipients { get; set; }
    }

    public class ResponseSendToAddress
    {
        [JsonProperty(PropertyName = "txHash")]
        public string TxHash { get; set; }
    }
}
