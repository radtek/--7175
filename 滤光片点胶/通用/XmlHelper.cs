using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 滤光片点胶
{
    public class XmlHelper
    {
        /// <summary>
        /// 判断文件夹及路径是否存在
        /// </summary>
        /// <param name="path">待判断路径</param>
        /// <param name="file">待判断文件名</param>
        /// <returns></returns>
        public static bool Exists(string path, string file)
        {
            if (Directory.Exists(path))
            {
                if (File.Exists(path + "/" + file))
                {
                    return true;
                }
                else
                {
                    Growl.Warning("文件[" + path + "/" + file + "]不存在");
                    return false;
                }
            }
            else
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                directoryInfo.Create();
                Growl.Warning("文件夹[" + path + "]不存在，已建立");
                return false;
            }
        }
    }
}
