using GZipTest.Classes.Data;

namespace GZipTest.Interfaces
{
    public interface IReader
    {
        DataBlock GetDataBlock();
        bool IsWorking();
        int BlocksRead();
    }
}
