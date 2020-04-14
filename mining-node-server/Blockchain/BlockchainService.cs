using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mining_node_server.Communication;
using System;
using System.Collections.Generic;
using System.Text;

namespace mining_node_server.Blockchain
{
    public class BlockchainService
    {
        private IConfiguration config;
        private ILogger<BlockchainService> logger;
        private AzureServiceBusManager serviceBusManger;
        private QueueSender queueSender;
        private BlockchainRepository blockchainRepository;

        public BlockchainService(IConfiguration config, 
            ILogger<BlockchainService> logger,
            AzureServiceBusManager serviceBusManager,
            QueueSender queueSender,
            BlockchainRepository blockchainRepository)
        {
            this.config = config;
            this.logger = logger;
            this.serviceBusManger = serviceBusManager;
            this.queueSender = queueSender;
            this.blockchainRepository = blockchainRepository;
        }

        public void InitBlockchain()
        {
            if (IsOnlyPeer())
            {
                
                logger.LogInformation("Is the only peer");
                logger.LogInformation("Creating new Blockchain and adding Genesis Block");
                blockchainRepository.Add(Block.CreateGenesisBlock());
                return;
            }

            else
            {
                logger.LogInformation("There is other peers in the network");
                logger.LogInformation("Requesting blockchain from other peers");

                var peer = GetRandomPeer();

                var message = new Message(Encoding.UTF8.GetBytes("hello from:" + config["BlockchainNodeSettings:NodeName"]));
                message.UserProperties.Add("Type", MessageType.BlockchainSyncRequest.ToString());
                message.UserProperties.Add("Sender", config["BlockchainNodeSettings:NodeName"]);

                IQueueClient queueClient = new QueueClient(config.GetConnectionString("AzureServiceBus"), peer);
                queueClient.SendAsync(message).GetAwaiter().GetResult();
                queueClient.CloseAsync().GetAwaiter().GetResult();

            }
        }

        public Block AddTransaction(List<Transaction> transactions)
        {
            var lastBlock = blockchainRepository.GetLastBlock();

            var block = Block.MineBlock(lastBlock, transactions);

            blockchainRepository.Add(block);

            return block;
        }


        public List<Block> GetBlockchain()
        {
            return blockchainRepository.Get();
        }
        bool IsOnlyPeer()
        {
            var queues = serviceBusManger.GetNodeQueues();

            if(queues.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        string GetRandomPeer()
        {
            var peers = serviceBusManger.GetNodeQueues();

            Random r = new Random();
            int peerNum = r.Next(0, peers.Count);
            return peers[peerNum];
        }
    }
}
