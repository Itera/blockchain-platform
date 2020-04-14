namespace mining_node_server.Blockchain
{
    public class Transaction
    {
        public string SenderAddress { get; set; }
        public string ReceiverAddress { get; set; }
        public int Amount { get; set; }

        public Transaction(string senderAddress, string receiverAddress, int amount)
        {
            SenderAddress = senderAddress;
            ReceiverAddress = receiverAddress;
            Amount = amount;
        }
    }
}
