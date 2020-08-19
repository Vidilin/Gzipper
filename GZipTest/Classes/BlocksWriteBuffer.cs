using GZipTest.Classes.Data;
using GZipTest.Classes.Events;
using GZipTest.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GZipTest.Classes
{

    public class BlocksWriteBuffer : IBlocksWriteBuffer
    {
        private readonly object locker = new object();
        private readonly int maxSize; //Максимальное число хранимых элементов
        SortedDictionary<int, DataBlock> buffer; 
        public event EventHandler<NewBlockEventArgs> BlockAdded;

        public BlocksWriteBuffer(int maxSize)
        {
            buffer = new SortedDictionary<int, DataBlock>();
            this.maxSize = maxSize;
        }

        protected void OnNewBlock(NewBlockEventArgs e)
        {
            // Сохранить ссылку на делегата во временной переменной
            // для обеспечения безопасности потоков
            EventHandler<NewBlockEventArgs> temp = BlockAdded;
            // Если есть объекты, зарегистрированные для получения
            // уведомления о событии, уведомляем их
            if (temp != null) temp(this, e);
        }

        public void AddBlock(DataBlock block)
        {
            try
            {
                Monitor.Enter(locker);

                //if (isStopped)
                //    throw new InvalidOperationException(terminatedExeption);

                buffer.Add(block.Id, block);

                Monitor.PulseAll(locker);

                OnNewBlock(new NewBlockEventArgs(block.Id));
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

                if (buffer.Count > 0)
                {
                    var block = buffer.First();

                    return block.Value;
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

        public void DeleteBlock(int id)
        {
            try
            {
                Monitor.Enter(locker);
                if (buffer.ContainsKey(id))
                    buffer.Remove(id);
                Monitor.PulseAll(locker);
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }

        public int GetCurrentBlocksCount()
        {
            return buffer.Count();
        }

        public bool IsFull()
        {
            return maxSize <= buffer.Count();
        }
    }
}
