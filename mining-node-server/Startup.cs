using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using mining_node_server.Blockchain;
using mining_node_server.Communication;

namespace mining_node_server
{
    public class Startup
    {
        private ILogger<Startup> logger;


        public Startup(IConfiguration configuration)
        {
            this.logger = logger;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSingleton<AzureServiceBusManager>();
            services.AddSingleton<BlockchainService>();
            services.AddSingleton<NodeQeueueHandler>();
            services.AddSingleton<TransactionBroadcastManager>();
            services.AddSingleton<BlockchainRepository>();
            services.AddSingleton<QueueSender>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime, ILogger<Startup> logger)
        {
            this.logger = logger;

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseRouting();
            applicationLifetime.ApplicationStopping.Register(DeleteQueues);


            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


            var serviceBusManger = app.ApplicationServices.GetService<AzureServiceBusManager>();
            serviceBusManger.CreateQueues();
            serviceBusManger.CreateTopicAndSubscription();

            var blockchainService = app.ApplicationServices.GetService<BlockchainService>();
            blockchainService.InitBlockchain();

            var nodeQueueHandler = app.ApplicationServices.GetService<NodeQeueueHandler>();
            nodeQueueHandler.Init();
            nodeQueueHandler.RegisterOnMessageHandlerAndReceiveMessages();

            var transactionBroadcastManager = app.ApplicationServices.GetService<TransactionBroadcastManager>();
            transactionBroadcastManager.RegisterOnMessageHandlerAndReceiveMessages();


        }

        private void CreateQueue()
        {
            logger.LogInformation("Startup------------------------------");
            ManagementClient managementClient = new ManagementClient(Configuration.GetConnectionString("AzureServiceBus"));

            if (!managementClient.QueueExistsAsync(Configuration["BlockchainNodeSettings:NodeName"]).Result)
            {
                logger.LogInformation("Queue does not exist. Creating new queue....");

                managementClient.CreateQueueAsync(Configuration["BlockchainNodeSettings:NodeName"]).GetAwaiter().GetResult();
            }
            else
            {
                logger.LogInformation("Queue already exist....");
            }

        }

        private void DeleteQueues()
        {
            logger.LogInformation("Shutdown------------------------------");
            ManagementClient managementClient = new ManagementClient(Configuration.GetConnectionString("AzureServiceBus"));
            managementClient.DeleteQueueAsync(Configuration["BlockchainNodeSettings:NodeName"]).GetAwaiter().GetResult();
            managementClient.DeleteSubscriptionAsync(Configuration["BlockchainNodeSettings:BlockchainTopicName"], Configuration["BlockchainNodeSettings:NodeName"]);
        }
    }
}
