using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

namespace AsynchronousServer
{
    public partial class Form1 : Form
    {
        public ArrayList users;
        //The main socket on which the server listens to the clients
        Socket serverSocket;

        byte[] byteData = new byte[1024];

        public Form1()
        {
            users = new ArrayList();
            InitializeComponent();
        }

        private void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = serverSocket.EndAccept(ar);

                //Start listening for more clients
                serverSocket.BeginAccept(new AsyncCallback(OnAccept), null);

                //Once the client connects then start receiving the commands from her
                clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None,
                    new AsyncCallback(OnReceive), clientSocket);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSserverTCP1",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                Socket clientSocket = (Socket)ar.AsyncState;
                clientSocket.EndReceive(ar);
                string content = Encoding.ASCII.GetString(byteData, 0, byteData.Length);

                char[] continut = content.ToCharArray();

                switch ((int)continut[0] - 48)
                {
                    case 1:
                        {//register
                            Register(clientSocket, content);
                            break;
                        }
                    case 2:
                        {//login
                            Login(clientSocket, content);
                            break;
                        }
                    case 3:
                        {//add friend 
                            AddFriend(clientSocket, content);
                            break;
                        }
                    case 4:
                        {//delete friend 
                            DeleteFriend(clientSocket, content);
                            break;
                        }
                    case 5:
                        {//send message
                            SendMessage(clientSocket, content);
                            break;
                        }
                    case 6:
                        {//send file
                            SendFile(clientSocket, content);
                            break;
                        }
                    case 7:
                        {//logout
                            Logout(clientSocket, content);
                            break;
                        }
                    case 8: {
                        string[] continut1 = content.Split(' ');
                        User u = GetUser(continut1[1]);
                        User friend = GetUser(continut1[2]);
                        u.friends.Add(friend);
                        break;
                    }

                    default: { break; }
                }

                //If the user is logging out then we need not listen from her
                if ((int)continut[0] - 48 != 7)
                {
                    //Start listening to the message send by the user
                    clientSocket.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), clientSocket);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSserverTCP2", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Register(Socket handler, String content)
        {
            string[] continut = content.Split(' ');

            string raspuns = "1 ";
            bool newuser = true;
            //interogare database pentru a vedea daca username-ul este folosit de alta persoana
            foreach (User user in users)
            {
                if (user.username == continut[1])
                {
                    raspuns += "0 ";
                    newuser = false;
                    break;
                }   
            }
            if (newuser)
            {
                raspuns += "1 ";
                User u = new User(handler, continut[1], null, continut[2]);
                users.Add(u);
            }
            byte[] response = Encoding.ASCII.GetBytes(raspuns);
            handler.BeginSend(response, 0, response.Length, SocketFlags.None,
                                new AsyncCallback(OnSend), handler);
        }

        private void Login(Socket handler, String content)
        {
            string[] continut = content.Split(' ');
            string raspuns = "2 ";
            User user_logat = GetUser(continut[1]);
            
            if (user_logat != null)
            {
                user_logat.handler = handler;
                raspuns += "1 ";
                string raspuns_to_friends = "8 " + user_logat.username+" ";
                byte[] rasp = Encoding.ASCII.GetBytes(raspuns_to_friends);
                foreach (User u in user_logat.friends)
                {
                    if (u.active == true)
                    {
                        raspuns += u.username+ " ";
                        u.handler.BeginSend(rasp, 0, rasp.Length, SocketFlags.None,
                                new AsyncCallback(OnSend), user_logat.handler);
                    }
                }
            }
            else
            {
                raspuns += "0 ";
            }
            byte[] response = Encoding.ASCII.GetBytes(raspuns);
            handler.BeginSend(response, 0, response.Length, SocketFlags.None,
                                new AsyncCallback(OnSend), handler);
        }

        private User GetUser(string username)
        {
            User user_logat = null;
            //interogare database pentru a vedea daca username-ul si parola sunt corecte
            foreach (User user in users)
            {
                if (user.username == username)
                {
                    user_logat = user;
                }
            }

            return user_logat;
        }

        private void AddFriend(Socket handler, String content)
        {
            string[] continut = content.Split(' ');
            string raspuns1 = "3 ";
            string raspuns2 = "9 ";

            User user_logat = GetUser(continut[1]);

            User friend = GetUser(continut[2]);
            if (friend != null)
            {
                raspuns1 += "1 " + friend.username + " ";
                raspuns2 += user_logat.username + " ";
                user_logat.friends.Add(friend);
            }
            else
            {
                raspuns1 += "0 ";
            }
            user_logat.handler = handler;
            byte[] response = Encoding.ASCII.GetBytes(raspuns1);
            user_logat.handler.BeginSend(response, 0, response.Length, SocketFlags.None,
                                new AsyncCallback(OnSend), user_logat.handler);
            byte[] response2 = Encoding.ASCII.GetBytes(raspuns2);
            if (friend != null && friend.active == true)
                friend.handler.BeginSend(response2, 0, response2.Length, SocketFlags.None,
                                new AsyncCallback(OnSend), friend.handler);
        }

        private void DeleteFriend(Socket handler, String content)
        {
            string[] continut = content.Split(' ');
            User user_logat = GetUser(continut[1]);
            User friend = GetUser(continut[2]);
            string raspuns = "4 ";
            if (friend != null)
            {
                user_logat.friends.Remove(friend);
                raspuns += "1 " + friend.username + " ";
            }
            else { raspuns += "0 "; }
            byte[] response = Encoding.ASCII.GetBytes(raspuns);
            user_logat.handler.BeginSend(response, 0, response.Length, SocketFlags.None,
                                new AsyncCallback(OnSend), user_logat.handler);
        }

        private void SendMessage(Socket handler, String content)
        {
            string[] continut = content.Split('|');
            User user_logat = GetUser(continut[1]);
            user_logat.handler = handler;
            User friend = GetUser(continut[2]);
            string raspuns = null;
            if (friend != null)
            {
                raspuns = "5|" + user_logat.username + "|" + continut[2] + "|";
                byte[] response = Encoding.ASCII.GetBytes(raspuns);
                friend.handler.BeginSend(response, 0, response.Length, SocketFlags.None,
                                    new AsyncCallback(OnSend), friend.handler);
            }
        }

        private void SendFile(Socket handler, String content)
        {

        }

        private void Logout(Socket handler, string content)
        {
            string[] continut = content.Split(' ');
            User user_logat = GetUser(continut[1]);
            string rasp = "7 " + user_logat.username + " ";
            byte[] response = Encoding.ASCII.GetBytes(rasp);
            foreach (User u in user_logat.friends)
            {
                if (u.active == true)
                {

                    u.handler.BeginSend(response, 0, response.Length, SocketFlags.None,
                                new AsyncCallback(OnSend), u.handler);

                }
            }
            //handler.Close();
        }

        public void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;
                client.EndSend(ar);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSserverTCP3", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            try
            {
                //We are using TCP sockets
                serverSocket = new Socket(AddressFamily.InterNetwork,
                                          SocketType.Stream,
                                          ProtocolType.Tcp);

                txtLog.Text = "Connectare";

                //Assign the any IP of the machine and listen on port number 1000
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 1000);

                //Bind and listen on the given address
                serverSocket.Bind(ipEndPoint);
                serverSocket.Listen(4);

                //Accept the incoming clients
                serverSocket.BeginAccept(new AsyncCallback(OnAccept), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSserverTCP4",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
