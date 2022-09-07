namespace Stratis.Bitcoin.Controllers.Models.Was
{
    public class OutPoint
    {
        public string Hash { get; set; }

        public long N { get; set; }
    }

    public class TxIn
    {
        public OutPoint PrevOut { get; set; }

        public string ScriptSig { get; set; }

        public string WitScript { get; set; }

        public long Sequence { get; set; }
    }

    public static class TxInExtends
    {
        public static TxIn ToModel(this NBitcoin.TxIn nTxIn)
        {
            return new()
            {
                PrevOut = nTxIn.PrevOut.ToModel(),
                ScriptSig = nTxIn.ScriptSig.ToHex(),
                WitScript = nTxIn.WitScript.ToString(),
                Sequence = nTxIn.Sequence,
            };
        }
    }

    public static class OutPointExtends
    {
        public static OutPoint ToModel(this NBitcoin.OutPoint nOutPoint)
        {
            return new()
            {
                Hash = nOutPoint.Hash.ToString(),
                N = nOutPoint.N,
            };
        }
    }
}
