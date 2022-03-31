using Amib.Threading;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace 滤光片点胶
{
    public class CameraViewModel:ViewModelBase
    {
        /// <summary>
        /// 相机信息
        /// </summary>
        public HiKhelper camera = new HiKhelper();

        /// <summary>
        /// /构造函数
        /// </summary>
        public CameraViewModel()
        {
            camInfos = HiKhelper.CamInfos;
            stp = new SmartThreadPool { MaxThreads = 1 };
            camera.MV_OnOriFrameInvoked += Hik_MV_OnOriFrameInvoked;
            
            ImSrc_test = new WriteableBitmap(new BitmapImage(new Uri(@"./图片/null.png", UriKind.Relative))); ;
        }

        #region 前台绑定
        
        /// <summary>
        /// Image控件显示的资源
        /// </summary>
        public WriteableBitmap ImSrc_test
        {
            get => GetProperty(() => ImSrc_test);
            set => SetProperty(() => ImSrc_test, value);
        }

        /// <summary>
        /// 相机信息 用于连接相机
        /// </summary>
        public ObservableCollection<HiKhelper.CamInfo> camInfos
        {
            get => GetProperty(() => camInfos);
            set => SetProperty(() => camInfos, value);
        }

        /// <summary>
        /// 被选中相机
        /// </summary>
        public int SelectedCam
        {
            get => GetProperty(() => SelectedCam);
            set => SetProperty(() => SelectedCam, value,()=>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("SelectedCam").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        /// 被选中算法
        /// </summary>
        public int SelectedAlg
        {
            get => GetProperty(() => SelectedAlg);
            set => SetProperty(() => SelectedAlg, value,()=>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("SelectedAlg").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        #endregion
        
        #region 相机操作

        public void Connect()
        {
            camera.Connect(SelectedCam);
        }

        public void Disconnect()
        {
            camera.Disconnect();
        }

        #endregion

        #region 画面显示

        /// <summary>
        /// 线程锁对象
        /// </summary>
        private static readonly object objlock = new object();

        /// <summary>
        /// 画面显示缓冲
        /// </summary>
        CVAlgorithms.BmpBuf bmpBuf = new CVAlgorithms.BmpBuf();

        /// <summary>
        /// 线程执行函数
        /// </summary>
        /// <param name="i"></param>
        /// <param name="e"></param>
        private void MV_STPAction(int i, HiKhelper.MV_IM_INFO e)
        {

            bmpBuf.Width = e.width;
            bmpBuf.Height = e.height;
            bmpBuf.pData_IntPtr = e.pData;
            //CVAlgorithms.MV_Upload(e.width, e.height, ref bmpBuf, 3);


            //foreach (var item in AlgPros.ElementAt(0).ProList)
            //{
            //    if (0 != item.Function()) break;
            //}
            //CVAlgorithms.MV_Download(ref bmpBuf);
            float[] outParam = new float[3];
            string[] inParam = new string[16];
            inParam[0] = ShowMode.ToString();

            inParam[1] = LocThresh.ToString();
            inParam[2] = MaxRadius.ToString();
            inParam[3] = MinRadius.ToString();

            inParam[4] = Radius.ToString();
            inParam[5] = nMaxRadius.ToString();
            inParam[6] = nMinRadius.ToString();

            inParam[7] = D1thresh.ToString();
            inParam[8] = D1SizeMax.ToString();
            inParam[9] = D1SizeMin.ToString();

            inParam[10] = D2AdapSize.ToString();
            inParam[11] = D2AdapC.ToString();
            inParam[12] = D2RoundnessMin.ToString();
            inParam[13] = D2RectangularityMin.ToString();
            inParam[14] = D2sizeMax.ToString();
            inParam[15] = D2sizeMin.ToString();

            CVAlgorithms.MV_EntryPoint(SelectedAlg, ref bmpBuf, inParam, ref outParam[0]);


            if (camera.isTrigger)
            {
                if (outParam[0] == 1)
                {
                    MV_OnSendMess?.Invoke(this, "T");
                }
                else
                {
                    MV_OnSendMess?.Invoke(this, "F");
                }
            }
            


            //显示
            Application.Current.Dispatcher.Invoke(() =>
            {
                int size = (int)bmpBuf.size;
                if (ImSrc_test == null || ImSrc_test.Width != bmpBuf.Width || ImSrc_test.Height != bmpBuf.Height)
                {
                    if (size > 3 * bmpBuf.Width * bmpBuf.Height / 2)
                        ImSrc_test = new WriteableBitmap(bmpBuf.Width, bmpBuf.Height, 96.0, 96.0, PixelFormats.Bgr24, null);
                    else
                        ImSrc_test = new WriteableBitmap(bmpBuf.Width, bmpBuf.Height, 24.0, 24.0, PixelFormats.Gray8, null);
                }

                lock (objlock)
                {
                    if ((e.pData != (IntPtr)0x00000000))
                    {
                        ImSrc_test.Lock();
                        ImSrc_test.WritePixels(new Int32Rect(0, 0, bmpBuf.Width, bmpBuf.Height), bmpBuf.pData, size, ImSrc_test.BackBufferStride);
                        ImSrc_test.AddDirtyRect(new Int32Rect(0, 0, bmpBuf.Width, bmpBuf.Height));
                        ImSrc_test.Unlock();

                    }
                }

                CVAlgorithms.MV_Release(ref bmpBuf);
            });
            

            ////这里是固定的
            //CVAlgorithms.MV_Release(ref bmpBuf);

            
        }

        /// <summary>
        /// 回调函数，回调帧为YUV格式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hik_MV_OnOriFrameInvoked(object sender, HiKhelper.MV_IM_INFO e)
        {
            stp.QueueWorkItem(new Action<int, HiKhelper.MV_IM_INFO>(MV_STPAction), 0, e, WorkItemPriority.Normal);
            //stp.QueueWorkItem(new Amib.Threading.Action<int, HiKhelper.MV_IM_INFO>(MV_STPAction), 0, e, Amib.Threading.WorkItemPriority.Normal);
        }

        /// <summary>
        /// 智能线程池，用于处理回调图像算法
        /// </summary>
        private SmartThreadPool stp;

        #endregion

        #region 外部通讯
        /// <summary>
        /// 路由事件，回调原始帧
        /// </summary>
        public event EventHandler<string> MV_OnSendMess;
        #endregion

        #region 配置读写
        public int CamID;

        string filePath = "";

        void InitErr()
        {
            //MainWindow.ErrInfo(filePath + "丢失");
            try
            {
                //获得文件路径
                string localFilePath = "";
                localFilePath = filePath;
                XDocument xdoc = new XDocument();
                XDeclaration xdec = new XDeclaration("1.0", "utf-8", "yes");
                xdoc.Declaration = xdec;

                XElement rootEle;
                XElement classEle;
                //XElement childEle;

                //添加根节点
                rootEle = new XElement("CamConfig");
                xdoc.Add(rootEle);

                classEle = new XElement("SelectedCam", 0);
                rootEle.Add(classEle);

                classEle = new XElement("SelectedAlg", 0);
                rootEle.Add(classEle);

                classEle = new XElement("ShowMode", 0);
                rootEle.Add(classEle);

                classEle = new XElement("LocThresh", 128);
                rootEle.Add(classEle);

                classEle = new XElement("MaxRadius", 100);
                rootEle.Add(classEle);

                classEle = new XElement("MinRadius", 0);
                rootEle.Add(classEle);

                classEle = new XElement("Radius", 128);
                rootEle.Add(classEle);

                classEle = new XElement("nMaxRadius", 100);
                rootEle.Add(classEle);

                classEle = new XElement("nMinRadius", 0);
                rootEle.Add(classEle);

                classEle = new XElement("D1thresh", 128);
                rootEle.Add(classEle);

                classEle = new XElement("D1SizeMax", 100);
                rootEle.Add(classEle);

                classEle = new XElement("D1SizeMin", 0);
                rootEle.Add(classEle);

                classEle = new XElement("D2AdapSize", 2);
                rootEle.Add(classEle);

                classEle = new XElement("D2AdapC", 2);
                rootEle.Add(classEle);

                classEle = new XElement("D2RoundnessMin", 0.5);
                rootEle.Add(classEle);

                classEle = new XElement("D2RectangularityMin", 0.5);
                rootEle.Add(classEle);

                classEle = new XElement("D2sizeMax", 100);
                rootEle.Add(classEle);

                classEle = new XElement("D2sizeMin", 0);
                rootEle.Add(classEle);

                xdoc.Save(localFilePath);
            }
            catch
            {
                Growl.Error("[" + filePath + "]生成失败！");
            }
        }

        public int Init(string path)
        {
            int ret = 0;
            filePath = path + "/CamConfig.xml";
            if (XmlHelper.Exists(path, "CamConfig.xml"))
            {
                try
                {
                    XDocument Config = XDocument.Load(path + "/CamConfig.xml");

                    SelectedCam = int.Parse(Config.Descendants("SelectedCam").ElementAt(0).Value);
                    SelectedAlg = int.Parse(Config.Descendants("SelectedAlg").ElementAt(0).Value);

                    ShowMode = int.Parse(Config.Descendants("ShowMode").ElementAt(0).Value);

                    LocThresh = int.Parse(Config.Descendants("LocThresh").ElementAt(0).Value);
                    MaxRadius = int.Parse(Config.Descendants("MaxRadius").ElementAt(0).Value);
                    MinRadius = int.Parse(Config.Descendants("MinRadius").ElementAt(0).Value);

                    Radius = int.Parse(Config.Descendants("Radius").ElementAt(0).Value);
                    nMaxRadius = int.Parse(Config.Descendants("nMaxRadius").ElementAt(0).Value);
                    nMinRadius = int.Parse(Config.Descendants("nMinRadius").ElementAt(0).Value);

                    D1thresh = int.Parse(Config.Descendants("D1thresh").ElementAt(0).Value);
                    D1SizeMax = int.Parse(Config.Descendants("D1SizeMax").ElementAt(0).Value);
                    D1SizeMin = int.Parse(Config.Descendants("D1SizeMin").ElementAt(0).Value);

                    D2AdapSize = int.Parse(Config.Descendants("D2AdapSize").ElementAt(0).Value);
                    D2AdapC = int.Parse(Config.Descendants("D2AdapC").ElementAt(0).Value);
                    D2RoundnessMin = float.Parse(Config.Descendants("D2RoundnessMin").ElementAt(0).Value);
                    D2RectangularityMin = float.Parse(Config.Descendants("D2RectangularityMin").ElementAt(0).Value);
                    D2sizeMax = int.Parse(Config.Descendants("D2sizeMax").ElementAt(0).Value);
                    D2sizeMin = int.Parse(Config.Descendants("D2sizeMin").ElementAt(0).Value);
                }
                catch (Exception err)
                {
                    Growl.Error("相机" + CamID + "配置信息丢失！已重新生成");
                    InitErr();
                }
            }
            else
            {
                Growl.Error("相机" + CamID + "初始化失败！");
                InitErr();
                ret++;
            }

            return ret;
        }

        #endregion

        #region 算法参数

        /// <summary>
        /// 显示模式
        /// </summary>
        public int ShowMode
        {
            get => GetProperty(() => ShowMode);
            set => SetProperty(() => ShowMode, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("ShowMode").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        /// 定位-灰度阈值
        /// </summary>
        public int LocThresh
        {
            get => GetProperty(() => LocThresh);
            set => SetProperty(() => LocThresh, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("LocThresh").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        /// 定位-半径上限
        /// </summary>
        public int MaxRadius
        {
            get => GetProperty(() => MaxRadius);
            set => SetProperty(() => MaxRadius, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("MaxRadius").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        /// 定位-半径下限
        /// </summary>
        public int MinRadius
        {
            get => GetProperty(() => MinRadius);
            set => SetProperty(() => MinRadius, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("MinRadius").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        /// 区域-有效径半径
        /// </summary>
        public int Radius
        {
            get => GetProperty(() => Radius);
            set => SetProperty(() => Radius, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("Radius").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        /// 区域-屏蔽径上限
        /// </summary>
        public int nMaxRadius
        {
            get => GetProperty(() => nMaxRadius);
            set => SetProperty(() => nMaxRadius, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("nMaxRadius").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        /// 区域-屏蔽径下限
        /// </summary>
        public int nMinRadius
        {
            get => GetProperty(() => nMinRadius);
            set => SetProperty(() => nMinRadius, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("nMinRadius").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        ///类型1-灰度阈值
        /// </summary>
        public int D1thresh
        {
            get => GetProperty(() => D1thresh);
            set => SetProperty(() => D1thresh, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("D1thresh").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        ///类型1-面积上限
        /// </summary>
        public int D1SizeMax
        {
            get => GetProperty(() => D1SizeMax);
            set => SetProperty(() => D1SizeMax, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("D1SizeMax").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        ///类型1-面积下限
        /// </summary>
        public int D1SizeMin
        {
            get => GetProperty(() => D1SizeMin);
            set => SetProperty(() => D1SizeMin, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("D1SizeMin").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        ///类型2-强度
        /// </summary>
        public int D2AdapSize
        {
            get => GetProperty(() => D2AdapSize);
            set => SetProperty(() => D2AdapSize, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("D2AdapSize").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        ///类型2-容差
        /// </summary>
        public int D2AdapC
        {
            get => GetProperty(() => D2AdapC);
            set => SetProperty(() => D2AdapC, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("D2AdapC").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        ///类型2-圆度下限
        /// </summary>
        public float D2RoundnessMin
        {
            get => GetProperty(() => D2RoundnessMin);
            set => SetProperty(() => D2RoundnessMin, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("D2RoundnessMin").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        ///类型2-矩形度下限
        /// </summary>
        public float D2RectangularityMin
        {
            get => GetProperty(() => D2RectangularityMin);
            set => SetProperty(() => D2RectangularityMin, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("D2RoundnessMin").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        ///类型2-面积上限
        /// </summary>
        public int D2sizeMax
        {
            get => GetProperty(() => D2sizeMax);
            set => SetProperty(() => D2sizeMax, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("D2sizeMax").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }

        /// <summary>
        ///类型2-面积下限
        /// </summary>
        public int D2sizeMin
        {
            get => GetProperty(() => D2sizeMin);
            set => SetProperty(() => D2sizeMin, value, () =>
            {
                XDocument config = XDocument.Load(filePath);
                config.Descendants("D2sizeMin").ElementAt(0).SetValue(value);
                config.Save(filePath);
            });
        }


        #endregion

    }
}
