using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace mining_node_server.Communication
{
    public class AzureServiceBusManager
    {
        private ILogger<NodeQeueueHandler> logger;
        private IConfiguration config;
        private ManagementClient managementClient;

        public AzureServiceBusManager(IConfiguration config, ILogger<NodeQeueueHandler> logger)
        {
            this.logger = logger ?? throw new ArgumentException(nameof(logger));
            this.config = config ?? throw new ArgumentException(nameof(config));

            managementClient = new ManagementClient(config.GetConnectionString("AzureServiceBus"));

        }

        public void CreateQueues()
        {
            if (!managementClient.QueueExistsAsync(config["BlockchainNodeSettings:NodeName"]).Result)
            {
                logger.LogInformation("Queue does not exist. Creating new queue....");

                QueueDescription queueDescription = new QueueDescription(config["BlockchainNodeSettings:NodeName"])
                {
                    RequiresDuplicateDetection = true,
                    DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(60)
                };


                managementClient.CreateQueueAsync(queueDescription).GetAwaiter().GetResult();
            }
            else
            {
                logger.LogInformation("Queue already exist....");
            }
        }

        public void CreateTopicAndSubscription()
        {
            if (!managementClient.TopicExistsAsync(config["BlockchainNodeSettings:BlockchainTopicName"]).Result)
            {
                logger.LogInformation("Topic does not exist. Creating new Topic....");

                managementClient.CreateTopicAsync(config["BlockchainNodeSettings:BlockchainTopicName"]).GetAwaiter().GetResult();
            }
            else
            {
                logger.LogInformation("Topic already exist....");
            }

            if (!managementClient.SubscriptionExistsAsync(config["BlockchainNodeSettings:BlockchainTopicName"], config["BlockchainNodeSettings:NodeName"]).Result)
            {
                logger.LogInformation("Subsription does not exist. Creating new Subsription....");

                SubscriptionDescription subscriptionDescription = new SubscriptionDescription(config["BlockchainNodeSettings:BlockchainTopicName"], config["BlockchainNodeSettings:NodeName"])
                {

                };

                managementClient.CreateSubscriptionAsync(subscriptionDescription).GetAwaiter().GetResult();
            }
            else
            {
                logger.LogInformation("Subsription already exist....");
            }
        }

        public List<string> GetNodeQueues()
        {
            var queues = managementClient.GetQueuesAsync().Result;
            var queuePaths = new List<string>();

            foreach(var q in queues)
            {
                queuePaths.Add(q.Path);
            }

            queuePaths.Remove(config["BlockchainNodeSettings:NodeName"]);

            return queuePaths;
        }



    }
}
