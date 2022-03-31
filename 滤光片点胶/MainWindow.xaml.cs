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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace 滤光片点胶
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel MainVM;

        public MainWindow()
        {
            InitializeComponent();


           


            Loaded += (s, e) =>
            {
                grid.Tag = 1;
                MultiView.InitGrid(grid, 2);
                MultiView.SetCurrentModel(2);

                Run run = new Run("[控制台输出]\n");
                Console.Inlines.Add(run);
                run.Foreground = new SolidColorBrush(Colors.White);
                MySerialPort.OriginalTextBlock = Console;
                

                MainVM = new MainViewModel();
                this.DataContext = MainVM;
            };
        }
    }
}
