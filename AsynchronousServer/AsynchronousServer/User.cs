using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

namespace AsynchronousServer
{
    class User
    {
        public Socket handler;
        public string username;
        public ArrayList friends;
        public bool active;
        public string password;

        public User(Socket handler, string username, ArrayList friends, string password)
        {
            this.handler = handler;
            this.username = username;
            friends = new ArrayList();
            if (friends != null)
            {
                this.friends = friends;
            }
            this.active = true;
            this.password = password;
        }
    }
}
