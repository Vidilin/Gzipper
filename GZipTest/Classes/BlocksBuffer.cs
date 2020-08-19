using GZipTest.Classes.Data;
using GZipTest.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GZipTest.Classes
{
    public class BlocksBuffer : IBlocksBuffer
    {
        private readonly object locker;
        Queue<DataBlock> queue;
        private int blockId;
        private bool isWork;

        public BlocksBuffer()
        {
            locker = new object();
            queue = new Queue<DataBlock>();
            blockId = 0;
            isWork = true;
        }

        public void AddBlock(byte[] data, int dataSize)
        {
            try
            {
                Monitor.Enter(locker);

                //if (!isWork)
                //    throw new InvalidOperationException(terminatedExeption);

                DataBlock block = new DataBlock(++blockId, data, dataSize);
                queue.Enqueue(block);

                Monitor.PulseAll(locker);
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }

        public DataBlock GetBlock()
        {
            try
            {
                Monitor.Enter(locker);

                if (queue.Count > 0)
                {
                    return queue.Dequeue();
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }

        public void FinishWork()
        {
            try
            {
                Monitor.Enter(locker);
                isWork = false;
                Monitor.PulseAll(locker);
            }
            finally
            {
                Monitor.Exit(locker);
            }                       
        }

        public int GetBlocksCount()
        {
            return blockId;
        }

        public bool IsWork()
        {
            return isWork;
        }

        public int GetCurrentBlocksCount()
        {
            return queue.Count();
        }
    }
}
