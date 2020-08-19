using GZipTest.Enums;

namespace GZipTest.Interfaces
{
    public interface IArchiver
    {
        void Launch();
        int CallBackResult();
        void Cancel();
        long GetLimitSize();
        WorkMode GetMode();
    }
}
