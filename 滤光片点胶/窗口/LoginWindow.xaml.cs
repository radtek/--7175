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

namespace 滤光片点胶
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        string[] Username = { "KNC", "ADMIN" };
        string[] Password = { "only", "admin" };

        public LoginWindow()
        {
            InitializeComponent();

            pw.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 2; i++)
            {
                if (String.Compare(Username[i], un.Text.ToUpper()) == 0 && String.Compare(Password[i], pw.Password.ToLower()) == 0)
                {
                    this.DialogResult = true;
                    Close();
                    return;
                }
            }

            MessageBox.Show("账户或密码输入错误！");
            this.DialogResult = false;
            Close();
        }
    }
}
