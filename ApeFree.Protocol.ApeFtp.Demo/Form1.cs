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


        public Form1()
        {
            InitializeComponent();

            ftpSender = new ApeFtpSender((bytes)=> ftpReceiver.Input(bytes));
            ftpReceiver = new SimpleReceiver((bytes) => ftpSender.Input(bytes)) {
                TransferCachePath = @"F:\Temp\laji\ApeFtp",
            };
        }

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
