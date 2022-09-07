namespace Stratis.Bitcoin.Controllers.Models.Was
{
    public class TxOut
    {
        public string ScriptPubKey { get; set; }
        public long Satoshis { get; set; }
    }

    public static class TxOutExtends
    {
        public static TxOut ToModel(this NBitcoin.TxOut nTxOut)
        {
            return new()
            {
                ScriptPubKey = nTxOut.ScriptPubKey.ToHex(),
                Satoshis = nTxOut.Value.Satoshi,
            };
        }
    }
}
