//#define DO_SOLUTION_EVENT_CHECKS

using ReadieFur.SourceAnalyzer.VSIX.Helpers;
using System;
using System.Runtime.Serialization;

namespace ReadieFur.SourceAnalyzer.VSIX.Configuration
{
    //I recall having to do something more complicated in order for the marshalling/serialization to work correctly but I can't seem to find where I did it on my dev branch of this project:
    //https://github.com/ReadieFur/CreateProcessAsUser/tree/development/src/CreateProcessAsUser.Shared
    //If I recall correctly it was something to do with defining the block size to 4 or something for unmanaged compatibility and consistency between platforms.
    [Serializable]
    internal struct SSharedConfigMessage : ISerializable
    {
        #region Configuration
        public Boolean RequestConfiguration = false;

        //https://github.com/ReadieFur/CreateProcessAsUser/blob/development/src/CreateProcessAsUser.Shared/SCredentials.cs
        //Causes the string to be marshalled as a char array of the specified size.
        //https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation?tabs=registry
        [SerializedArraySize(260)]
        public Char[] ConfigurationPath = new Char[260];
        private volatile UInt16 ConfigurationPath_size = 0;
        #endregion

        #region Solution events
#if DO_SOLUTION_EVENT_CHECKS
        public Boolean SolutionUpdated = false; //TODO: Use for dynamic configuration reloading (I don't belive this can be done anyway without restarting the IDE).
#endif
        #endregion

        public SSharedConfigMessage() { }

        public SSharedConfigMessage(SerializationInfo info, StreamingContext context) =>
            this.FromCustomSerializedData(ref info);

        public void GetObjectData(SerializationInfo info, StreamingContext context) =>
            this.ToCustomSerializedData(ref info);
    }
}
