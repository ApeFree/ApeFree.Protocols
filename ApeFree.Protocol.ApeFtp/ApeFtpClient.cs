using System;
using System.Threading.Tasks;

namespace ApeFree.Protocol.ApeFtp
{
    public abstract class ApeFtpClient
    {
        public Action<byte[]> SendBytesHandler { get; set; }

        // 解包器
        protected ApeFtpUnpacker Unpacker { get; set; }

        protected ApeFtpClient(Action<byte[]> sendBytesHandler)
        {
            SendBytesHandler = sendBytesHandler;

            Unpacker = new ApeFtpUnpacker();
            Unpacker.OnDataParsed += Unpacker_OnDataParsed; ;
        }

        private void Unpacker_OnDataParsed(object sender, STTech.BytesIO.Core.Component.DataParsedEventArgs e)
        {
            Task.Run(() => OnUnpackerDataParsed(sender, e));
        }

        protected abstract void OnUnpackerDataParsed(object sender, STTech.BytesIO.Core.Component.DataParsedEventArgs e);

        public virtual void Input(byte[] data)
        {
            Unpacker.Input(data);
        }
    }
}
