using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mining_node_server.Blockchain;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace mining_node_server.Communication
{
    public class NodeQeueueHandler
    {
        private readonly ILogger<NodeQeueueHandler> logger;
        private IConfiguration config;
        private QueueSender queueSender;
        private BlockchainRepository blockchainRepository;
        private IQueueClient queueClient;

        public NodeQeueueHandler(IConfiguration config, 
            ILogger<NodeQeueueHandler> logger,
            QueueSender queueSender,
            BlockchainRepository blockchainRepository)
        {
            this.logger = logger ?? throw new ArgumentException(nameof(logger));
            this.config = config ?? throw new ArgumentException(nameof(config));
            this.queueSender = queueSender;
            this.blockchainRepository = blockchainRepository;

            this.logger.LogInformation("Constructed Singelton");
        }

        public void Init()
        {
            queueClient = new QueueClient(config.GetConnectionString("AzureServiceBus"), config["BlockchainNodeSettings:NodeName"]);
        }
        public void RegisterOnMessageHandlerAndReceiveMessages()
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = true
            };

            queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        private async Task ProcessMessagesAsync(Message message, CancellationToken token)
        {
            logger.LogInformation("Recieved a message of type: " + message.UserProperties["Type"].ToString());

            string messageTypeString = message.UserProperties["Type"].ToString();
            string sender = message.UserProperties["Sender"].ToString();


            switch (Enum.Parse(typeof(MessageType), messageTypeString))
            {
                case MessageType.BlockchainSyncRequest:
                    Console.WriteLine("Get sync request");

                    var messageList = new List<Message>();
                    foreach (Block b in blockchainRepository.Get())
                    {
                        var objectString = JsonConvert.SerializeObject(b);
                        var m = new Message(Encoding.UTF8.GetBytes(objectString));
                        m.UserProperties.Add("Sender", config["BlockchainNodeSettings:NodeName"]);
                        m.UserProperties.Add("Type", MessageType.BlockchainSyncResponse.ToString());
                        //m.MessageId = b.index.ToString();

                        messageList.Add(m);
                    }

                    IQueueClient queueClient = new QueueClient(config.GetConnectionString("AzureServiceBus"), sender);
                    await queueClient.SendAsync(messageList);

                    await queueClient.CloseAsync();

                    break;

                case MessageType.BlockchainSyncResponse:
                    Console.WriteLine(message.Body);

                    var jsonstring = Encoding.UTF8.GetString(message.Body);

                    var block = JsonConvert.DeserializeObject<Block>(jsonstring);
                    blockchainRepository.Add(block);

                    break;
            }

        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            logger.LogError(exceptionReceivedEventArgs.Exception, "Message handler encountered an exception");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            logger.LogDebug($"- Endpoint: {context.Endpoint}");
            logger.LogDebug($"- Entity Path: {context.EntityPath}");
            logger.LogDebug($"- Executing Action: {context.Action}");

            return Task.CompletedTask;
        }

        public async Task CloseQueueAsync()
        {
            await queueClient.CloseAsync();
        }

    }
}
