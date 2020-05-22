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
    /// Interaction logic for AddQuestionWindow.xaml
    /// </summary>
    public partial class AddQuestionWindow : Window
    {
        public string QuestionText { get; set; }
        public double ScoreModifier { get; set; }
        public bool AddPressed { get; set; } = false;

        public AddQuestionWindow()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            QuestionText = QuestionTextBox.Text;
            ScoreModifier = Convert.ToDouble(ModifierTextBox.Text);
            AddPressed = true;
            this.Close();
        }
    }
}
