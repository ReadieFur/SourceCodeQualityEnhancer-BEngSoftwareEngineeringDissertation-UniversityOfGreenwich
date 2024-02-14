//#define DO_SOLUTION_EVENT_CHECKS

using CSharpTools.Pipes;
using static CSharpTools.Pipes.Helpers;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.Threading;
using ReadieFur.SourceAnalyzer.VSIX.Helpers;

namespace ReadieFur.SourceAnalyzer.VSIX.Configuration
{
    internal class ClientConfigManager : AConfigManager
    {
        private PipeClient _pipeClient;
        private ManualResetEventSlim _configurationReceivedEvent = new(false);

        public ClientConfigManager(string ipcName) : base(ipcName)
        {
            //My library I believe checks for race conditions and waits for a pipe to exist, however we don't need to check regardless as the host pipe will always be created first by the VSIX extension.
            _pipeClient = new PipeClient(ipcName, ComputeBufferSizeOf<SSharedConfigMessage>());
            _pipeClient.OnMessage += PipeClient_OnMessage;
            _pipeClient.WaitForConnection();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits")]
        private void PipeClient_OnMessage(ReadOnlyMemory<byte> data)
        {
            SSharedConfigMessage message;
            try { message = Deserialize<SSharedConfigMessage>(data.ToArray()); }
            catch { return; }
            
            if (message.RequestConfiguration)
            {
                //A new configuration has been received (response to request), deserialize it and cache it.
                _cachedConfiguration = YamlLoader.LoadAsync(message.ConfigurationPath.FromCharArray()).Result;
                _configurationReceivedEvent.Set();
            }
        }

        public override void Dispose()
        {
            _pipeClient.Dispose();
        }

        public override ConfigRoot GetConfiguration()
        {
            lock (_lock)
            {
                if (_cachedConfiguration is not null)
                    return _cachedConfiguration;

                //Request the configuration from the host.
                SSharedConfigMessage message = new()
                {
                    RequestConfiguration = true
                };
                var a = Serialize(new SSharedConfigMessage());
                var b = Serialize(new SSharedConfigMessage() { RequestConfiguration = true });
                var c = a == b;
                var d = a.Equals(b);
                _pipeClient.SendMessage(Serialize(message));

                //Wait for the configuration to be received.

                _configurationReceivedEvent.Wait(
#if !DEBUG
                    5000 //Allow up to x milliseconds for the configuration to be received, it it takes any longer, assume it failed.
#endif
                 );
                _configurationReceivedEvent.Reset();

                if (_cachedConfiguration is null)
                    throw new InvalidOperationException("Failed to receive the configuration from the host.");

                return _cachedConfiguration;
            }
        }
    }
}
