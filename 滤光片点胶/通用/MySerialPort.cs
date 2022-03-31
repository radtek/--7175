using DevExpress.Mvvm;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace 滤光片点胶
{
    public class MySerialPort : ViewModelBase
    {
        private bool close = false;

        /// <summary>
        /// 界面显示
        /// </summary>
        public static TextBlock OriginalTextBlock = null;

        /// <summary>
        /// 首标
        /// </summary>
        public string CmdTag => "[" + DateTime.Now.ToString("MM/dd") + " " + DateTime.Now.ToLongTimeString() + "]";

        /// <summary>
        /// 串口实例
        /// </summary>
        public SerialPort serialPort;

        /// <summary>
        /// 事件
        /// </summary>
        public event EventHandler<string> MV_Mess;

        /// <summary>
        /// 信息队列
        /// </summary>
        Queue<string> Mess = new Queue<string>();

        SerialDataReceivedEventHandler handler;

        /// <summary>
        /// 信息接收
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (close) return;

            byte[] readBuffer = new byte[serialPort.ReadBufferSize];
            serialPort.Read(readBuffer, 0, readBuffer.Length);
            string str = Encoding.Default.GetString(readBuffer);

            MV_Mess?.Invoke(this, str);
        }

        /// <summary>
        /// 新建配置文件
        /// </summary>
        private void InitErr()
        {
            try
            {
                //获得文件路径
                string localFilePath = "";
                localFilePath = "./Para/ComConfig.xml";
                XDocument xdoc = new XDocument();
                XDeclaration xdec = new XDeclaration("1.0", "utf-8", "yes");
                xdoc.Declaration = xdec;

                XElement rootEle;
                XElement classEle;
                //XElement childEle;

                //添加根节点
                rootEle = new XElement("CamConfig");
                xdoc.Add(rootEle);

                classEle = new XElement("SelectedSerial", SelectedSerial);
                rootEle.Add(classEle);

                classEle = new XElement("BaudRate", BaudRate);
                rootEle.Add(classEle);

                classEle = new XElement("DataBits", DataBits);
                rootEle.Add(classEle);

                classEle = new XElement("SelectedParity", SelectedParity);
                rootEle.Add(classEle);

                classEle = new XElement("SelectedStopBits", SelectedStopBits);
                rootEle.Add(classEle);

                xdoc.Save(localFilePath);
            }
            catch
            {
                Growl.Error("[" + "./Para/CamConfig.xml" + "]生成失败！");
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            int ret = 0;
            string filePath = "./Para/ComConfig.xml";

            try
            {
                if (XmlHelper.Exists("./Para", "ComConfig.xml"))
                {
                    XDocument Config = XDocument.Load(filePath);

                    SelectedSerial = int.Parse(Config.Descendants("SelectedSerial").ElementAt(0).Value);
                    BaudRate = int.Parse(Config.Descendants("BaudRate").ElementAt(0).Value);
                    DataBits = int.Parse(Config.Descendants("DataBits").ElementAt(0).Value);
                    SelectedParity = int.Parse(Config.Descendants("SelectedParity").ElementAt(0).Value);
                    SelectedStopBits = int.Parse(Config.Descendants("SelectedStopBits").ElementAt(0).Value);
                }
                else
                {
                    Growl.Error("串口配置丢失！");
                    InitErr();
                    ret++;
                }
            }
            catch
            {
                Growl.Error("串口配置损坏！");
                InitErr();
            }

            
        }

        #region 构造/析构 函数

        ~MySerialPort()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                ClosePort();
            }
        }

        public MySerialPort()
        {
            SerialInfos = new ObservableCollection<CommboxInfo>();
            ParityInfos = new ObservableCollection<CommboxInfo>();
            StopBitsInfos = new ObservableCollection<CommboxInfo>();
            
            ParityInfos.Add(new CommboxInfo() { Name = "None", ID = 0 });
            ParityInfos.Add(new CommboxInfo() { Name = "Odd", ID = 1 });
            ParityInfos.Add(new CommboxInfo() { Name = "Even", ID = 2 });
            ParityInfos.Add(new CommboxInfo() { Name = "Mark", ID = 3 });
            ParityInfos.Add(new CommboxInfo() { Name = "Space", ID = 4 });

            StopBitsInfos.Add(new CommboxInfo() { Name = "None", ID = 0 });
            StopBitsInfos.Add(new CommboxInfo() { Name = "One", ID = 1 });
            StopBitsInfos.Add(new CommboxInfo() { Name = "Two", ID = 2 });
            StopBitsInfos.Add(new CommboxInfo() { Name = "OnePointFive", ID = 3 });

            //配置文件读取
            Init();

            Search();
        }

        #endregion
        
        #region 外调函数
        
        /// <summary>
        /// 打印一条信息到界面
        /// </summary>
        /// <param name="str"></param>
        public void WriteLine(string str,string color = "white")
        {
            Application.Current.Dispatcher.Invoke(new Action(() => 
            {
                try
                {
                    Run run = new Run(CmdTag + str + "\r");

                    switch (color)
                    {
                        case "Blue": run.Foreground = new SolidColorBrush(Colors.Blue); break;
                        case "Green": run.Foreground = new SolidColorBrush(Colors.Green); break;
                        case "Red": run.Foreground = new SolidColorBrush(Colors.Red); break;
                        default:
                            run.Foreground = new SolidColorBrush(Colors.White); break;
                    }

                    OriginalTextBlock.Inlines.Add(run);

                    (OriginalTextBlock.Parent as System.Windows.Controls.ScrollViewer).ScrollToEnd();
                    while (OriginalTextBlock.Inlines.Count > 10)
                        OriginalTextBlock.Inlines.Remove(OriginalTextBlock.Inlines.ElementAt(1));
                }
                catch
                {
                    Growl.Error("控制台输出失败");
                }
            }));
        }

        /// <summary>
        /// 发送一条信息
        /// </summary>
        /// <param name="CommandString"></param>
        public void SendCommand(string CommandString)
        {
            try
            {
                byte[] WriteBuffer = Encoding.ASCII.GetBytes(CommandString);
                serialPort.Write(WriteBuffer, 0, WriteBuffer.Length);

                WriteLine("信息发送成功：" + CommandString, "Green");
            }
            catch (Exception)
            {

                WriteLine("信息发送失败：" + CommandString,"Red");
            }

        }

        /// <summary>
        /// 打开串口
        /// </summary>
        /// <returns></returns>
        public bool OpenPort()
        {
            try//这里写成异常处理的形式以免串口打不开程序崩溃
            {
                string PortName = SerialInfos[SelectedSerial].Name;
                Parity parity = (Parity)ParityInfos[SelectedParity].ID;
                StopBits stopBits = (StopBits)StopBitsInfos[SelectedStopBits].ID;
                
                serialPort = new SerialPort(PortName, BaudRate, parity, DataBits, stopBits);
                handler = new SerialDataReceivedEventHandler(serialPort_DataReceived);
                serialPort.DataReceived += handler;
                serialPort.ReceivedBytesThreshold = 1;
                serialPort.RtsEnable = true;
                

                serialPort.Open();
                GC.SuppressFinalize(serialPort.BaseStream);
                close = false;

            }
            catch
            {
                Growl.Error("串口打开失败");
                if (serialPort != null) serialPort.Close();
                return false;
            }

            if (serialPort.IsOpen)
            {
                return true;
            }
            else
            {
                Growl.Error("串口打开失败");
                return false;
            }
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        /// <returns></returns>
        public bool ClosePort()
        {
            try
            {
                serialPort.DataReceived -= handler;
                close = true;
                GC.ReRegisterForFinalize(serialPort.BaseStream);
                serialPort.Close();

                if (!serialPort.IsOpen)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// 搜索有效端口
        /// </summary>
        /// <returns></returns>
        public bool Search()
        {
            int i = 0;
            ObservableCollection<CommboxInfo> tempInfos = new ObservableCollection<CommboxInfo>();

            foreach (string portName in SerialPort.GetPortNames())
            {
                tempInfos.Add(new CommboxInfo() { Name = portName, ID = i++ });
                SerialInfos.Add(new CommboxInfo() { Name = portName, ID = i++ });
            }

            SerialInfos = tempInfos;
            if (SerialInfos.Count == 0)
            {
                Growl.Warning("未找到有效串口");
            }

            return false;
        }

        #endregion

        #region 绑定参数

        /// <summary>
        /// 相机信息，用于下拉框显示
        /// </summary>
        public struct CommboxInfo
        {
            public string Name { set; get; }
            public int ID { set; get; }
        }

        /// <summary>
        /// 串口信息列表
        /// </summary>
        public ObservableCollection<CommboxInfo> SerialInfos
        {
            get => GetProperty(() => SerialInfos);
            set => SetProperty(() => SerialInfos, value);
        }

        /// <summary>
        /// 当前所选端口
        /// </summary>
        public int SelectedSerial
        {
            get => GetProperty(() => SelectedSerial);
            set => SetProperty(() => SelectedSerial, value,()=> 
            {
                XDocument config = XDocument.Load("./Para/ComConfig.xml");
                config.Descendants("SelectedSerial").ElementAt(0).SetValue(value);
                config.Save("./Para/ComConfig.xml");
            });
        }

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate
        {
            get => GetProperty(() => BaudRate);
            set => SetProperty(() => BaudRate, value, () =>
            {
                XDocument config = XDocument.Load("./Para/ComConfig.xml");
                config.Descendants("BaudRate").ElementAt(0).SetValue(value);
                config.Save("./Para/ComConfig.xml");
            });
        }

        /// <summary>
        /// 奇偶校验
        /// </summary>
        public ObservableCollection<CommboxInfo> ParityInfos
        {
            get => GetProperty(() => ParityInfos);
            set => SetProperty(() => ParityInfos, value);
        }

        /// <summary>
        /// 当前所选校验
        /// </summary>
        public int SelectedParity
        {
            get => GetProperty(() => SelectedParity);
            set => SetProperty(() => SelectedParity, value, () =>
            {
                XDocument config = XDocument.Load("./Para/ComConfig.xml");
                config.Descendants("SelectedParity").ElementAt(0).SetValue(value);
                config.Save("./Para/ComConfig.xml");
            });
        }

        /// <summary>
        /// 当前所选停止位
        /// </summary>
        public int DataBits
        {
            get => GetProperty(() => DataBits);
            set => SetProperty(() => DataBits, value, () =>
            {
                XDocument config = XDocument.Load("./Para/ComConfig.xml");
                config.Descendants("DataBits").ElementAt(0).SetValue(value);
                config.Save("./Para/ComConfig.xml");
            });
        }

        /// <summary>
        /// 奇偶校验
        /// </summary>
        public ObservableCollection<CommboxInfo> StopBitsInfos
        {
            get => GetProperty(() => StopBitsInfos);
            set => SetProperty(() => StopBitsInfos, value);
        }

        /// <summary>
        /// 当前所选停止位
        /// </summary>
        public int SelectedStopBits
        {
            get => GetProperty(() => SelectedStopBits);
            set => SetProperty(() => SelectedStopBits, value, () =>
            {
                XDocument config = XDocument.Load("./Para/ComConfig.xml");
                config.Descendants("SelectedStopBits").ElementAt(0).SetValue(value);
                config.Save("./Para/ComConfig.xml");
            });
        }

#if false
        /// <summary>
        /// 端口号
        /// </summary>
        public string PortName
        {
            get => GetProperty(() => PortName);
            set => SetProperty(() => PortName, value);
        }

        /// <summary>
        /// 触发信号
        /// </summary>
        public string TriggerMess
        {
            get => GetProperty(() => TriggerMess);
            set => SetProperty(() => TriggerMess, value);
        }

        /// <summary>
        /// 回复信息-OK
        /// </summary>
        public string OK_Ret
        {
            get => GetProperty(() => OK_Ret);
            set => SetProperty(() => OK_Ret, value);
        }

        /// <summary>
        /// 回复信息-NG
        /// </summary>
        public string NG_Ret
        {
            get => GetProperty(() => NG_Ret);
            set => SetProperty(() => NG_Ret, value);
        }
#endif

        /// <summary>
        /// 控制台文本显示
        /// </summary>
        public string console
        {
            get => GetProperty(() => console);
            set => SetProperty(() => console, value);
        }
        #endregion

    }
}
