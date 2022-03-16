using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace 滤光片点胶
{
    public class MainVIewModel:ViewModelBase
    {
        public MainVIewModel()
        {
            //ImSrc_test.Add(new WriteableBitmap());
        }

        /// <summary>
        /// Image控件显示的资源
        /// </summary>
        public ObservableCollection<WriteableBitmap> ImSrc_test
        {
            get => GetProperty(() => ImSrc_test);
            set => SetProperty(() => ImSrc_test, value);
        }
    }
}
