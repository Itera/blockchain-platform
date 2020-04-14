using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace mining_node_server.Communication
{
    public class QueueSender
    {
        private IConfiguration config;
        private ILogger<NodeQeueueHandler> logger;

        public QueueSender(IConfiguration config, ILogger<NodeQeueueHandler> logger)
        {
            this.config = config;
            this.logger = logger;
        }

        public void Send(string recieverQueue, Message message)
        {
            IQueueClient queueClient = new QueueClient(config.GetConnectionString("AzureServiceBus"), recieverQueue);
            queueClient.SendAsync(message);
            queueClient.CloseAsync();
        }
    }
}
