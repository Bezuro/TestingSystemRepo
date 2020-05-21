using MyListViewObjects;
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
    /// Interaction logic for UserAddWindow.xaml
    /// </summary>
    public partial class UserAddWindow : Window
    {
        public UserLW UserLW { get; private set; }

        public UserAddWindow()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (CheckData())
            {
                TextBlock selectedItem = (TextBlock)UserTypeComboBox.SelectedItem;

                UserLW = new UserLW
                {
                    Login = LoginTextBox.Text,
                    Password = PasswordTextBox.Password,
                    FullName = FullNameTextBox.Text,
                    UserType = selectedItem.Text
                };

                this.Close();
            }
        }

        private bool CheckData()
        {
            if (LoginTextBox.Text.Length < 4 || LoginTextBox.Text.Length > 20)
            {
                MessageBox.Show("Login must be 4 to 20 characters long");
                return false;
            }
            if (PasswordTextBox.Password.Length < 4 || PasswordTextBox.Password.Length > 30)
            {
                MessageBox.Show("Password must be 4 to 30 characters long");
                return false;
            }
            if (FullNameTextBox.Text.Length < 4 || PasswordTextBox.Password.Length > 60)
            {
                MessageBox.Show("FullName must be 4 to 60 characters long");
                return false;
            }

            if ((TextBlock)UserTypeComboBox.SelectedItem == null)
            {
                MessageBox.Show("You must choose UserType");
                return false;
            }
            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
