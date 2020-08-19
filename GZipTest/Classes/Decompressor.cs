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
    public class Decompressor : Archiver
    {
        private IReader reader;
        private int lastWritedBlockId; //Последний записанный блок для отслеживания очередности записи

        public Decompressor(string sourceFileName, string destinationFileName, bool needProcessInfo = false) 
            : base(sourceFileName, destinationFileName, needProcessInfo)
        {
            this.modeMessage = "Decompress";
            this.reader = new DecompressReader(sourceFileName, blockSize);
            lastWritedBlockId = 0;
        }

        public override WorkMode GetMode()
        {
            return WorkMode.Decompress;
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

                    using (MemoryStream memoryStream = new MemoryStream(incomingBlock.Data))
                    {
                        using (GZipStream decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                        {
                            byte[] decompressedData = new byte[incomingBlock.BlockLenghtOrigin];
                            decompressStream.Read(decompressedData, 0, incomingBlock.BlockLenghtOrigin);
                            DataBlock outcomingBlock = new DataBlock(incomingBlock.Id, decompressedData);

                            writeBuffer.AddBlock(outcomingBlock);
                            if (needProcessInfo)
                                Console.WriteLine("Block {0} processed by {1} thread", outcomingBlock.Id, Thread.CurrentThread.Name);
                        }
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
                using (FileStream destinationStream = new FileStream(destinationFileName, FileMode.Append, FileAccess.Write))
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

                        destinationStream.Write(outcomingBlock.Data, 0, outcomingBlock.Data.Length);

                        lastWritedBlockId = outcomingBlock.Id;
                        writeBuffer.DeleteBlock(outcomingBlock.Id);

                        if (needProcessInfo)
                            Console.WriteLine("Block {0} writed by thread {1}",
                                outcomingBlock.Id, Thread.CurrentThread.Name);
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
