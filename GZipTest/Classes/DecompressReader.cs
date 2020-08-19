using GZipTest.Abstract;
using GZipTest.Classes.Data;
using System;
using System.Threading;

namespace GZipTest.Classes
{
    public class DecompressReader : Reader
    {
        public DecompressReader(string fileName, int blockSize) : base(fileName, blockSize)
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

                byte[] buffer = new byte[8];
                stream.Read(buffer, 0, buffer.Length);

                //получить длину текущего блока
                int blockLength = BitConverter.ToInt32(buffer, 4);
                byte[] compressedData = new byte[blockLength];

                buffer.CopyTo(compressedData, 0);
                stream.Read(compressedData, 8, blockLength - 8);

                //получить длину блока после распаковки
                int blockLengthOrigin = BitConverter.ToInt32(compressedData, blockLength - 4);

                var block = new DataBlock(++blockId, compressedData, blockLengthOrigin);

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

        ~DecompressReader()
        {
            if (stream != null)
                stream.Dispose();
        }
    }
}
