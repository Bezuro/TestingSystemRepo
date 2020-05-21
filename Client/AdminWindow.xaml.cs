using MyListViewObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using TrueMessageDLL;

namespace Client
{
    /// <summary>
    /// Interaction logic for AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        string login;
        static TcpClient client;
        static NetworkStream networkStream;

        public ObservableCollection<UserLW> UserList { get; set; } = new ObservableCollection<UserLW>();

        UserLW lastAddedUser;

        bool firstClose = true;

        public AdminWindow(string login)
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

            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.AdminConnect, Login = login };

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

            byte[] dataConnectAnswer = new byte[1024];
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
                Array array = (Array)trueMessageFromServer.Message;
                UserList = new ObservableCollection<UserLW>((IEnumerable<UserLW>)array);

                this.Dispatcher.Invoke(() =>
                {
                    UsersListView.ItemsSource = UserList;
                });

            }
            else if (trueMessageFromServer.Command == Command.Reject)
            {

            }
            else
            {
                MessageBox.Show("Received message from server is incorrect.", "Error");
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            UserAddWindow userAddWindow = new UserAddWindow();

            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                userAddWindow.ShowDialog();

                if (userAddWindow.UserLW != null)
                {
                    UserList.Add(userAddWindow.UserLW);
                }

                lastAddedUser = userAddWindow.UserLW;
            });

            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.AddUser, Login = login, Message = userAddWindow.UserLW };

            byte[] dataAddUserRequest;
            IFormatter formatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, trueMessageToServer);
                dataAddUserRequest = stream.ToArray();
            }

            networkStream.BeginWrite(dataAddUserRequest, 0, dataAddUserRequest.Length, new AsyncCallback(AddUserRequest), null);
        }

        private void AddUserRequest(IAsyncResult ar)
        {
            networkStream.EndWrite(ar);

            byte[] dataAddUserAnswer = new byte[1024];
            networkStream.BeginRead(dataAddUserAnswer, 0, dataAddUserAnswer.Length, new AsyncCallback(AddUserAnswer), dataAddUserAnswer);
        }

        private void AddUserAnswer(IAsyncResult ar)
        {
            networkStream.EndRead(ar);

            byte[] dataAddUserAnswer = (byte[])ar.AsyncState;

            IFormatter formatter = new BinaryFormatter();
            TrueMessage trueMessageFromServer = new TrueMessage();
            using (MemoryStream memoryStream = new MemoryStream(dataAddUserAnswer))
            {
                trueMessageFromServer = (TrueMessage)formatter.Deserialize(memoryStream);
            }

            if (trueMessageFromServer.Command == Command.Approve)
            {
                MessageBox.Show("User successfully added");
            }
            else if (trueMessageFromServer.Command == Command.Reject)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    UserList.Remove(lastAddedUser);
                });
                MessageBox.Show($"User cannot be added: {trueMessageFromServer.Message}");
            }
            else
            {
                MessageBox.Show("Received message from server is incorrect.", "Error");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int idx = UsersListView.SelectedIndex;

            if (idx == -1)
            {
                return;
            }

            string userLoginToDelete = UserList[idx].Login;

            UserList.Remove(UserList[idx]);
            DeleteButton.IsEnabled = false;

            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.DeleteUser, Login = login, Message = userLoginToDelete };

            byte[] dataDeleteUserRequest;
            IFormatter formatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, trueMessageToServer);
                dataDeleteUserRequest = stream.ToArray();
            }

            networkStream.BeginWrite(dataDeleteUserRequest, 0, dataDeleteUserRequest.Length, new AsyncCallback(DeleteUserRequest), null);
        }

        private void DeleteUserRequest(IAsyncResult ar)
        {
            networkStream.EndWrite(ar);

            byte[] dataDeleteUserAnswer = new byte[1024];
            networkStream.BeginRead(dataDeleteUserAnswer, 0, dataDeleteUserAnswer.Length, new AsyncCallback(DeleteUserAnswer), dataDeleteUserAnswer);
        }

        private void DeleteUserAnswer(IAsyncResult ar)
        {
            networkStream.EndRead(ar);

            byte[] dataDeleteUserAnswer = (byte[])ar.AsyncState;

            IFormatter formatter = new BinaryFormatter();
            TrueMessage trueMessageFromServer = new TrueMessage();
            using (MemoryStream memoryStream = new MemoryStream(dataDeleteUserAnswer))
            {
                trueMessageFromServer = (TrueMessage)formatter.Deserialize(memoryStream);
            }

            if (trueMessageFromServer.Command == Command.Approve)
            {
                MessageBox.Show("User successfully deleted");
            }
            else if (trueMessageFromServer.Command == Command.Reject)
            {
                MessageBox.Show($"User cannot be deleted");
            }
            else
            {
                MessageBox.Show("Received message from server is incorrect.", "Error");
            }
        }

        private void UsersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteButton.IsEnabled = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (firstClose)
            {
                this.Visibility = System.Windows.Visibility.Hidden;
                firstClose = false;

                e.Cancel = true;

                TrueMessage trueMessageToServer = new TrueMessage { Command = Command.Disconnect, Login = login };

                byte[] dataWriteDisconnectRequest;
                IFormatter formatter = new BinaryFormatter();

                using (MemoryStream stream = new MemoryStream())
                {
                    formatter.Serialize(stream, trueMessageToServer);
                    dataWriteDisconnectRequest = stream.ToArray();
                }

                IAsyncResult asyncResult = networkStream.BeginWrite(dataWriteDisconnectRequest, 0, dataWriteDisconnectRequest.Length, new AsyncCallback(DisconnectRequest), null);
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
