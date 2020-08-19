using GZipTest.Classes.Data;

namespace GZipTest.Interfaces
{
    public interface IBlocksBuffer
    {
        void AddBlock(byte[] data, int dataSize);
        void FinishWork();
        DataBlock GetBlock();
        int GetBlocksCount();
        int GetCurrentBlocksCount();
        bool IsWork();
    }
}
