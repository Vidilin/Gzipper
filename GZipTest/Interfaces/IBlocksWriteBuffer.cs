using GZipTest.Classes.Data;
using GZipTest.Classes.Events;
using System;

namespace GZipTest.Interfaces
{
    public interface IBlocksWriteBuffer
    {
        /// <summary>
        /// Return current blocks count
        /// </summary>
        /// <returns></returns>
        int GetCurrentBlocksCount();
        /// <summary>
        /// Delete block by id
        /// </summary>
        /// <param name="id"></param>
        void DeleteBlock(int id);
        /// <summary>
        /// Add new block
        /// </summary>
        /// <param name="block"></param>
        void AddBlock(DataBlock block);
        /// <summary>
        /// Get block on write
        /// </summary>
        /// <returns></returns>
        DataBlock GetBlock();
        event EventHandler<NewBlockEventArgs> BlockAdded;
        bool IsFull();
    }
}
