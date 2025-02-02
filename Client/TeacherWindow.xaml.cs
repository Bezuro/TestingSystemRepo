﻿using MyListViewObjects;
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
using TestDLL;
using TrueMessageDLL;

namespace Client
{
    /// <summary>
    /// Interaction logic for TeacherWindow.xaml
    /// </summary>
    public partial class TeacherWindow : Window
    {
        string login;
        static TcpClient client;
        static NetworkStream networkStream;

        ObservableCollection<string> TestNames = new ObservableCollection<string>();
        ObservableCollection<MarkLW> MarkLWs = new ObservableCollection<MarkLW>();

        bool firstClose = true;

        public TeacherWindow(string login)
        {
            if (login == null)
            {
                MessageBox.Show("Login is null");
                this.Close();
            }

            InitializeComponent();

            this.login = login;
        }

        private void TestListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteButton.IsEnabled = true;
            UpdateMarksButton.IsEnabled = true;
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

            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.TeacherConnect, Login = login };

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
                Array array = (Array)trueMessageFromServer.Message;
                TestNames = new ObservableCollection<string>((IEnumerable<string>)array);

                this.Dispatcher.Invoke(() =>
                {
                    TestListView.ItemsSource = TestNames;
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

        private void UpdateMarksButton_Click(object sender, RoutedEventArgs e)
        {
            int idx = TestListView.SelectedIndex;
            string selectedTest = TestNames[idx];

            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.UpdateMarks, Login = login, Message = selectedTest };

            byte[] dataUpdateMarksRequest;
            IFormatter formatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, trueMessageToServer);
                dataUpdateMarksRequest = stream.ToArray();
            }

