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
using TestDLL;

namespace Client
{
    /// <summary>
    /// Interaction logic for AddTestWindow.xaml
    /// </summary>
    public partial class AddTestWindow : Window
    {
        public Test Test { get; set; }

        private ObservableCollection<QuestionLW> Questions = new ObservableCollection<QuestionLW>();

        private Dictionary<string, ObservableCollection<AnswerLW>> Answers = new Dictionary<string, ObservableCollection<AnswerLW>>();

        public AddTestWindow()
        {
            InitializeComponent();

            QuestionsListView.ItemsSource = Questions;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddQuestionWindow addQuestionWindow = new AddQuestionWindow();
            addQuestionWindow.ShowDialog();

            if (addQuestionWindow.AddPressed)
            {
                Questions.Add(new QuestionLW
                {
                    QuestionText = addQuestionWindow.QuestionText,
                    ScoreModifier = addQuestionWindow.ScoreModifier
                });
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int idx = QuestionsListView.SelectedIndex;

            if (idx != -1)
            {
                Questions.Remove(Questions[idx]);

                DeleteButton.IsEnabled = false;
            }
        }

        private void AnswersButton_Click(object sender, RoutedEventArgs e)
        {
            int idx = QuestionsListView.SelectedIndex;

            if (idx == -1)
            {
                return;
            }

            string question = Questions[idx].QuestionText;

            AddAnswersWindow addAnswersWindow = new AddAnswersWindow(question);
            addAnswersWindow.ShowDialog();

            if (addAnswersWindow.IsSaved)
            {
                Answers.Add(question, addAnswersWindow.Answers);
                MessageBox.Show("Answers successfully added");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Test = new Test();
            Test.Name = TestNameTextBox.Text;
            Test.Description = DescriptionTextBox.Text;

            foreach (QuestionLW item in Questions)
            {
                string questionText = item.QuestionText;
                double mod = item.ScoreModifier;

                if (!Answers.ContainsKey(questionText))
                {
                    MessageBox.Show("Each question must have answers!");
                    return;
                }

                bool isManyAnswers = false;
                int correctAnswersCount = 0;
                List<Answer> answersList = new List<Answer>();

                foreach (AnswerLW answer in Answers[questionText])
                {
                    answersList.Add(new Answer
                    {
                        Text = answer.Answer,
                        IsRight = answer.IsRight
                    });

                    if (answer.IsRight)
                    {
                        correctAnswersCount++;
                    }
                }

                if (correctAnswersCount == 0)
                {
                    MessageBox.Show("Each question must have at least 1 correct answer!");
                    return;
                }
                else if (correctAnswersCount > 1)
                {
                    isManyAnswers = true;
                }

                Question question = new Question
                {
                    Text = questionText,
                    ScoreModifier = mod,
                    IsManyAnswers = isManyAnswers,
                    Answers = answersList,
                    IsAnsweredCorrectly = false
                };

                Test.Questions.Add(question);
            }

            this.Close();
        }

        private void QuestionsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DeleteButton.IsEnabled = true;
        }
    }
}
