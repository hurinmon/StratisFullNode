using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using NBitcoin;

using Stratis.Bitcoin.AsyncWork;
using Stratis.Bitcoin.Base;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Connection;
using Stratis.Bitcoin.Consensus;
using Stratis.Bitcoin.Controllers.Models;
using Stratis.Bitcoin.Features.BlockStore.AddressIndexing;
using Stratis.Bitcoin.Features.Consensus.CoinViews;
using Stratis.Bitcoin.Features.Wallet.Interfaces;
using Stratis.Bitcoin.Interfaces;
using Stratis.Bitcoin.P2P;
using Stratis.Bitcoin.Signals;
using Stratis.Bitcoin.Utilities;

namespace Stratis.Bitcoin.Features.Api
{
    /// <summary>
    /// Provides methods that interact with the full node.
    /// </summary>
    [ApiVersion("1")]
    [Route("api/[controller]")]
    public class BtcController : Controller
    {
        /// <summary>Full Node.</summary>
        private readonly IFullNode fullNode;

        /// <summary>Instance logger.</summary>
        private readonly ILogger logger;

        /// <summary>Information about node's chain.</summary>
        private readonly IChainState chainState;

        /// <summary>Provider of date and time functions.</summary>
        private readonly IDateTimeProvider dateTimeProvider;

        /// <summary>The settings for the node.</summary>
        private readonly NodeSettings nodeSettings;

        /// <summary>The connection manager.</summary>
        private readonly IConnectionManager connectionManager;

        /// <summary>Thread safe access to the best chain of block headers from genesis.</summary>
        private readonly ChainIndexer chainIndexer;

        /// <summary>An interface implementation used to retrieve the network's difficulty target.</summary>
        private readonly INetworkDifficulty networkDifficulty;

        /// <summary>An interface implementaiton used to retrieve a pooled transaction.</summary>
        private readonly IPooledTransaction pooledTransaction;

        /// <summary>An interface implementation used to retrieve unspent transactions from a pooled source.</summary>
        private readonly IPooledGetUnspentTransaction pooledGetUnspentTransaction;

        /// <summary>An interface implementation used to retrieve unspent transactions.</summary>
        private readonly IGetUnspentTransaction getUnspentTransaction;

        private readonly IInitialBlockDownloadState initialBlockDownloadState;

        /// <summary>Specification of the network the node runs on.</summary>
        private readonly Network network; // Not readonly because of ValidateAddress

        /// <summary>An interface implementation for the blockstore.</summary>
        private readonly IBlockStore blockStore;

        /// <summary>Provider for creating and managing background async loop tasks.</summary>
        private readonly IAsyncProvider asyncProvider;

        private readonly ISelfEndpointTracker selfEndpointTracker;

        private readonly ISignals signals;

        private readonly IConsensusManager consensusManager;

        private readonly IBroadcasterManager broadcasterManager;

        private readonly IAddressIndexer addressIndexer;

        private readonly ICoinView coinView;

        public BtcController(
            ChainIndexer chainIndexer,
            IChainState chainState,
            IConnectionManager connectionManager,
            IDateTimeProvider dateTimeProvider,
            IFullNode fullNode,
            ILoggerFactory loggerFactory,
            NodeSettings nodeSettings,
            Network network,
            IAsyncProvider asyncProvider,
            ISelfEndpointTracker selfEndpointTracker,
            IConsensusManager consensusManager,
            IBlockStore blockStore,
            IInitialBlockDownloadState initialBlockDownloadState,
            ISignals signals,
            IGetUnspentTransaction getUnspentTransaction = null,
            INetworkDifficulty networkDifficulty = null,
            IPooledGetUnspentTransaction pooledGetUnspentTransaction = null,
            IPooledTransaction pooledTransaction = null,
            IBroadcasterManager broadcasterManager = null,
            IAddressIndexer addressIndexer = null,
            ICoinView coinView = null)
        {
            this.asyncProvider = asyncProvider;
            this.chainIndexer = chainIndexer;
            this.chainState = chainState;
            this.connectionManager = connectionManager;
            this.dateTimeProvider = dateTimeProvider;
            this.fullNode = fullNode;
            this.logger = loggerFactory.CreateLogger(GetType().FullName);
            this.network = network;
            this.nodeSettings = nodeSettings;
            this.selfEndpointTracker = selfEndpointTracker;
            this.signals = signals;

            this.consensusManager = consensusManager;
            this.blockStore = blockStore;
            this.getUnspentTransaction = getUnspentTransaction;
            this.initialBlockDownloadState = initialBlockDownloadState;
            this.networkDifficulty = networkDifficulty;
            this.pooledGetUnspentTransaction = pooledGetUnspentTransaction;
            this.pooledTransaction = pooledTransaction;
            this.addressIndexer = Guard.NotNull(addressIndexer, nameof(addressIndexer));
            this.broadcasterManager = Guard.NotNull(broadcasterManager, nameof(broadcasterManager));
            this.coinView = Guard.NotNull(coinView, nameof(coinView));
        }

