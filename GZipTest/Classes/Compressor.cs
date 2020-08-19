using GZipTest.Abstract;
using GZipTest.Classes.Data;
using GZipTest.Enums;
using GZipTest.Interfaces;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipTest.Classes
{
    public class Compressor : Archiver
    {
        private IReader reader;
        private int lastWritedBlockId; //Последний записанный блок для отслеживания очередности записи

        public Compressor(string sourceFileName, string destinationFileName, bool needProcessInfo = false) 
            : base(sourceFileName, destinationFileName, needProcessInfo)
        {
            this.modeMessage = "Compress";
            this.reader = new CompressReader(sourceFileName, blockSize);
            lastWritedBlockId = 0;
        }

        public override WorkMode GetMode()
        {
            return WorkMode.Compress;
        }

        protected override void ReadWork(object i)
        {
            try
            {
                while (!isTerminated && reader.IsWorking())
                {
                    DataBlock incomingBlock = reader.GetDataBlock();

                    if (incomingBlock == null)
                    {
                        break;
                    }

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (GZipStream compressStream = new GZipStream(memoryStream, CompressionMode.Compress))
                        {
                            compressStream.Write(incomingBlock.Data, 0, incomingBlock.Data.Length);
                        }

                        byte[] compressedData = memoryStream.ToArray();
                        DataBlock outcomingBlock = new DataBlock(incomingBlock.Id, compressedData);
                        writeBuffer.AddBlock(outcomingBlock);
                        if (needProcessInfo)
                            Console.WriteLine("Block {0} processed by {1} thread", outcomingBlock.Id, Thread.CurrentThread.Name);
                    }
                }

                if (needProcessInfo)
                    Console.WriteLine("Thread {0} finish work", Thread.CurrentThread.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                isTerminated = hasErrors = true;
            }
            finally
            {
                AutoResetEvent doneEvent = doneEvents[(int)i];
                doneEvent.Set();
            }
        }

        protected override void Write()
        {
            try
            {               
                using (FileStream destinationStream = new FileStream($"{destinationFileName}.gz", FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    do
                    {
                        DataBlock outcomingBlock = writeBuffer.GetBlock();

                        if (outcomingBlock == null)
                        {
                            //Если поток запросит блок раньше, чем появились готовые блоки
                            // TODO: подумать как обойти
                            continue;
                        }

                        if (outcomingBlock.Id != lastWritedBlockId + 1)
                        {
                            //очередность блоков (Если какой-нибудь поток обработал следующий блок раньше предыдущего)
                            continue;
                        }

                        BitConverter.GetBytes(outcomingBlock.Data.Length).CopyTo(outcomingBlock.Data, 4);
                        byte[] buffer = outcomingBlock.Data;
                        BitConverter.GetBytes(buffer.Length).CopyTo(buffer, 4);
                        destinationStream.Write(buffer, 0, buffer.Length);

                        //destinationStream.Write(outcomingBlock.Data, 0, outcomingBlock.Data.Length);

                        lastWritedBlockId = outcomingBlock.Id;
                        writeBuffer.DeleteBlock(outcomingBlock.Id);

                        if (needProcessInfo)
                            Console.WriteLine("Block {0} writed by thread {1}, id {2}",
                                outcomingBlock.Id, Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId);
                    } while (!isTerminated && (reader.IsWorking() || reader.BlocksRead() > lastWritedBlockId || writeBuffer.GetCurrentBlocksCount() > 0));

                    if (needProcessInfo)
                        Console.WriteLine("Thread {0} finish work", Thread.CurrentThread.Name);                 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                isTerminated = hasErrors = true;
            }
            finally
            {
                waitEndOfWriteHandle.Set();
            }
        }
    }
}
