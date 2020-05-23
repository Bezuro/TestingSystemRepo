using MyListViewObjects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

namespace Client
{
    /// <summary>
    /// Interaction logic for AddAnswersWindow.xaml
    /// </summary>
    public partial class AddAnswersWindow : Window
    {
        public ObservableCollection<AnswerLW> Answers = new ObservableCollection<AnswerLW>();

        public bool IsSaved { get; set; } = false;

        public AddAnswersWindow(string question)
        {
            InitializeComponent();

            AnswersListView.ItemsSource = Answers;
            QuestionTextBox.Text = question;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            bool isRight = false;
            if (IsRightCheckBox.IsChecked == true)
            {
                isRight = true;
            }

            Answers.Add(new AnswerLW
            {
                Answer = AnswerTextBox.Text,
                IsRight = isRight
            });

            AnswerTextBox.Text = "";
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int idx = AnswersListView.SelectedIndex;

            if (idx == -1)
            {
                return;
            }

            Answers.Remove(Answers[idx]);

            DeleteButton.IsEnabled = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            IsSaved = true;
            this.Close();
        }

        private void AnswersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteButton.IsEnabled = true;
        }
    }
}
