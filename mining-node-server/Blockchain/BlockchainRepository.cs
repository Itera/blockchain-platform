using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;

namespace mining_node_server.Blockchain
{
    public class BlockchainRepository
    {
        private readonly IMongoCollection<Block> blocks;

        public BlockchainRepository(IConfiguration config, ILogger<BlockchainRepository> logger)
        {
            var connectionString = config["MongoDB:connectionString"];
            var client = new MongoClient(connectionString);

            client.DropDatabase(config["MongoDB:database"]);

            var database = client.GetDatabase(config["MongoDB:database"]);
            blocks = database.GetCollection<Block>(config["MongoDB:collection"]);
        }

        public Block Add(Block block)
        {
            blocks.InsertOne(block);
            return block;
        }

        public List<Block> Get() => blocks.Find(block => true).ToList();

        public Block Get(ulong index) => blocks.Find<Block> (block => block.index == index).FirstOrDefault();

        public Block GetLastBlock()
        {
            var sort = Builders<Block>.Sort.Descending("index");
            var filter = Builders<Block>.Filter.Empty;

            return blocks.Find(filter).Sort(sort).FirstOrDefault();
        }
    }
}
