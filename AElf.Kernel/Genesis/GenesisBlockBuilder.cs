﻿using System;
using System.Collections.Generic;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Services;
using Google.Protobuf;

namespace AElf.Kernel
{
    public class GenesisBlockBuilder
    {
        public Block Block { get; set; }

        public GenesisBlockBuilder Build()
        {
            var block = new Block(Hash.Zero)
            {
                Header = new BlockHeader
                {
                    Index = 0,
                    PreviousHash = Hash.Zero
                },
                Body = new BlockBody()
            };

            // Genesis block is empty
            // TODO: Maybe add info like Consensus protocol in Genesis block

            block.FillTxsMerkleTreeRootInHeader();
            
            Block = block;

            return this;
        }
    }
}