using System;

namespace GZipTest.Classes.Events
{
    public class NewBlockEventArgs : EventArgs
    {
        private readonly int blockId;

        public NewBlockEventArgs(int blockId)
        {
            this.blockId = blockId;
        }

        public int GetBlockId => blockId;
    }
}
