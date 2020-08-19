namespace GZipTest.Classes.Data
{
    public class DataBlock
    {
        public int Id { get; }
        public byte[] Data { get; }
        public int BlockLenghtOrigin { get; }

        public DataBlock(int id, byte[] data) : this (id, data, 0)
        {

        }

        public DataBlock(int id, byte[] data, int blockLengthOrigin)
        {
            Id = id;
            Data = data;
            BlockLenghtOrigin = blockLengthOrigin;
        }
    }
}