        [Route("getaddressbalance")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public IActionResult GetAddressBalance(string address)
        {
            AddressBalance response = new()
            {
                Satoshi = -1
            };
            try
            {
                AddressBalancesResult result = this.addressIndexer.GetAddressBalances(new[] { address }, 1);
                response.Satoshi = result.Balances.First().Balance.Satoshi;
            }
            catch (Exception e)
            {
                this.logger.LogError("Exception occurred: {0}", e.ToString());
            }

            return Json(response);
        }

        [Route("sendtoadress")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<ResponseSendToAddress> SendToAdressAsync([FromBody] RequestSendToAddress request)
        {
            Transaction transaction = SendCoin(request.Wif, request.fromAddress, request.ToAddress, request.Amount, false);
            await this.broadcasterManager.BroadcastTransactionAsync(transaction);
            return new ResponseSendToAddress
            {
                TxHash = transaction.GetHash().ToString(),
            };
        }
        public Transaction SendCoin(string wif, string fromAddress, string destinationAddress, long amount, bool subtractFeesFromRecipients)
        {
            return SendCoin(Key.Parse(wif, this.network), fromAddress, destinationAddress, amount, new Money(this.network.MinTxFee), subtractFeesFromRecipients);
        }
        public Transaction SendCoin(Key sourcePriveatKey, string fromAddress, string destinationAddress, long amount, Money fee, bool subtractFeesFromRecipients)
        {
            List<Coin> unspendCoins = new();
            {

                VerboseAddressBalancesResult balancesResult = this.addressIndexer.GetAddressIndexerState(new[] { fromAddress });

                if (balancesResult.BalancesData == null || balancesResult.BalancesData.Count != 1)
                {
                    this.logger.LogWarning("No balances found for address {0}, Reason: {1}", fromAddress, balancesResult.Reason);
                    return null;
                }

                BitcoinAddress bitcoinAddress = this.network.CreateBitcoinAddress(fromAddress);

                AddressIndexerData addressBalances = balancesResult.BalancesData.First();

                List<AddressBalanceChange> deposits = addressBalances.BalanceChanges.Where(x => x.Deposited).ToList();

                List<int> heights = deposits.Select(x => x.BalanceChangedHeight).Distinct().ToList();
                HashSet<uint256> blocksToRequest = new HashSet<uint256>(heights.Count);

                foreach (int height in heights)
                {
                    uint256 blockHash = this.chainState.ConsensusTip.GetAncestor(height).Header.GetHash();
                    blocksToRequest.Add(blockHash);
                }

                List<Block> blocks = this.blockStore.GetBlocks(blocksToRequest.ToList());
                List<OutPoint> collectedOutPoints = new List<OutPoint>(deposits.Count);

                foreach (Transaction transaction in blocks.SelectMany(x => x.Transactions))
                {
                    for (int i = 0; i < transaction.Outputs.Count; i++)
                    {
                        if (!transaction.Outputs[i].IsTo(bitcoinAddress))
                            continue;

                        collectedOutPoints.Add(new OutPoint(transaction, i));
                    }
                }

                FetchCoinsResponse fetchCoinsResponse = this.coinView.FetchCoins(collectedOutPoints.ToArray());

                foreach (KeyValuePair<OutPoint, UnspentOutput> unspentOutput in fetchCoinsResponse.UnspentOutputs)
                {
                    if (unspentOutput.Value.Coins == null)
                        continue; // spent
                    unspendCoins.Add(new Coin(unspentOutput.Key, unspentOutput.Value.Coins.TxOut));
                }
            }
            TransactionBuilder builder = null;
            try
            {
                NBitcoin.Script destination = BitcoinAddress.Create(destinationAddress).ScriptPubKey;

                Coin[] coins = unspendCoins.ToArray();

                builder = new TransactionBuilder(this.network);
                builder = builder
                    .AddCoins(coins)
                    .AddKeys(sourcePriveatKey)
                    .Send(destination, new Money(amount, MoneyUnit.Satoshi))
                    .SetChange(sourcePriveatKey)
                    .SendFees(fee);

                if (subtractFeesFromRecipients)
                    builder = builder.SubtractFees();


                return builder.BuildTransaction(sign: true);
            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}
