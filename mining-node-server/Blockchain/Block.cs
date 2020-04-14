using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace mining_node_server.Blockchain
{
    public class Block
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string guid { get; set; }
        public ulong index { get; set; }
        public DateTime timestamp { get; set; }
        public string previousHash { get; set; }
        public string hash { get; set; }
        public List<Transaction> transactions { get; set; }

        public int nonce { get; set; }

        const int DIFFICULTY = 3;


        public Block(DateTime timestamp, string previousHash, string hash, List<Transaction> transactions, ulong index, int nonce)
        {
            this.timestamp = timestamp;
            this.previousHash = previousHash;
            this.hash = hash;
            this.transactions = transactions;
            this.index = index;
            this.nonce = nonce;
        }

        public static Block CreateGenesisBlock()
        {
            return new Block(DateTime.MinValue, null, null, new List<Transaction>(){ }, 0, 0);
        }

        public static Block MineBlock(Block previousBlock, List<Transaction> transactions)
        {
            var timestamp = DateTime.UtcNow;
            var previousHash = previousBlock.hash;
            var index = previousBlock.index + 1;

            var nonce = 0;
            string hash = "";
            var difficultyString = DifficultyString();

            while(!hash.StartsWith(DifficultyString(), StringComparison.Ordinal))
            {
                nonce++;
                hash = CalculateHash(index, timestamp, previousHash, transactions, nonce);
            }

            return new Block(timestamp, previousHash, hash, transactions, index, nonce);
        }

        static string CalculateHash(ulong index, DateTime timestamp, string previousHash, List<Transaction> transactions, int nonce)
        {
            var serializedData = JsonConvert.SerializeObject(transactions);

            SHA256 sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes($"{index}-{timestamp}-{previousHash}-{serializedData}-{nonce}");
            var hash = sha256.ComputeHash(bytes);
            
            return Convert.ToBase64String(hash);
        }

        static string DifficultyString()
        {
            string difficultyString = string.Empty;

            for (int i = 0; i < DIFFICULTY; i++)
            {
                difficultyString += "0";
            }

            return difficultyString;
        }

        public override string ToString()
        {
            return $"Block - Timestamp: {this.timestamp} Previous Hash: {this.previousHash} Hash: {this.hash} Transactions: {this.transactions}";
        }
    }
}
