using GZipTest.Classes.Data;
using GZipTest.Interfaces;
using System.IO;
using System.Threading;

namespace GZipTest.Abstract
{
    public abstract class Reader : IReader
    {
        protected readonly object locker;
        protected readonly string fileName;
        protected readonly int blockSize;
        protected int blockId;
        //private long position;
        protected FileStream stream;

        public Reader(string fileName, int blockSize)
        {
            locker = new object();
            this.fileName = fileName;
            this.blockSize = blockSize;
            stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            blockId = 0;
        }

        public bool IsWorking()
        {
            return stream.CanRead;
        }

        public int BlocksRead()
        {
            try
            {
                Monitor.Enter(locker);
                return blockId;
            }
            finally
            {
                Monitor.Exit(locker);
            }           
        }

        public abstract DataBlock GetDataBlock();
    }
}
