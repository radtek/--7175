using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace 滤光片点胶
{
    class CACG_Conv: IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int YTJ_XT_Nums = int.Parse(values[0].ToString());
            int YTJ_XT_ID = (int)values[1];
            if (YTJ_XT_ID == YTJ_XT_Nums)
            { return Visibility.Visible; }
            else
            { return Visibility.Collapsed; }

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
