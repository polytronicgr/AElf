﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Kernel.Managers;
using AElf.Kernel.Miner;
using AElf.Kernel.Node.Config;
using AElf.Kernel.Node.Protocol;
using AElf.Kernel.Node.RPC;
using AElf.Kernel.Node.RPC.DTO;
using AElf.Kernel.TxMemPool;
using AElf.Network.Data;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Node
{
    [LoggerName("Node")]
    public class MainChainNode : IAElfNode
    {
        private readonly ITxPoolService _poolService;
        private readonly ITransactionManager _transactionManager;
        private readonly IRpcServer _rpcServer;
        private readonly ILogger _logger;
        private readonly IProtocolDirector _protocolDirector;
        private readonly INodeConfig _nodeConfig;
        private readonly IMiner _miner;

        public MainChainNode(ITxPoolService poolService, ITransactionManager txManager, IRpcServer rpcServer, 
            IProtocolDirector protocolDirector, ILogger logger, INodeConfig nodeConfig, IMiner miner)
        {
            _poolService = poolService;
            _protocolDirector = protocolDirector;
            _transactionManager = txManager;
            _rpcServer = rpcServer;
            _logger = logger;
            _nodeConfig = nodeConfig;
            _miner = miner;
        }

        public void Start(bool startRpc)
        {
            if (startRpc)
                _rpcServer.Start();
            
            _poolService.Start();
            _protocolDirector.Start();
            
            // todo : avoid circular dependency
            _rpcServer.SetCommandContext(this);
            _protocolDirector.SetCommandContext(this);
            
            if(_nodeConfig.IsMiner)
                _miner.Start();    
            
            _logger.Log(LogLevel.Debug, "AElf node started.");
            if (_nodeConfig.IsMiner)
            {
                _logger.Log(LogLevel.Debug, "Chain Id = \"{0}\"", _nodeConfig.ChainId.ToByteString().ToBase64());
                _logger.Log(LogLevel.Debug, "Coinbase = \"{0}\"", _miner.Coinbase.Value.ToBase64());
            }
        }



        /// <summary>
        /// get the tx from tx pool or database
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        public async Task<ITransaction> GetTransaction(Hash txId)
        {
            if (_poolService.TryGetTx(txId, out var tx))
            {
                return tx;
            }
            return await _transactionManager.GetTransaction(txId);
        }

        /// <summary>
        /// This inserts a transaction into the node. Note that it does
        /// not broadcast it to the network and doesn't add it to the
        /// transaction pool. Essentially it just inserts the transaction
        /// in the database.
        /// </summary>
        /// <param name="tx">The transaction to insert</param>
        /// <returns>The hash of the transaction that was inserted</returns>
        public async Task<IHash> InsertTransaction(Transaction tx)
        {
            return await _transactionManager.AddTransactionAsync(tx);
        }

        /// <summary>
        /// Broadcasts a transaction to the network. This method
        /// also places it in the transaction pool.
        /// </summary>
        /// <param name="tx">The tx to broadcast</param>
        public async Task<bool> BroadcastTransaction(ITransaction tx)
        {
            bool res;
            
            try
            {
                res = await _poolService.AddTxAsync(tx);
            }
            catch (Exception e)
            {
                _logger.Trace("Pool insertion failed: " + tx.GetHash().Value.ToBase64());
                return false;
            }

            if (res)
            {
                await _protocolDirector.BroadcastTransaction(tx);
                
                _logger.Trace("Broadcasted transaction to peers: " + tx.GetTransactionInfo());
                return true;
            }
            
            _logger.Trace("Broadcasting transaction failed: { txid: " + tx.GetHash().Value.ToBase64() + " }");
            return false;
        }
        
        /// <summary>
        /// This method processes a transaction received from one of the
        /// connected peers.
        /// </summary>
        /// <param name="messagePayload"></param>
        /// <returns></returns>
        public async Task ReceiveTransaction(ByteString messagePayload)
        {
            try
            {
                Transaction tx = Transaction.Parser.ParseFrom(messagePayload);
                _logger.Trace("Received Transaction: " + JsonFormatter.Default.Format(tx));
                await _poolService.AddTxAsync(tx);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Invalid tx - Could not receive transaction from the network", null);
            }
        }

        /// <summary>
        /// This method requests a specified number of peers from
        /// the node's peer list.
        /// </summary>
        /// <param name="numPeers"></param>
        /// <returns></returns>
        public async Task<List<NodeData>> GetPeers(ushort? numPeers)
        {
            return _protocolDirector.GetPeers(numPeers);
        }
    }
}