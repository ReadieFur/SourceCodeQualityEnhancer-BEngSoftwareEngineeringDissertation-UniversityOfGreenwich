using System;

namespace ReadieFur.SourceAnalyzer.VSIX.Helpers
{
    internal class SerializedArraySizeAttribute : Attribute
    {
        public uint Size { get; }

        public SerializedArraySizeAttribute(uint size)
        {
            Size = size;
        }
    }
}
