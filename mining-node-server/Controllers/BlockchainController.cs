using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using mining_node_server.Blockchain;
using mining_node_server.Communication;
using mining_node_server.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace mining_node_server.Controllers
{
    [ApiController]
    public class BlockchainController : ControllerBase
    {
        private IConfiguration config;
        private ILogger<BlockchainController> logger;
        private QueueSender queueSender;
        private TransactionBroadcastManager transactionBroadcastManager;
        private BlockchainService blockchainService;

        public BlockchainController(IConfiguration config, 
            ILogger<BlockchainController> logger,
            QueueSender queueSender,
            TransactionBroadcastManager transactionBroadcastManager,
            BlockchainService blockchainService)
        {
            this.config = config;
            this.logger = logger;
            this.queueSender = queueSender;
            this.transactionBroadcastManager = transactionBroadcastManager;
            this.blockchainService = blockchainService;
        }
        [HttpGet]
        [Route("blockchain")]
        public ActionResult GetBlockchain()
        {
            var block = blockchainService.GetBlockchain();
            string json = JsonConvert.SerializeObject(block, Formatting.Indented);

            return Ok(json);
        }

        [HttpPost]
        [Route("transaction")]
        public ActionResult AddTransaction([FromBody] TransactionPostModel transactionInput)
        {
            if(transactionInput == null)
            {
                BadRequest();
            }

            var transaction = new Transaction("sender", transactionInput.Receiver, transactionInput.Ammount);

            var block = blockchainService.AddTransaction(new List<Transaction>() { transaction });
            transactionBroadcastManager.BroadcastNewBlock(block);

            return Ok();

        }
    }
}
