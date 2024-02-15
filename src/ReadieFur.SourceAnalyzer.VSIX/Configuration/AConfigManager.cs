using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace ReadieFur.SourceAnalyzer.VSIX.Configuration
{
    internal abstract class AConfigManager : IConfigManager, IDisposable
    {
        protected const int MaxBufferSize = 1024;

        protected object _lock { get; private set; } = new object();

        private MemoryMappedFile _sharedMemory;

        protected MemoryMappedViewAccessor SharedMemoryAccessor;

        protected ConfigRoot? CachedConfiguration { get; set; } = null;

        public AConfigManager(string ipcName)
        {
            _sharedMemory = MemoryMappedFile.CreateOrOpen(ipcName + "_memory", MaxBufferSize);
            SharedMemoryAccessor = _sharedMemory.CreateViewAccessor();
        }

        public virtual void Dispose()
        {
            _sharedMemory.Dispose();
        }

        protected string ReadSharedMemory()
        {
            ushort size = SharedMemoryAccessor.ReadUInt16(54);
            byte[] buffer = new byte[size];
            SharedMemoryAccessor.ReadArray(54 + 2, buffer, 0, buffer.Length);
            return Encoding.ASCII.GetString(buffer);
        }

        protected void WriteSharedMemory(string data)
        {
            byte[] Buffer = Encoding.ASCII.GetBytes(data);
            SharedMemoryAccessor.Write(54, (ushort)Buffer.Length);
            SharedMemoryAccessor.WriteArray(54 + 2, Buffer, 0, Buffer.Length);
        }

        public abstract ConfigRoot GetConfiguration();
    }
}
