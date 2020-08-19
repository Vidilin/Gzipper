using GZipTest.Classes;
using GZipTest.Enums;
using GZipTest.Interfaces;
using System;
using System.Threading;

namespace GZipTest.Abstract
{
    public abstract class Archiver : IArchiver
    {
        protected bool isTerminated = false;
        protected bool hasErrors = false;
        protected bool needProcessInfo;
        protected static readonly int threadsCount = Environment.ProcessorCount;
        private static readonly long limitBytes = 32000000000; //Максимальный размер файла
        protected readonly int blockSize = 1000000;//1048576; //Размер блока МБ/МиБ
        protected IBlocksWriteBuffer writeBuffer = new BlocksWriteBuffer(1000);
        protected AutoResetEvent[] doneEvents = new AutoResetEvent[threadsCount];
        protected AutoResetEvent waitEndOfWriteHandle = new AutoResetEvent(false);
        protected readonly string sourceFileName, destinationFileName;
        protected string modeMessage;

        public Archiver(string sourceFileName, string destinationFileName, bool needProcessInfo = false)
        {
            this.sourceFileName = sourceFileName;
            this.destinationFileName = destinationFileName;
            this.needProcessInfo = needProcessInfo;
        }

        public long GetLimitSize()
        {
            return limitBytes;
        }

        public void Cancel()
        {
            isTerminated = true;
        }

        public int CallBackResult()
        {
            if (!isTerminated)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        public void Launch()
        {
            DateTime start = DateTime.Now;

            Console.WriteLine($"{modeMessage}...");

            for (int i = 0; i < threadsCount; i++)
            {
                doneEvents[i] = new AutoResetEvent(false);
                Thread workThread = new Thread(new ParameterizedThreadStart(ReadWork));
                workThread.Name = $"Work thread number {i}";
                workThread.Start(i);
            }

            var writeThread = new Thread(new ThreadStart(Write));
            writeThread.Name = "WriteThread";
            writeThread.Start();

            //WaitHandle.WaitAll(doneEvents);
            //waitEndOfWriteHandle.WaitOne();
            writeThread.Join();

            if (!isTerminated && !hasErrors)
            {
                TimeSpan execTime = DateTime.Now - start;
                Console.WriteLine($"{modeMessage} has been succesfully finished. Total seconds - {Math.Floor(execTime.TotalSeconds)}");
            }
        }

        protected abstract void ReadWork(object reader);
        protected abstract void Write();
        public abstract WorkMode GetMode();
    }
}

