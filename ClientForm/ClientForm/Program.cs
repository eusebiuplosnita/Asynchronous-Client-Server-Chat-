using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ClientForm
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            LoginForm loginForm = new LoginForm();
            Application.Run(loginForm);

            if (loginForm.DialogResult == DialogResult.OK)
            {
                Form1 form1 = new Form1();
                form1.username = loginForm.username;
                form1.clientSocket = loginForm.clientSocket;
                form1.ShowDialog();
            }
        }
    }
}
