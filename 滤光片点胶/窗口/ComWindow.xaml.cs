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
    /// ComWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ComWindow : Window
    {
        public ComWindow()
        {
            InitializeComponent();
        }

        // 进程互斥
        private System.Threading.Mutex myMutex = null;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 禁止同时打开2个
            bool mutexIsNew = false;
            try
            {
                myMutex = new System.Threading.Mutex(true, "通讯设置", out mutexIsNew);
            }
            catch { }

            if (!mutexIsNew)
            {
                this.Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            myMutex.Close();
        }
    }
}
