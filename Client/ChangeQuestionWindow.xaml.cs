using System;
using System.Collections.Generic;
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
    /// Interaction logic for ChangeQuestionWindow.xaml
    /// </summary>
    public partial class ChangeQuestionWindow : Window
    {
        public string QuestionText { get; set; }
        public double ScoreModifier { get; set; }
        public bool ChangePressed { get; set; } = false;

        public ChangeQuestionWindow(string questionText, double scoreModifier)
        {
            InitializeComponent();

            QuestionTextBox.Text = questionText;
            ModifierTextBox.Text = scoreModifier.ToString();
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            QuestionText = QuestionTextBox.Text;
            ScoreModifier = Convert.ToDouble(ModifierTextBox.Text);
            ChangePressed = true;
            this.Close();
        }
    }
}
