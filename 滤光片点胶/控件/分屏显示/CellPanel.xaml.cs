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
    /// CellPanel.xaml 的交互逻辑
    /// </summary>
    public partial class CellPanel : UserControl
    {
        public int Index;
        public bool Selected;
        private bool isFull = false;
        private SolidColorBrush SelectedColor = new SolidColorBrush(Colors.YellowGreen);
        private SolidColorBrush MouseEnterColor = new SolidColorBrush(Colors.Red);
        private SolidColorBrush NormalColor = new SolidColorBrush(Colors.Silver);

        public CameraViewModel camera = new CameraViewModel();
        //public CamParamViewModel camParamViewModel = new CamParamViewModel();

        public CellPanel()
        {
            InitializeComponent();

            this.DataContext = camera;
            this.Selected = false;
        }

        /// <summary>
        /// 初始化类
        /// </summary>
        /// <param name="path"></param>
        public int Init(string path)
        {
            int ret = 0;
            camera.CamID = Index;

            ret += camera.Init(path);

            return ret;
        }

        private void ImageViewer_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!this.Selected)
            {
                this.BorderBrush = MouseEnterColor;
            }
        }

        private void ImageViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!this.Selected)
            {
                this.BorderBrush = NormalColor;
            }
        }

        private void ImageViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                if (this.isFull)
                {
                    MultiView.SetCurrentModel(MultiView.CurrentModel);
                }
                else
                {
                    MultiView.SetFullScreen(this.Index);
                }

                this.isFull = !this.isFull;
            }
            else if (e.ChangedButton == MouseButton.Left && e.ClickCount == 1)
            {
                foreach (CellPanel cellPanel in MultiView.DictPanel.Values)
                {
                    cellPanel.Selected = false;
                    cellPanel.BorderBrush = NormalColor;
                }
                this.Selected = true;
                this.BorderBrush = SelectedColor;

                MultiView.SetSelectedItem(this.Index);
            }

            if (e.ChangedButton == MouseButton.Right && e.ClickCount == 2)
            {

            }
        }

        private void Panel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.panel.Height = (double)this.Height - 2;
            this.panel.Width = (double)this.Height - 2;
        }
    }
}
