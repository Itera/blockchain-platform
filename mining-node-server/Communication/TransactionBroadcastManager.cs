using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mining_node_server.Blockchain;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mining_node_server.Communication
{
    public class TransactionBroadcastManager
    {
        private IConfiguration config;
        private ILogger<TransactionBroadcastManager> logger;
        private ISubscriptionClient subscriptionClient;
        private BlockchainRepository blockchainRepository;

        public TransactionBroadcastManager(IConfiguration config, 
            ILogger<TransactionBroadcastManager> logger,
            BlockchainRepository blockchainRepository)
        {
            this.config = config;
            this.logger = logger;
            this.blockchainRepository = blockchainRepository;

            subscriptionClient = new SubscriptionClient(config.GetConnectionString("AzureServiceBus"), config["BlockchainNodeSettings:BlockchainTopicName"], config["BlockchainNodeSettings:NodeName"]);

        }

        public async void BroadcastNewBlock(Block block)
        {
            try
            {
                ITopicClient topicClient = new TopicClient(config.GetConnectionString("AzureServiceBus"), config["BlockchainNodeSettings:BlockchainTopicName"]);

                string messageBody = JsonConvert.SerializeObject(block);
                var message = new Message(Encoding.UTF8.GetBytes(messageBody));
                message.UserProperties["Sender"] = config["BlockchainNodeSettings:NodeName"];

                Console.WriteLine($"Sending message: {messageBody}");
                await topicClient.SendAsync(message);
                topicClient.CloseAsync();
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }

        public void RegisterOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
       
                MaxConcurrentCalls = 1,

                AutoComplete = true
            };

            subscriptionClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            var messageString = Encoding.UTF8.GetString(message.Body);
            logger.LogInformation("Recieved new block");
            logger.LogInformation(messageString);

            var jsonstring = Encoding.UTF8.GetString(message.Body);
            var block = JsonConvert.DeserializeObject<Block>(jsonstring);

            var lastBlock = blockchainRepository.GetLastBlock();


            if (block.index > lastBlock.index)
           {
                blockchainRepository.Add(block);
           }

        }

        Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            return Task.CompletedTask;
        }
    }
}
