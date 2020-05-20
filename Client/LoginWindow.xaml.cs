using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TrueMessageDLL;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        static TcpClient client;
        static NetworkStream networkStream;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            int port = 8888;

            client = new TcpClient();
            try
            {
                client.BeginConnect("127.0.0.1", port, ConnectFunction, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ConnectFunction(IAsyncResult ar)
        {
            client.EndConnect(ar);
            networkStream = client.GetStream();

            string login = null;
            string password = null;

            this.Dispatcher.Invoke(() =>
            {
                login = UsernameTextBox.Text;
                password = PasswordTextBox.Password;
            });

            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.LogIn, Login = login, Message = password };

            byte[] dataLogInRequest;
            IFormatter formatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, trueMessageToServer);
                dataLogInRequest = stream.ToArray();
            }

            networkStream.BeginWrite(dataLogInRequest, 0, dataLogInRequest.Length, new AsyncCallback(LogInRequest), null);
        }

        private void LogInRequest(IAsyncResult ar)
        {
            networkStream.EndWrite(ar);

            byte[] dataLogInAnswer = new byte[1024];
            networkStream.BeginRead(dataLogInAnswer, 0, dataLogInAnswer.Length, new AsyncCallback(LogInAnswer), dataLogInAnswer);
        }

        private void LogInAnswer(IAsyncResult ar)
        {
            networkStream.EndRead(ar);

            byte[] dataLogInAnswer = (byte[])ar.AsyncState;

            IFormatter formatter = new BinaryFormatter();
            TrueMessage trueMessageFromServer = new TrueMessage();
            using (MemoryStream memoryStream = new MemoryStream(dataLogInAnswer))
            {
                trueMessageFromServer = (TrueMessage)formatter.Deserialize(memoryStream);
            }

            if (trueMessageFromServer.Command == Command.Approve)
            {
                Disconnect();

                if ((string)trueMessageFromServer.Message == "Student")
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        string login = UsernameTextBox.Text;
                        StudentWindow dashBoard = new StudentWindow(login);
                        dashBoard.Show();
                        this.Close();
                    });
                }
                else if ((string)trueMessageFromServer.Message == "Teacher")
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        string login = UsernameTextBox.Text;
                        TeacherWindow dashBoard = new TeacherWindow(login);
                        dashBoard.Show();
                        this.Close();
                    });
                }
                else if ((string)trueMessageFromServer.Message == "Admin")
                {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        string login = UsernameTextBox.Text;
                        AdminWindow dashBoard = new AdminWindow(login);
                        dashBoard.Show();
                        this.Close();
                    });
                }
                else
                {
                    MessageBox.Show($"Wrong message from server. Message: {(string)trueMessageFromServer.Message}", "Error");
                }
            }
            else if (trueMessageFromServer.Command == Command.Reject)
            {
                MessageBox.Show((string)trueMessageFromServer.Message);
            }
            else
            {
                MessageBox.Show("Received message from server is incorrect.", "Error");
            }
        }

        static void Disconnect()
        {
            if (networkStream != null)
                networkStream.Close();
            if (client != null)
                client.Close();
        }
    }
}
