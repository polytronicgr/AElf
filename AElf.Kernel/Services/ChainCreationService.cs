﻿using System;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;

namespace AElf.Kernel.Services
{
    public class ChainCreationService : IChainCreationService
    {
        private readonly IChainManager _chainManager;
        private readonly IBlockManager _blockManager;
        private readonly ISmartContractService _smartContractService;

        public ChainCreationService(IChainManager chainManager, IBlockManager blockManager, ISmartContractService smartContractService)
        {
            _chainManager = chainManager;
            _blockManager = blockManager;
            _smartContractService = smartContractService;
        }

        /// <summary>
        /// Creates a new chain with the provided ChainId and Smart Contract Zero.
        /// </summary>
        /// <returns>The new chain async.</returns>
        /// <param name="chainId">The new chain id which will be derived from the creator address.</param>
        /// <param name="smartContractRegistration">Thec smart contract registration containing the code of the SmartContractZero.</param>
        public async Task<IChain> CreateNewChainAsync(Hash chainId, SmartContractRegistration smartContractRegistration)
        {
            // TODO: Centralize this function in Hash class
            // SmartContractZero address can be derived from ChainId
            var contractAddress = chainId.CalculateHashWith("__SmartContractZero__");
            await _smartContractService.DeployContractAsync(contractAddress, smartContractRegistration);
            var builder = new GenesisBlockBuilder();
            builder.Build();

            // add block to storage
            await _blockManager.AddBlockAsync(builder.Block);

            // set height and lastBlockHash for a chain
            await _chainManager.SetChainCurrentHeight(chainId, 0);
            await _chainManager.SetChainLastBlockHash(chainId, builder.Block.GetHash());
            var chain = await _chainManager.AddChainAsync(chainId, builder.Block.GetHash());
            await _chainManager.AppendBlockToChainAsync(chainId, builder.Block);
            return chain;
        }
    }
}