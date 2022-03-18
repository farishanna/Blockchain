using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainAssignment
{
    class Blockchain
    {
        // List of block objects forming the blockchain
        public List<Block> blocks;

        // Maximum number of transactions per block
        private int transactionsPerBlock = 5;

        // List of pending transactions to be mined
        public List<Transaction> transactionPool = new List<Transaction>();

        // Default Constructor - initialises the list of blocks and generates the genesis block
        public Blockchain()
        {
            blocks = new List<Block>()
            {
                new Block() // Create and append the Genesis Block
            };
        }

        // Prints the block at the specified index to the UI
        public String GetBlockAsString(int index)
        {
            // Check if referenced block exists
            if (index >= 0 && index < blocks.Count)
                return blocks[index].ToString(); // Return block as a string
            else
                return "No such block exists";
        }

        // Retrieves the most recently appended block in the blockchain
        public Block GetLastBlock()
        {
            return blocks[blocks.Count - 1];
        }

        // Retrieve pending transactions and remove from pool
        public List<Transaction> GetPendingTransactions(String mineSetting, String address)
        {
            List<Transaction> transactions = new List<Transaction>();
            // Determine the number of transactions to retrieve dependent on the number of pending transactions and the limit specified
            int n = Math.Min(transactionsPerBlock, transactionPool.Count);

            /* Gets and Removes Ranges - Then returns to original order */
            void GetAndRemoveRanges(bool returnOrder = false)
            {
                // "Pull" transactions from the transaction list (modifying the original list)
                transactions = transactionPool.GetRange(0, n);
                transactionPool.RemoveRange(0, n);
                if (returnOrder)
                {
                    // Returns pool to original order
                    transactionPool = transactionPool.OrderBy(x => x.timestamp).ToList();
                }
            }

            /* Gets transactions of sender or recipient when specified */
            void HandleAddressPreference(String addressPreference)
            {
                // Get the transactions where specified
                if (addressPreference == "SenderAddress")
                {
                    transactions = transactionPool.Where(x => (x.senderAddress == address)).ToList();
                }
                else if (addressPreference == "RecipientAddress")
                {
                    transactions = transactionPool.Where(x => (x.recipientAddress == address)).ToList();
                }
                // Take remaining transactions
                if (transactions.Count < transactionsPerBlock)
                {
                    List<Transaction> remainingTransactions = new List<Transaction>();
                    remainingTransactions = transactionPool.Where(x => !transactions.Any(t => t == x)).ToList();
                    remainingTransactions = remainingTransactions.Take(transactionsPerBlock - transactions.Count).ToList();
                    transactions.AddRange(remainingTransactions);
                }
                else
                {
                    transactions = transactions.GetRange(0, n);
                }
                // Remove transactions from pool
                transactionPool = transactionPool.Except(transactions).ToList();
            }

            // By default, the method already has a alrtuistic setting
            if (transactionPool.Count > transactionsPerBlock && mineSetting != "Altruistic")
            {
                if (mineSetting == "Greedy")
                {
                    // Takes the highest fees
                    transactionPool = transactionPool.OrderByDescending(x => x.fee).ToList();
                    GetAndRemoveRanges(true);
                }
                else if (mineSetting == "Random")
                {
                    // Takes random fees
                    Random random = new Random();
                    transactionPool = transactionPool.OrderBy(x => random.Next()).ToList();
                    GetAndRemoveRanges(true);
                }
                // Handles address preference
                else if (mineSetting == "Sender Address")
                {
                    HandleAddressPreference("SenderAddress");
                }
                else if (mineSetting == "Reciever Address")
                {
                    HandleAddressPreference("RecipientAddress");
                }
            }
            else
            {
                GetAndRemoveRanges();
            }
            // Return the extracted transactions
            return transactions;
        }

        // Check validity of a blocks hash by recomputing the hash and comparing with the mined value
        public static bool ValidateHash(Block b)
        {
            String rehash = b.CreateHash(b.nonce);
            return rehash.Equals(b.hash);
        }

        // Check validity of the merkle root by recalculating the root and comparing with the mined value
        public static bool ValidateMerkleRoot(Block b)
        {
            String reMerkle = Block.MerkleRoot(b.transactionList);
            return reMerkle.Equals(b.merkleRoot);
        }

        // Check the balance associated with a wallet based on the public key
        public double GetBalance(String address)
        {
            // Accumulator value
            double balance = 0;

            // Loop through all approved transactions in order to assess account balance
            foreach(Block b in blocks)
            {
                foreach(Transaction t in b.transactionList)
                {
                    if (t.recipientAddress.Equals(address))
                    {
                        balance += t.amount; // Credit funds recieved
                    }
                    if (t.senderAddress.Equals(address))
                    {
                        balance -= (t.amount + t.fee); // Debit payments placed
                    }
                }
            }
            return balance;
        }

        // Output all blocks of the blockchain as a string
        public override string ToString()
        {
            return String.Join("\n", blocks);
        }
    }
}
