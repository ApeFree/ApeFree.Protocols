using STTech.BytesIO.Core;
using STTech.BytesIO.Core.Component;
using System;

namespace ApeFree.Protocol.ApeFtp
{
    public abstract class ApeFtpClient : VirtualClient
    {
        /// <summary>
        /// 解包器
        /// </summary>
        public Unpacker<TransferResponse> Unpacker { get; }

        protected ApeFtpClient(BytesClient client)
            : base(client)
        {
            Unpacker = new ApeFtpUnpacker();
            this.BindUnpacker(Unpacker);
            Unpacker.OnDataParsed += Unpacker_OnDataParsed;
        }

        private void Unpacker_OnDataParsed(object sender, DataParsedEventArgs<TransferResponse> e)
        {
            OnUnpackerDataParsed(sender, e);
        }

        protected abstract void OnUnpackerDataParsed(object sender, DataParsedEventArgs<TransferResponse> e);
    }
}
