using STTech.BytesIO.Tcp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApeFree.Protocol.ApeFtp.Demo
{
    public partial class Form1 : Form
    {
        private ApeFtpSender ftpSender;
        private ApeFtpReceiver ftpReceiver;
        private TcpClient client;
        private TcpServer server;
        public Form1()
        {
            InitializeComponent();
            TcpClient client = new TcpClient();
            TcpServer server = new TcpServer();
            server.Port = 45555;
            server.StartAsync();
            client.Port = server.Port;
            client.Host = "127.0.0.1";
            client.Connect();
            server.ClientConnected += Server_ClientConnected;
            client.OnDataReceived += Client_OnDataReceived1;
            ftpSender = new ApeFtpSender((bytes) => client.Send(bytes));
            ftpReceiver = new SimpleReceiver((bytes) => server.Clients.First().Send(bytes))
            {
                TransferCachePath = @"C:\Users\16023\Desktop\a",
            };
        }

        private void Client_OnDataReceived1(object sender, STTech.BytesIO.Core.DataReceivedEventArgs e)
        {
            ftpSender.Input(e.Data);
        }

        private void Server_ClientConnected(object sender, STTech.BytesIO.Tcp.ClientConnectedEventArgs e)=> e.Client.OnDataReceived += Client_OnDataReceived;
        private void Client_OnDataReceived(object sender, STTech.BytesIO.Core.DataReceivedEventArgs e) => ftpReceiver.Input(e.Data);

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var filePath = openFileDialog.FileName;

                    ftpSender.SendFile(filePath);
                }
            }
        }
    }
}
