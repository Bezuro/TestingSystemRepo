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
using System.Windows.Threading;
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
        int testTime;

        bool firstClose = true;

        DispatcherTimer timer;
        TimeSpan time;
        int currentPage = 0;

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
                    this.Dispatcher.Invoke(() =>
                    {
                        RetryLoadButton.Content = "Loaded";
                        RetryLoadButton.IsEnabled = false;
                        RetryLoadButton.Foreground = new SolidColorBrush(Colors.Black);

                        TestNameTextBlock.Text = testPair.Key.Name;
                        TestNameTextBlock.Visibility = Visibility.Visible;
                    });

                    Test = testPair.Key;
                    testTime = Convert.ToInt32(testPair.Value);
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

                        TestNameTextBlock.Text = testPair.Key.Name;
                        TestNameTextBlock.Visibility = Visibility.Visible;
                    });

                    Test = testPair.Key;
                    testTime = Convert.ToInt32(testPair.Value);
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

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            QuestionNumberTextBlock.Visibility = Visibility.Visible;
            QuestionFrame.Visibility = Visibility.Visible;
            currentPage = 0;
            QuestionNumberTextBlock.Text = $"Question {currentPage + 1} of {Test.Questions.Count}";

            time = TimeSpan.FromMinutes(testTime);
            timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Loaded, delegate
            {
                TimerTextBlock.Text = time.ToString("c");
                if (time == TimeSpan.Zero)
                {
                    timer.Stop();
                    EndTest();
                }
                time = time.Add(TimeSpan.FromSeconds(-1));
            }, Application.Current.Dispatcher);

            Page page = new Page();

            DockPanel dockPanel = new DockPanel();
            page.Content = dockPanel;

            TextBlock textBlock = new TextBlock();
            textBlock.Text = Test.Questions[0].Text;
            textBlock.FontSize = 18;
            textBlock.Foreground = new SolidColorBrush(Colors.NavajoWhite);
            DockPanel.SetDock(textBlock, Dock.Top);
            dockPanel.Children.Add(textBlock);

            if (Test.Questions[0].IsManyAnswers)
            {
                int i = 0;
                foreach (var answer in Test.Questions[0].Answers)
                {
                    CheckBox checkBox = new CheckBox();
                    checkBox.Content = $"{(char)(i + 'A')}. " + answer.Text;
                    checkBox.FontSize = 18;
                    checkBox.Foreground = new SolidColorBrush(Colors.White);
                    checkBox.Checked += CheckBox_Checked;
                    checkBox.Unchecked += CheckBox_Unchecked;
                    checkBox.Tag = i;
                    DockPanel.SetDock(checkBox, Dock.Top);
                    dockPanel.Children.Add(checkBox);
                    i++;
                }
            }
            else //1 answer
            {
                int i = 0;
                foreach (var answer in Test.Questions[0].Answers)
                {
                    RadioButton radioButton = new RadioButton();
                    radioButton.Content = $"{(char)(i + 'A')}. " + answer.Text;
                    radioButton.FontSize = 18;
                    radioButton.Foreground = new SolidColorBrush(Colors.White);
                    radioButton.Checked += RadioButton_Checked;
                    radioButton.Unchecked += RadioButton_Unchecked;
                    radioButton.Tag = i;
                    DockPanel.SetDock(radioButton, Dock.Top);
                    dockPanel.Children.Add(radioButton);
                    i++;
                }
            }

            QuestionFrame.Content = page;
        }

        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            int answerNumber = (int)(sender as RadioButton)?.Tag;
            Test.Questions[currentPage].Answers[answerNumber].IsChecked = false;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            int answerNumber = (int)(sender as RadioButton)?.Tag;
            Test.Questions[currentPage].Answers[answerNumber].IsChecked = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            int answerNumber = (int)(sender as CheckBox)?.Tag;
            Test.Questions[currentPage].Answers[answerNumber].IsChecked = false;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            int answerNumber = (int)(sender as CheckBox)?.Tag;
            Test.Questions[currentPage].Answers[answerNumber].IsChecked = true;
        }

        private void EndTest()
        {
            QuestionNumberTextBlock.Visibility = Visibility.Hidden;
            QuestionFrame.Visibility = Visibility.Hidden;

            double baseScore = 0;
            double modifierSum = 0;
            double totalScore = 0;
            int totalAnswered = 0;

            foreach (Question question in Test.Questions)
            {
                question.IsAnsweredCorrectly = true;
                foreach (Answer answer in question.Answers)
                {
                    if (answer.IsChecked != answer.IsRight)
                    {
                        question.IsAnsweredCorrectly = false;
                        break;
                    }
                }
                modifierSum += question.ScoreModifier;
            }

            baseScore = 100 / modifierSum;

            foreach (Question question in Test.Questions)
            {
                if (question.IsAnsweredCorrectly)
                {
                    totalScore += baseScore * question.ScoreModifier;
                    totalAnswered++;
                }
            }

            totalScore = Math.Round(totalScore, 0, MidpointRounding.AwayFromZero);

            ScoreTextBlock.Text = $"{totalScore}";
            QuestionsAnsweredTextBlock.Text = $"{totalAnswered}";

            TestEndedGrid.Visibility = Visibility.Visible;

            KeyValuePair<string, int> markPair = new KeyValuePair<string, int>(Test.Name, (int)totalScore);
            TrueMessage trueMessageToServer = new TrueMessage { Command = Command.SendMark, Login = login, Message = markPair };

            byte[] dataSendMarkRequest;
            IFormatter formatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, trueMessageToServer);
                dataSendMarkRequest = stream.ToArray();
            }

            networkStream.BeginWrite(dataSendMarkRequest, 0, dataSendMarkRequest.Length, new AsyncCallback(SendMarkRequest), null);
        }

        private void SendMarkRequest(IAsyncResult ar)
        {
            networkStream.EndWrite(ar);

            byte[] dataSendMarkAnswer = new byte[64000];
            networkStream.BeginRead(dataSendMarkAnswer, 0, dataSendMarkAnswer.Length, new AsyncCallback(SendMarkAnswer), dataSendMarkAnswer);
        }

        private void SendMarkAnswer(IAsyncResult ar)
        {
            networkStream.EndRead(ar);

            byte[] dataSendMarkAnswer = (byte[])ar.AsyncState;

            IFormatter formatter = new BinaryFormatter();
            TrueMessage trueMessageFromServer = new TrueMessage();
            using (MemoryStream memoryStream = new MemoryStream(dataSendMarkAnswer))
            {
                trueMessageFromServer = (TrueMessage)formatter.Deserialize(memoryStream);
            }

            if (trueMessageFromServer.Command == Command.Approve)
            {
                MessageBox.Show("Test ended");
            }
            else if (trueMessageFromServer.Command == Command.Reject)
            {

            }
            else
            {
                MessageBox.Show("Received message from server is incorrect.", "Error");
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (QuestionFrame.CanGoBack && currentPage > 0)
            {
                QuestionFrame.GoBack();
                currentPage--;
                QuestionNumberTextBlock.Text = $"Question {currentPage + 1} of {Test.Questions.Count}";
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (QuestionFrame.CanGoForward)
            {
                QuestionFrame.GoForward();
                currentPage++;
                QuestionNumberTextBlock.Text = $"Question {currentPage + 1} of {Test.Questions.Count}";
            }
            else if (currentPage < Test.Questions.Count - 1)
            {
                CreatePage();
                currentPage++;
                QuestionNumberTextBlock.Text = $"Question {currentPage + 1} of {Test.Questions.Count}";
            }
        }

        private void CreatePage()
        {
            Page page = new Page();

            DockPanel dockPanel = new DockPanel();
            page.Content = dockPanel;

            TextBlock textBlock = new TextBlock();
            textBlock.Text = Test.Questions[currentPage + 1].Text;
            textBlock.FontSize = 18;
            textBlock.Foreground = new SolidColorBrush(Colors.NavajoWhite);
            DockPanel.SetDock(textBlock, Dock.Top);
            dockPanel.Children.Add(textBlock);

            if (Test.Questions[currentPage + 1].IsManyAnswers)
            {
                int i = 0;
                foreach (var answer in Test.Questions[currentPage + 1].Answers)
                {
                    CheckBox checkBox = new CheckBox();
                    checkBox.Content = $"{(char)(i + 'A')}. " + answer.Text;
                    checkBox.FontSize = 18;
                    checkBox.Foreground = new SolidColorBrush(Colors.White);
                    checkBox.Checked += CheckBox_Checked;
                    checkBox.Unchecked += CheckBox_Unchecked;
                    checkBox.Tag = i;
                    DockPanel.SetDock(checkBox, Dock.Top);
                    dockPanel.Children.Add(checkBox);
                    i++;
                }
            }
            else //1 answer
            {
                int i = 0;
                foreach (var answer in Test.Questions[currentPage + 1].Answers)
                {
                    RadioButton radioButton = new RadioButton();
                    radioButton.Content = $"{(char)(i + 'A')}. " + answer.Text;
                    radioButton.FontSize = 18;
                    radioButton.Foreground = new SolidColorBrush(Colors.White);
                    radioButton.Checked += RadioButton_Checked;
                    radioButton.Unchecked += RadioButton_Unchecked;
                    radioButton.Tag = i;
                    DockPanel.SetDock(radioButton, Dock.Top);
                    dockPanel.Children.Add(radioButton);
                    i++;
                }
            }

            QuestionFrame.Content = page;
        }

        private void EndButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            time = new TimeSpan(0, 0, 0);
            TimerTextBlock.Text = time.ToString("c");
            EndTest();
        }
    }
}
