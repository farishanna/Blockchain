using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlockchainAssignment
{
    class Block
    {
        /* Block Variables */
        private DateTime timestamp; // Time of creation

        private int index; // Position of the block in the sequence of blocks

        public String prevHash, // A reference pointer to the previous block
            hash, // The current blocks "identity"
            merkleRoot,  // The merkle root of all transactions in the block
            minerAddress; // Public Key (Wallet Address) of the Miner

        public List<Transaction> transactionList; // List of transactions in this block
        
        // Proof-of-work
        public long nonce = 0; // Number used once for Proof-of-Work and mining
        public long nonce2 = 1; // Second nonce for multi threading

        public Boolean mined = false; // Turns true once nonce is found

        /* Dynamic Difficulty */
        public int difficulty; // difficulty as a variable
        TimeSpan diff; // Getting the time difference between blocks for dynamic difficulty

        // Rewards
        public double reward; // Simple fixed reward established by "Coinbase"
        

        /* Genesis block constructor */
        public Block()
        {
            timestamp = DateTime.Now;
            index = 0;
            transactionList = new List<Transaction>();
            difficulty = 2;

            Thread thread1 = new Thread(Mine); // First thread Mine
            Thread thread2 = new Thread(Mine2); // Second thread Mine2
            // Starts threads
            thread1.Start();
            thread2.Start();
        }

        /* New Block constructor */
        public Block(Block lastBlock, List<Transaction> transactions, String minerAddress)
        {
            timestamp = DateTime.Now;

            index = lastBlock.index + 1;
            prevHash = lastBlock.hash;

            this.minerAddress = minerAddress; // The wallet to be credited the reward for the mining effort
            reward = 1.0; // Assign a simple fixed value reward

            difficulty = lastBlock.difficulty; // Get the last difficulty to compare
            diff = timestamp - lastBlock.timestamp; // for dynamic difficulty to get the time difference from the last block
            // if the time difference is less than 10 seconds, then inrease difficulty
            if (diff < TimeSpan.FromSeconds(10)) { difficulty++; }
            // if time difference is more than 10 seconds, then decrease difficulty
            else if (diff > TimeSpan.FromSeconds(10)) { difficulty--; }
            // minimum difficulty of 2
            if (difficulty < 2) { difficulty = 2; }
            // maximum difficulty of 5
            else if (difficulty > 5) { difficulty = 5; }

            transactions.Add(createRewardTransaction(transactions)); // Create and append the reward transaction
            transactionList = new List<Transaction>(transactions); // Assign provided transactions to the block

            merkleRoot = MerkleRoot(transactionList); // Calculate the merkle root of the blocks transactions

            Thread thread1 = new Thread(Mine); // First thread Mine
            Thread thread2 = new Thread(Mine2); // Second thread Mine2
            // Starts threads
            thread1.Start();
            thread2.Start();
        }

        /* Hashes the entire Block object */
        public String CreateHash(long nonce)
        {
            String hash = String.Empty;
            SHA256 hasher = SHA256Managed.Create();

            /* Concatenate all of the blocks properties including nonce as to generate a new hash on each call */
            String input = timestamp.ToString() + index + prevHash + nonce + merkleRoot;

            /* Apply the hash function to the block as represented by the string "input" */
            Byte[] hashByte = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));

            /* Reformat to a string */
            foreach (byte x in hashByte)
                hash += String.Format("{0:x2}", x);
            
            return hash;
        }

        public void Mine()
        {
            String zeros = new String('0', difficulty);
            while (!mined)
            {
                hash = CreateHash(nonce);
                if (hash.Substring(0, difficulty) != zeros)
                {
                    nonce += 2;
                }
                else
                {
                    mined = true;
                    return;
                }
            }
        }

        /* For Multi-Threading, Another mine function was made */
        public void Mine2()
        {
            String zeros = new String('0', difficulty);
            while (!mined)
            {
                hash = CreateHash(nonce2);
                if (hash.Substring(0, difficulty) != zeros)
                {
                    nonce2 += 2;
                }
                else
                {
                    nonce = nonce2;
                    mined = true;
                    return;
                }
            }
        }


        // Merkle Root Algorithm - Encodes transactions within a block into a single hash
        public static String MerkleRoot(List<Transaction> transactionList)
        {
            List<String> hashes = transactionList.Select(t => t.hash).ToList(); // Get a list of transaction hashes for "combining"
            
            // Handle Blocks with...
            if (hashes.Count == 0) // No transactions
            {
                return String.Empty;
            }
            if (hashes.Count == 1) // One transaction - hash with "self"
            {
                return HashCode.HashTools.combineHash(hashes[0], hashes[0]);
            }
            while (hashes.Count != 1) // Multiple transactions - Repeat until tree has been traversed
            {
                List<String> merkleLeaves = new List<String>(); // Keep track of current "level" of the tree

                for (int i=0; i<hashes.Count; i+=2) // Step over neighbouring pair combining each
                {
                    if (i == hashes.Count - 1)
                    {
                        merkleLeaves.Add(HashCode.HashTools.combineHash(hashes[i], hashes[i])); // Handle an odd number of leaves
                    }
                    else
                    {
                        merkleLeaves.Add(HashCode.HashTools.combineHash(hashes[i], hashes[i + 1])); // Hash neighbours leaves
                    }
                }
                hashes = merkleLeaves; // Update the working "layer"
            }
            return hashes[0]; // Return the root node
        }

        // Create reward for incentivising the mining of block
        public Transaction createRewardTransaction(List<Transaction> transactions)
        {
            double fees = transactions.Aggregate(0.0, (acc, t) => acc + t.fee); // Sum all transaction fees
            return new Transaction("Mine Rewards", minerAddress, (reward + fees), 0, ""); // Issue reward as a transaction in the new block
        }

        /* Concatenate all properties to output to the UI */
        public override string ToString()
        {
            return "[BLOCK START]"
                + "\nIndex: " + index
                + "\tTimestamp: " + timestamp
                + "\nPrevious Hash: " + prevHash
                + "\n-- PoW --"
                + "\nDifficulty Level: " + difficulty
                + "\nNonce: " + nonce
                + "\nHash: " + hash
                + "\n-- Rewards --"
                + "\nReward: " + reward
                + "\nMiners Address: " + minerAddress
                + "\n-- " + transactionList.Count + " Transactions --"
                +"\nMerkle Root: " + merkleRoot
                + "\n" + String.Join("\n", transactionList)
                + "\n[BLOCK END]";
        }
    }
}
