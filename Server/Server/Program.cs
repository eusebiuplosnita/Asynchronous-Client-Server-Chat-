using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.IO;

namespace Server
{
    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class AsynchronousSocketListener
    {
        public static ArrayList users;

        // Thread signal.
        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsynchronousSocketListener()
        {
            users = new ArrayList();
            StreamReader sr = new StreamReader("users.txt");
            string line = null;
            while ((line = sr.ReadLine()) != null)
            {
                string[] date = line.Split(' ');
                User u = new User(null, date[0], null, date[1]);
                users.Add(u);
            }
        }

        public static void StartListening()
        {
            users = new ArrayList();
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            Console.WriteLine(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 1000);

            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.sb.Append(Encoding.ASCII.GetString(
                    state.buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the 
                    // client. Display it on the console.
                    Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                        content.Length, content);

                    char[] continut = content.ToCharArray();
                    switch ((int)continut[0] - 48)
                    {
                        case 1:
                            {//register
                                Register(handler, content);
                                break;
                            }
                        case 2:
                            {//login
                                Login(handler, content);
                                break;
                            }
                        case 3:
                            {//add friend 
                                AddFriend(handler, content);
                                break;
                            }
                        case 4:
                            {//delete friend 
                                DeleteFriend(handler, content);
                                break;
                            }
                        case 5:
                            {//send message
                                SendMessage(handler, content);
                                break;
                            }
                        case 6:
                            {//send file
                                SendFile(handler, content);
                                break;
                            }
                        case 7:
                            {//logout
                                Logout(handler);
                                break;
                            }
                        default: { break; }
                    }



                }
                else
                {// Not all data received. Get more.
                    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None,
                    new AsyncCallback(ReadCallback), state);
                }
            }
            try
            {
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None,
                        new AsyncCallback(ReadCallback), state);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exceptie");
            }
        }

        private static void Register(Socket handler, String content)
        {
            string[] continut = content.Split(' ');

            string raspuns = "1 ";
            
            foreach(User user in users)
            {
                if(user.username == continut[1])
                    raspuns += "0";
                else 
                    raspuns += "1";
            }
            User u = new User(handler, continut[1], null, continut[2]);
            users.Add(u);
            Send(handler, raspuns);
        }

        private static void Login(Socket handler, String content)
        {
            string[] continut = content.Split(' ');
            string raspuns = "2 ";
            User user_logat = GetUser(continut[1]);
            if (user_logat != null)
            {
                raspuns += "1";
                foreach (User u in user_logat.friends)
                    if (u.active == true)
                    {
                        raspuns += u.username;
                    }
            }
            else
            {
                raspuns += "0";
            }
            Send(handler, raspuns);
        }

        private static User GetUser(string username)
        {
            User user_logat = null;
            
            foreach (User user in users)
            {
                if (user.username == username)
                {
                    user_logat = user;
                }
            }

            return user_logat;
        }

        private static void AddFriend(Socket handler, String content)
        {
            string[] continut = content.Split(' ');
            string raspuns1 = "3 ";
            string raspuns2 = "3 ";

            User user_logat = GetUser(continut[1]);

            User friend = GetUser(continut[2]);
            if (friend != null)
            {
                raspuns1 += "1";
                raspuns2 += user_logat.username;
                user_logat.friends.Add(friend);
            }
            else
            {
                raspuns1 += "0";
            }
            user_logat.handler = handler;
            Send(handler, raspuns1);
            if (friend.active == true)
                Send(friend.handler, raspuns2);
        }

        private static void DeleteFriend(Socket handler, String content)
        {
            string[] continut = content.Split(' ');
            User user_logat = GetUser(continut[1]);
            User friend = GetUser(continut[2]);
            string raspuns = "4 ";
            if (friend != null)
            {
                user_logat.friends.Remove(friend);
                raspuns += "1";
            }
            Send(handler, raspuns);
        }

        public static User GetUserByHandler(Socket handler)
        {
            User user_logat = null;
            
            foreach (User user in users)
            {
                if (user.handler.Equals(handler))
                {
                    user_logat = user;
                }
            }

            return user_logat;
        }

        private static void SendMessage(Socket handler, String content)
        {
            string[] continut = content.Split('|');
            User user_logat = GetUser(continut[1]);
            user_logat.handler = handler;
            User friend = GetUser(continut[2]);
            string raspuns = null;
            if (friend != null)
            {
                raspuns = "5|" + user_logat.username + continut[2];
                Send(friend.handler, raspuns);
            }
        }

        private static void SendFile(Socket handler, String content)
        {
            
        }

        private static void Logout(Socket handler)
        {
            User user_logat = GetUserByHandler(handler);
            foreach (User u in user_logat.friends)
            {
                if (u.active == true)
                    Send(u.handler, "7 " + user_logat.username);
            }
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), handler);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        public static int Main(String[] args)
        {
            StartListening();
            Console.ReadKey();
            return 0;
        }
    }
}
