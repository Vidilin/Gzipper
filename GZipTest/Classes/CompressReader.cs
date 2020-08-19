using GZipTest.Abstract;
using GZipTest.Classes.Data;
using System.Threading;

namespace GZipTest.Classes
{
    public class CompressReader : Reader
    {
        public CompressReader (string fileName, int blockSize) : base (fileName, blockSize)
        {
            
        }

        public override DataBlock GetDataBlock()
        {
            try
            {
                Monitor.Enter(locker);

                if (!stream.CanRead)
                {
                    return null;
                }

                ++blockId;

                int bytesRead;
                if (stream.Length - stream.Position <= blockSize)
                {
                    bytesRead = (int)(stream.Length - stream.Position);
                }
                else
                {
                    bytesRead = blockSize;
                }
                byte[] data = new byte[bytesRead];
                stream.Read(data, 0, bytesRead);
                DataBlock block = new DataBlock(blockId, data, bytesRead);

                if (stream.Position == stream.Length)
                {
                    stream.Close();
                }

                return block;
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }

        ~CompressReader()
        {
            if (stream != null)
                stream.Dispose();
        }
    }
}
