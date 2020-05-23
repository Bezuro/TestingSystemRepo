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
using System.Windows.Shapes;
using TestDLL;
using TrueMessageDLL;

namespace Client
{
    /// <summary>
    /// Interaction logic for StudentWindow.xaml
    /// </summary>
    public partial class StudentWindow : Window
    {
        string login;
        static TcpClient client;
        static NetworkStream networkStream;

        TestDLL.Test Test;
        int time;

        bool firstClose = true;

        public StudentWindow(string login)
        {
            if (login == null)
            {
                MessageBox.Show("Login is null");
                this.Close();
            }

            InitializeComponent();

            this.login = login;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
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

            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.StudentConnect, Login = login };

            byte[] dataConnectRequest;
            IFormatter formatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, trueMessageToServer);
                dataConnectRequest = stream.ToArray();
            }

            networkStream.BeginWrite(dataConnectRequest, 0, dataConnectRequest.Length, new AsyncCallback(ConnectRequest), null);
        }

        private void ConnectRequest(IAsyncResult ar)
        {
            networkStream.EndWrite(ar);

            byte[] dataConnectAnswer = new byte[64000];
            networkStream.BeginRead(dataConnectAnswer, 0, dataConnectAnswer.Length, new AsyncCallback(ConnectAnswer), dataConnectAnswer);
        }

        private void ConnectAnswer(IAsyncResult ar)
        {
            networkStream.EndRead(ar);

            byte[] dataConnectAnswer = (byte[])ar.AsyncState;

            IFormatter formatter = new BinaryFormatter();
            TrueMessage trueMessageFromServer = new TrueMessage();
            using (MemoryStream memoryStream = new MemoryStream(dataConnectAnswer))
            {
                trueMessageFromServer = (TrueMessage)formatter.Deserialize(memoryStream);
            }

            if (trueMessageFromServer.Command == Command.Approve)
            {
                if (trueMessageFromServer.Message == null)
                {
                    MessageBox.Show("Message was null");
                    return;
                }
                KeyValuePair<Test, string> testPair = (KeyValuePair<Test, string>)trueMessageFromServer.Message;
                if (testPair.Key != null)
                {
                    RetryLoadButton.Content = "Loaded";
                    RetryLoadButton.IsEnabled = false;

                    Test = testPair.Key;
                    time = Convert.ToInt32(testPair.Value);
                }
            }
            else if (trueMessageFromServer.Command == Command.Reject)
            {

            }
            else
            {
                MessageBox.Show("Received message from server is incorrect.", "Error");
            }
        }

        private void RetryLoadButton_Click(object sender, RoutedEventArgs e)
        {
            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.LoadCurrentTest, Login = login };

            byte[] dataLoadCurrentTestRequest;
            IFormatter formatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, trueMessageToServer);
                dataLoadCurrentTestRequest = stream.ToArray();
            }

            networkStream.BeginWrite(dataLoadCurrentTestRequest, 0, dataLoadCurrentTestRequest.Length, new AsyncCallback(LoadCurrentTestRequest), null);
        }

        private void LoadCurrentTestRequest(IAsyncResult ar)
        {
            networkStream.EndWrite(ar);

            byte[] dataLoadCurrentTestAnswer = new byte[64000];
            networkStream.BeginRead(dataLoadCurrentTestAnswer, 0, dataLoadCurrentTestAnswer.Length, new AsyncCallback(LoadCurrentTestAnswer), dataLoadCurrentTestAnswer);
        }

        private void LoadCurrentTestAnswer(IAsyncResult ar)
        {
            networkStream.EndRead(ar);

            byte[] dataLoadCurrentTestAnswer = (byte[])ar.AsyncState;

            IFormatter formatter = new BinaryFormatter();
            TrueMessage trueMessageFromServer = new TrueMessage();
            using (MemoryStream memoryStream = new MemoryStream(dataLoadCurrentTestAnswer))
            {
                trueMessageFromServer = (TrueMessage)formatter.Deserialize(memoryStream);
            }

            if (trueMessageFromServer.Command == Command.Approve)
            {
                if (trueMessageFromServer.Message == null)
                {
                    MessageBox.Show("Message was null");
                    return;
                }
                KeyValuePair<Test, string> testPair = (KeyValuePair<Test, string>)trueMessageFromServer.Message;
                if (testPair.Key != null)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        RetryLoadButton.Content = "Loaded";
                        RetryLoadButton.IsEnabled = false;
                        RetryLoadButton.Foreground = new SolidColorBrush(Colors.Black);
                    });

                    Test = testPair.Key;
                    time = Convert.ToInt32(testPair.Value);
                }
            }
            else if (trueMessageFromServer.Command == Command.Reject)
            {
                MessageBox.Show("There no test to load");
            }
            else
            {
                MessageBox.Show("Received message from server is incorrect.", "Error");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (firstClose)
            {
                this.Visibility = System.Windows.Visibility.Hidden;
                firstClose = false;

                e.Cancel = true;

                TrueMessage trueMessageToServer = new TrueMessage { Command = Command.Disconnect, Login = login };

                byte[] dataDisconnectRequest;
                IFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, trueMessageToServer);
                    dataDisconnectRequest = stream.ToArray();
                }

                IAsyncResult asyncResult = networkStream.BeginWrite(dataDisconnectRequest, 0, dataDisconnectRequest.Length, new AsyncCallback(DisconnectRequest), null);
                asyncResult.AsyncWaitHandle.WaitOne();

                Environment.Exit(0);
            }
        }

        private void DisconnectRequest(IAsyncResult ar)
        {
            networkStream.EndWrite(ar);
            Disconnect();
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
