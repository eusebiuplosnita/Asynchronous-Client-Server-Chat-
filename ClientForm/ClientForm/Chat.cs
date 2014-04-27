using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace ClientForm
{
    public partial class Chat : Form
    {
        public string friend;
        public string conversation;
        public Socket clientSocket;
        public string username;

        byte[] byteData;

        public Chat(Socket clientSocket)
        {
            InitializeComponent();
            this.clientSocket = clientSocket;
            conversation = null;
            
        }

        private void Chat_Load(object sender, EventArgs e)
        {
            richTextBox1.Text = conversation;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            byteData = new byte[1024];
            if (e.KeyCode == Keys.Enter && textBox1.Text != null)
            {
                byteData = Encoding.ASCII.GetBytes("5|"+ username + "|" + friend+ "|: " + textBox1.Text);
                clientSocket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnSend), null);

                conversation += "\n" +this.username+": "+ textBox1.Text;
                textBox1.Text = null;
                richTextBox1.Text = conversation;
            }
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndSend(ar);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "clientTCPchat: " + username, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        

       


    }
}
