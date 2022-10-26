using System.Collections.Generic;

using Newtonsoft.Json;

namespace Stratis.Bitcoin.Controllers.Models.Was
{

    public class ResponseError
    {
        [JsonProperty(PropertyName = "errorCode")]
        public int ErrorCode { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
    }
    public class Response
    {
        [JsonProperty(PropertyName = "error")]
        public ResponseError Error { get; set; }
    }

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

    public class ResponseSendToAddress : Response
    {
        [JsonProperty(PropertyName = "txHash")]
        public string TxHash { get; set; }
    }

    public class RequestTransactions
    {
        [JsonProperty(PropertyName = "txHashs")]
        public IEnumerable<string> TxHashs { get; set; }
    }

    public class ResponseTransactions
    {
        [JsonProperty(PropertyName = "txs")]
        public IEnumerable<Transaction> Txs { get; set; }

        [JsonProperty(PropertyName = "pools")]
        public IEnumerable<Transaction> Pools { get; set; }
    }
}
