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
    public class GZipper : Archiver
    {
        private IReader reader;
        private int lastWritedBlockId; //Последний записанный блок для отслеживания очередности записи
        private readonly WorkMode mode;
        private delegate void WorkMethod(DataBlock incomingBlock);
        private delegate void WriteMethod(FileStream destinationStream);
        private WorkMethod work;
        private WriteMethod write;

        public GZipper(string sourceFileName, string destinationFileName, WorkMode mode, bool needProcessInfo = false) 
            : base(sourceFileName, destinationFileName, needProcessInfo)
        {           
            lastWritedBlockId = 0;
            this.mode = mode;
            this.modeMessage = mode.ToString();
            if (mode == WorkMode.Compress)
            {
                this.reader = new CompressReader(sourceFileName, blockSize);
                work = CompressWork;
                write = CompressWrite;
            }
            else if (mode == WorkMode.Decompress)
            {
                this.reader = new DecompressReader(sourceFileName, blockSize);
                work = DecompressWork;
                write = DecompressWrite;
            }
            else
            {
                throw new InvalidOperationException("Unknown work mode");
            }
        }

        public override WorkMode GetMode()
        {
            return mode;
        }

        protected override void ReadWork(object i)
        {
            try
            {
                while (!isTerminated && reader.IsWorking())
                {
                    if (writeBuffer.IsFull())
                    {
                        //Приостановить поток если очередь на запись заполнена
                        Thread.Sleep(100);
                        continue;
                    }

                    DataBlock incomingBlock = reader.GetDataBlock();

                    if (incomingBlock == null)
                    {
                        break;
                    }

                    work(incomingBlock);
                }

                ProcessFinishedInfo();
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

        private void CompressWork(DataBlock incomingBlock)
        {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (GZipStream compressStream = new GZipStream(memoryStream, CompressionMode.Compress))
                    {
                        compressStream.Write(incomingBlock.Data, 0, incomingBlock.Data.Length);
                    }

                    byte[] compressedData = memoryStream.ToArray();
                    DataBlock outcomingBlock = new DataBlock(incomingBlock.Id, compressedData);
                    writeBuffer.AddBlock(outcomingBlock);

                    BlockProcessedInfo(outcomingBlock);
                }
        }

        private void DecompressWork(DataBlock incomingBlock)
        {
            using (MemoryStream memoryStream = new MemoryStream(incomingBlock.Data))
            {
                using (GZipStream decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    byte[] decompressedData = new byte[incomingBlock.BlockLenghtOrigin];
                    decompressStream.Read(decompressedData, 0, incomingBlock.BlockLenghtOrigin);
                    DataBlock outcomingBlock = new DataBlock(incomingBlock.Id, decompressedData);

                    writeBuffer.AddBlock(outcomingBlock);

                    BlockProcessedInfo(outcomingBlock);
                }
            }
        }

        protected override void Write()
        {
            try
            {
                var fileName = mode == WorkMode.Compress ? $"{destinationFileName}.gz" : destinationFileName;
                using (FileStream destinationStream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    write(destinationStream);
                }                    

                ProcessFinishedInfo();
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

        private void CompressWrite(FileStream destinationStream)
        {
            do
            {
                DataBlock outcomingBlock = writeBuffer.GetBlock();

                if (outcomingBlock == null)
                {
                    //Если поток запросит блок раньше, чем появились готовые блоки
                    continue;
                }

                if (outcomingBlock.Id != lastWritedBlockId + 1)
                {
                    //очередность блоков (Если какой-нибудь поток обработал следующий блок раньше предыдущего)
                    continue;
                }

                //записать длину блока
                BitConverter.GetBytes(outcomingBlock.Data.Length).CopyTo(outcomingBlock.Data, 4);
                //byte[] buffer = outcomingBlock.Data;
                //BitConverter.GetBytes(buffer.Length).CopyTo(buffer, 4);

                destinationStream.Write(outcomingBlock.Data, 0, outcomingBlock.Data.Length);

                lastWritedBlockId = outcomingBlock.Id;
                writeBuffer.DeleteBlock(outcomingBlock.Id);

                BlockWritedInfo(outcomingBlock);

            } while (!isTerminated && (reader.IsWorking() || reader.BlocksRead() > lastWritedBlockId || writeBuffer.GetCurrentBlocksCount() > 0));
        }

        private void DecompressWrite(FileStream destinationStream)
        {
            do
            {
                DataBlock outcomingBlock = writeBuffer.GetBlock();

                if (outcomingBlock == null)
                {
                    //Если поток запросит блок раньше, чем появились готовые блоки
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

                BlockWritedInfo(outcomingBlock);

            } while (!isTerminated && (reader.IsWorking() || reader.BlocksRead() > lastWritedBlockId || writeBuffer.GetCurrentBlocksCount() > 0));
        }

        private void ProcessFinishedInfo()
        {
            if (needProcessInfo)
                Console.WriteLine($"Thread {Thread.CurrentThread.Name} finish work");
        }

        private void BlockProcessedInfo(DataBlock block)
        {
            if (needProcessInfo)
                Console.WriteLine($"Block {block.Id} processed by {Thread.CurrentThread.Name} thread");
        }

        private void BlockWritedInfo(DataBlock block)
        {
            if (needProcessInfo)
                Console.WriteLine($"Block {block.Id} writed by thread {Thread.CurrentThread.Name} thread");
        }
    }
}
