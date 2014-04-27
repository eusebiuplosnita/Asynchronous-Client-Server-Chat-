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
    public partial class Form1 : Form
    {
        public Socket clientSocket; //The main client socket

        public List<string> lista;
        public string username;
        public bool logat;

        public List<Chat> chat_rooms;

        private byte[] byteData = new byte[1024];

        public Form1()
        {
            chat_rooms = new List<Chat>();
            this.lista = new List<string>();
            InitializeComponent();
            AfiseazaUseri(lista);
        }

        private delegate void AfiseazaUseriDelegate(List<string> friends);

        public void AfiseazaUseri(List<string> friends)
        {
            int inaltime;
            panel.Controls.Clear();
            if (friends != null)
            {
                if (friends.Count() > 20)
                {
                    inaltime = 594 / friends.Count();
                }
                else
                {
                    inaltime = 30;
                }
                int x = 0;

                foreach (string s in friends)
                {
                    Button b = new Button();
                    x += 30;
                    b.Location = new Point(10, x + 10);
                    b.Size = new Size(200, inaltime);
                    b.Text = s;
                    b.MouseClick += new MouseEventHandler(b_MouseClick);
                    panel.Controls.Add(b);
                }
            }

            
        }

        void b_MouseClick(object sender, MouseEventArgs e)
        {
            Button b = sender as Button;
            Chat chat = new Chat(clientSocket);
            chat.friend = b.Text;
            chat.username = this.username;
            chat.Show();
            chat_rooms.Add(chat);
        }


        private void AddFriend_Click(object sender, EventArgs e)
        {
            AddFriend af = new AddFriend();
            af.ShowDialog();
            if (af.username != null)
            {
                string rasp = "3 " + this.username + " " + af.username + " ";
                byteData = Encoding.ASCII.GetBytes(rasp);
                lista.Add(af.username);
                Invoke(new AfiseazaUseriDelegate(AfiseazaUseri), new Object[] { lista });

                clientSocket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnSend), null);

                //lista.Add(af.username);
                //AfiseazaUseri(lista);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;

            byteData = new byte[1024];
            //Start listening to the data asynchronously
            clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);

            
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                clientSocket.EndReceive(ar);
                string content = Encoding.ASCII.GetString(byteData);
                string[] continut;
                if(!content.Contains('|'))
                    continut = content.Split(' ');
                else
                    continut = content.Split('|');
                User.Text = content;
                //Accordingly process the message received
                switch (Convert.ToInt32(continut[0]))
                {
                    case 1: {//register
                        if (continut[1] == "1")
                        {
                            MessageBox.Show("Inregistrare reusita", "cient", MessageBoxButtons.OK);
                        }
                        else
                        {
                            MessageBox.Show("Inregistrare nereusita", "cient", MessageBoxButtons.OK);
                        }
                        break;
                    }
                    case 2: {//login
                        if (continut[1] == "1")
                        {

                            for (int i = 2; i < continut.Length; i++)
                            {
                                char[] sir = continut[i].ToCharArray();
                                if (sir!= null && sir[0] != '\0')
                                {
                                    lista.Add(continut[i]);
                                }
                            }
                            Invoke(new AfiseazaUseriDelegate(AfiseazaUseri), new Object[] { lista });
                        }
                        break;
                    }
                    case 3: {//addFriend
                        if (continut[1] == "1")
                        {
                            lista.Add(continut[2]);
                            Invoke(new AfiseazaUseriDelegate(AfiseazaUseri), new Object[] { lista });
                        }
                        //AfiseazaUseri(lista);
                        break;
                    }
                    case 4: {//delete
                        if (continut[1] == "1")
                        {
                            foreach(string s in lista)
                                if(s == continut[2])
                                    lista.Remove(s);
                        }
                        AfiseazaUseri(lista);
                        break;
                    }
                    case 5: {//message
                        bool fereastra_activa = false;
                        foreach (Chat chat in chat_rooms)
                        {
                            if (chat.friend == continut[1])
                            {
                                fereastra_activa = true;
                                chat.conversation += continut[1] + ":" + continut[2];
                                break;
                            }
                        }
                        if (fereastra_activa == false)
                        {
                            Chat chat = new Chat(clientSocket);
                            chat.friend = continut[1];
                            chat.conversation = continut[1] + ":" + continut[2];
                            chat.username = this.username;
                            chat.Show();
                            chat_rooms.Add(chat);
                        }

                        break;
                    }
                    case 6: {//file

                        break;
                    }
                    case 7: {//logout
                        lista.Remove(continut[1]);
                        Invoke(new AfiseazaUseriDelegate(AfiseazaUseri), new Object[] { lista });
                        break;
                    }
                    case 8: {//logare prieten
                        lista.Add(continut[1]);
                        Invoke(new AfiseazaUseriDelegate(AfiseazaUseri), new Object[] { lista });
                        break;
                    }
                    case 9: {//ai fost adaugat de catre cineva
                        DialogResult dr = MessageBox.Show(continut[1] + " vrea sa te adauge in lista de prieteni.", "addFried", MessageBoxButtons.YesNo);

                        if (dr == DialogResult.Yes)
                        {
                            string rasp = "8 " + continut[1];
                            byteData = Encoding.ASCII.GetBytes(rasp);
                            lista.Add(continut[1]);
                            //AfiseazaUseri(lista);
                            Invoke(new AfiseazaUseriDelegate(AfiseazaUseri), new Object[] {lista});

                            clientSocket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnSend), null);
                        }

                        break;
                    }
                    case 10: {//un prieten s-a deconectat
                        lista.Remove(continut[1]);
                        Invoke(new AfiseazaUseriDelegate(AfiseazaUseri), new Object[] { lista });
                        break;
                    }
                    default: { break; }


                }


                byteData = new byte[1024];

                clientSocket.BeginReceive(byteData,
                                          0,
                                          byteData.Length,
                                          SocketFlags.None,
                                          new AsyncCallback(OnReceive),
                                          null);

            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "clientTCPrecv: " + username, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show(ex.Message, "clientTCPsend: " + username, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to leave the chat room?", "client: " + username,
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            string rasp = "7 " + username;
            byteData = Encoding.ASCII.GetBytes(rasp);
            clientSocket.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnSend), null);
        }

    }
}
