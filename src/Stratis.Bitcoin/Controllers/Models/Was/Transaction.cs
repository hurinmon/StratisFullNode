
using System.Collections.Generic;
using System.Linq;

namespace Stratis.Bitcoin.Controllers.Models.Was
{
    public class Transaction
    {
        public string Hash { get; set; }
        public uint Version { get; set; }
        public IEnumerable<TxIn> TxIns { get; set; }
        public IEnumerable<TxOut> TxOuts { get; set; }

        internal const uint LOCKTIME_THRESHOLD = 500000000; // Tue Nov  5 00:53:20 1985 UTC
        public long LockTime { get; set; }


    }

    public static class TransactionExtends
    {
        public static Transaction ToModel(this NBitcoin.Transaction nTransaction)
        {
            return new()
            {
                Version = nTransaction.Version,
                Hash = nTransaction.GetHash().ToString(),
                TxIns = nTransaction.Inputs.Select(x => x.ToModel()),
                TxOuts = nTransaction.Outputs.Select(x => x.ToModel()),
                LockTime = nTransaction.LockTime,
            };
        }
    }
}