            networkStream.BeginWrite(dataUpdateMarksRequest, 0, dataUpdateMarksRequest.Length, new AsyncCallback(UpdateMarksRequest), null);
        }

        private void UpdateMarksRequest(IAsyncResult ar)
        {
            networkStream.EndWrite(ar);

            byte[] dataUpdateMarksAnswer = new byte[64000];
            networkStream.BeginRead(dataUpdateMarksAnswer, 0, dataUpdateMarksAnswer.Length, new AsyncCallback(UpdateMarksAnswer), dataUpdateMarksAnswer);
        }

        private void UpdateMarksAnswer(IAsyncResult ar)
        {
            networkStream.EndRead(ar);

            byte[] dataUpdateMarksAnswer = (byte[])ar.AsyncState;

            IFormatter formatter = new BinaryFormatter();
            TrueMessage trueMessageFromServer = new TrueMessage();
            using (MemoryStream memoryStream = new MemoryStream(dataUpdateMarksAnswer))
            {
                trueMessageFromServer = (TrueMessage)formatter.Deserialize(memoryStream);
            }

            if (trueMessageFromServer.Command == Command.Approve)
            {
                Array array = (Array)trueMessageFromServer.Message;
                MarkLWs = new ObservableCollection<MarkLW>((IEnumerable<MarkLW>)array);

                this.Dispatcher.Invoke(() =>
                {
                    MarkListView.ItemsSource = MarkLWs;
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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddTestWindow addTestWindow = new AddTestWindow();
            addTestWindow.ShowDialog();

            if (addTestWindow.Test == null)
            {
                return;
            }

            Test test = addTestWindow.Test;

            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.AddTest, Login = login, Message = test };

            byte[] dataAddTestRequest;
            IFormatter formatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, trueMessageToServer);
                dataAddTestRequest = stream.ToArray();
            }

            networkStream.BeginWrite(dataAddTestRequest, 0, dataAddTestRequest.Length, new AsyncCallback(AddTestRequest), null);
        }

        private void AddTestRequest(IAsyncResult ar)
        {
            networkStream.EndWrite(ar);

            byte[] dataAddTestAnswer = new byte[64000];
            networkStream.BeginRead(dataAddTestAnswer, 0, dataAddTestAnswer.Length, new AsyncCallback(AddTestAnswer), dataAddTestAnswer);
        }

        private void AddTestAnswer(IAsyncResult ar)
        {
            networkStream.EndRead(ar);

            byte[] dataAddTestAnswer = (byte[])ar.AsyncState;

            IFormatter formatter = new BinaryFormatter();
            TrueMessage trueMessageFromServer = new TrueMessage();
            using (MemoryStream memoryStream = new MemoryStream(dataAddTestAnswer))
            {
                trueMessageFromServer = (TrueMessage)formatter.Deserialize(memoryStream);
            }

            if (trueMessageFromServer.Command == Command.Approve)
            {
                this.Dispatcher.Invoke(() =>
                {
                    TestNames.Add((string)trueMessageFromServer.Message); //return test name if added
                });

                MessageBox.Show("Test successfully added");
            }
            else if (trueMessageFromServer.Command == Command.Reject)
            {
                MessageBox.Show("Test was not added");
            }
            else
            {
                MessageBox.Show("Received message from server is incorrect.", "Error");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int idx = TestListView.SelectedIndex;
            if (idx == -1)
            {
                return;
            }

            string testToDelete = TestNames[idx];

            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.DeleteTest, Login = login, Message = testToDelete };

            byte[] dataDeleteTestRequest;
            IFormatter formatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, trueMessageToServer);
                dataDeleteTestRequest = stream.ToArray();
            }

            networkStream.BeginWrite(dataDeleteTestRequest, 0, dataDeleteTestRequest.Length, new AsyncCallback(DeleteTestRequest), null);
        }

        private void DeleteTestRequest(IAsyncResult ar)
        {
            networkStream.EndWrite(ar);

            byte[] dataDeleteTestAnswer = new byte[64000];
            networkStream.BeginRead(dataDeleteTestAnswer, 0, dataDeleteTestAnswer.Length, new AsyncCallback(DeleteTestAnswer), dataDeleteTestAnswer);
        }

        private void DeleteTestAnswer(IAsyncResult ar)
        {
            networkStream.EndRead(ar);

            byte[] dataDeleteTestAnswer = (byte[])ar.AsyncState;

            IFormatter formatter = new BinaryFormatter();
            TrueMessage trueMessageFromServer = new TrueMessage();
            using (MemoryStream memoryStream = new MemoryStream(dataDeleteTestAnswer))
            {
                trueMessageFromServer = (TrueMessage)formatter.Deserialize(memoryStream);
            }

            if (trueMessageFromServer.Command == Command.Approve)
            {
                this.Dispatcher.Invoke(() =>
                {
                    TestNames.Remove((string)trueMessageFromServer.Message);
                });

                MessageBox.Show("Test successfully deleted");
            }
            else if (trueMessageFromServer.Command == Command.Reject)
            {
                MessageBox.Show("Test was not deleted");
            }
            else
            {
                MessageBox.Show("Received message from server is incorrect.", "Error");
            }
        }

        private void SetCurrentButton_Click(object sender, RoutedEventArgs e)
        {
            if ((TextBlock)TimeComboBox.SelectedItem == null)
            {
                MessageBox.Show("You must set Time", "Error");
                return;
            }

            TextBlock selectedItem = (TextBlock)TimeComboBox.SelectedItem;
            string time = selectedItem.Text;
            switch (time)
            {
                case "10 min":
                    time = "10";
                    break;
                case "20 min":
                    time = "20";
                    break;
                case "30 min":
                    time = "30";
                    break;
                case "45 min":
                    time = "45";
                    break;
                case "60 min":
                    time = "60";
                    break;
                default:
                    break;
            }

            int idx = TestListView.SelectedIndex;
            if (idx == -1)
            {
                MessageBox.Show("You must select test first", "Error");
                return;
            }

            string testToChoose = TestNames[idx];

            KeyValuePair<string, string> choosePair = new KeyValuePair<string, string>(testToChoose, time);

            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.ChooseCurrentTest, Login = login, Message = choosePair };

            byte[] dataChooseCurrentTestRequest;
            IFormatter formatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, trueMessageToServer);
                dataChooseCurrentTestRequest = stream.ToArray();
            }

            networkStream.BeginWrite(dataChooseCurrentTestRequest, 0, dataChooseCurrentTestRequest.Length, new AsyncCallback(ChooseCurrentTestRequest), null);
        }

        private void ChooseCurrentTestRequest(IAsyncResult ar)
        {
            networkStream.EndWrite(ar);

            byte[] dataChooseCurrentTestAnswer = new byte[64000];
            networkStream.BeginRead(dataChooseCurrentTestAnswer, 0, dataChooseCurrentTestAnswer.Length, 
                new AsyncCallback(ChooseCurrentTestAnswer), dataChooseCurrentTestAnswer);
        }

        private void ChooseCurrentTestAnswer(IAsyncResult ar)
        {
            networkStream.EndRead(ar);

            byte[] dataChooseCurrentTestAnswer = (byte[])ar.AsyncState;

            IFormatter formatter = new BinaryFormatter();
            TrueMessage trueMessageFromServer = new TrueMessage();
            using (MemoryStream memoryStream = new MemoryStream(dataChooseCurrentTestAnswer))
            {
                trueMessageFromServer = (TrueMessage)formatter.Deserialize(memoryStream);
            }

            if (trueMessageFromServer.Command == Command.Approve)
            {
                MessageBox.Show("Test successfully started");
            }
            else if (trueMessageFromServer.Command == Command.Reject)
            {
                MessageBox.Show("Test was not started");
            }
            else
            {
                MessageBox.Show("Received message from server is incorrect.", "Error");
            }
        }
    }
}
