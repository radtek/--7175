using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace 滤光片点胶
{
    public class CVAlgorithms
    {
        public struct BmpBuf
        {
            public IntPtr pData;
            public int size;
            public IntPtr pData_IntPtr;
            //public int size_IntPtr;
            public int Height;
            public int Width;
        }

        /// <summary>
        /// 导入图片进行测试
        /// </summary>
        /// <param name="bmpBuf"></param>
        /// <param name="input_Parameter"></param>
        /// <param name="output_Parameter_Float"></param>
        /// <returns></returns>
        [DllImport("./dll/DLL4CS.dll", CharSet = CharSet.Ansi, EntryPoint = "MV_EntryPoint", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool MV_EntryPoint(int type,ref BmpBuf bmpBuf, string[] input_Parameter, ref float output_Parameter_Float);

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="data"></param>
        [DllImport("./dll/DLL4CS.dll", EntryPoint = "MV_Release", ExactSpelling = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool MV_Release(ref BmpBuf data);
    }
}
