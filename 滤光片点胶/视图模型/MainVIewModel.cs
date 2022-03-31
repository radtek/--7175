using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace 滤光片点胶
{
    public class MainViewModel:ViewModelBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public MainViewModel()
        {
            HiKhelper.Search();

            SeletedCam = 0;
            camParamViewModel = MultiView.DictPanel[0].camera;
            IsCamOn = new ObservableCollection<bool>();

            int i = 0;

            MySerial.MV_Mess += Serial_OnTrigger;

            foreach (var item in MultiView.DictPanel.Values)
            {
                IsCamOn.Add(false);
                item.Init("./Para/Cam"+i.ToString());
                item.camera.MV_OnSendMess += SY_MV_OnSendMsg;
                i++;
            }
            IsCamOn.Add(false);

            CommboxID = new int[10];
            for (i = 0; i < 10; i++)
            {
                CommboxID[i] = i;
            }

            Version = Application.ResourceAssembly.GetName().Version.ToString();
        }

        #region 绑定参数

        /// <summary>
        /// 前台绑定参数
        /// </summary>
        public CameraViewModel camParamViewModel
        {
            get => GetProperty(() => camParamViewModel);
            set => SetProperty(() => camParamViewModel, value);
        }

        /// <summary>
        /// 当前被选中相机
        /// </summary>
        public int SeletedCam
        {
            get => GetProperty(() => SeletedCam);
            set => SetProperty(() => SeletedCam, value, () =>
            {
                if (value >= 0)
                {
                    camParamViewModel = MultiView.DictPanel[SeletedCam].camera;
                }
            });
        }

        /// <summary>
        /// 通讯连接状态
        /// </summary>
        public ObservableCollection<bool> IsCamOn
        {
            get => GetProperty(() => IsCamOn);
            set => SetProperty(() => IsCamOn, value);
        }

        /// <summary>
        /// 触发模式
        /// </summary>
        public bool IsTrigger
        {
            get => GetProperty(() => IsTrigger);
            set => SetProperty(() => IsTrigger, value);
        }

        /// <summary>
        /// Commbox选项
        /// </summary>
        public int[] CommboxID
        {
            get => GetProperty(() => CommboxID);
            set => SetProperty(() => CommboxID, value);
        }

        /// <summary>
        /// 软件版本号
        /// </summary>
        public string Version
        {
            get => GetProperty(() => Version);
            set => SetProperty(() => Version, value);
        }
        #endregion

        #region Commands

        /// <summary>
        /// 模式切换
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void ModeSwitchCommand(object obj)
        {
            camParamViewModel.camera.TriggerMode();
        }

        /// <summary>
        /// 开始测试
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void StartCommand(object obj)
        {
            Growl.Clear();

            for (int i = 0; i < MultiView.DictPanel.Count; i++)
            {
                MultiView.DictPanel[i].camera.Connect();
                if (MultiView.DictPanel[i].camera.camera.isConnect)
                {
                    IsCamOn[i] = true;
                    //MultiView.DictPanel[i].camera.camera.TriggerMode();
                }
                
            }

            IsCamOn[MultiView.DictPanel.Count] = MySerial.OpenPort();

        }

        /// <summary>
        /// 停止测试
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void StopCommand(object obj)
        {
            Growl.Clear();

            for (int i = 0; i < MultiView.DictPanel.Count; i++)
            {
                MultiView.DictPanel[i].camera.Disconnect();
                if (!MultiView.DictPanel[i].camera.camera.isConnect)
                {
                    IsCamOn[i] = false;
                }
            }
            IsCamOn[MultiView.DictPanel.Count] = false;
        }

        /// <summary>
        /// 参数设置
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void ConfigCommand(object obj)
        {
            Growl.Clear();
            LoginWindow loginWindow = new LoginWindow();
            if (true == loginWindow.ShowDialog())
            {
                ConfigWindow configWindow = new ConfigWindow();
                configWindow.DataContext = this;
                configWindow.Show();
            }
        }

        /// <summary>
        /// 通讯设置
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void CommunicationCommand(object obj)
        {
            Growl.Clear();
            LoginWindow loginWindow = new LoginWindow();
            if (true == loginWindow.ShowDialog())
            {
                ComWindow comConfig = new ComWindow();
                comConfig.DataContext = MySerial;
                comConfig.Show();
            }
        } 
        
        /// <summary>
        /// 导出配置
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void UploadCommand(object obj)
        {
            Growl.Clear();
        }

        /// <summary>
        /// 导入配置
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void DownloadCommand(object obj)
        {
            Growl.Clear();
        }

        /// <summary>
        /// 退出软件
        /// </summary>
        /// <param name="obj"></param>
        [AsyncCommand]
        public void ExitCommand(object obj)
        {
            for (int i = 0; i < MultiView.DictPanel.Count; i++)
            {
                MultiView.DictPanel[i].camera.Disconnect();
                if (!MultiView.DictPanel[i].camera.camera.isConnect)
                {
                    IsCamOn[i] = false;
                }
            }
            IsCamOn[MultiView.DictPanel.Count] = false;

            Application.Current.Shutdown(-1);
        }

        #endregion

        #region 通讯相关

        /// <summary>
        /// 串口通讯实例
        /// </summary>
        MySerialPort MySerial = new MySerialPort();

        /// <summary>
        /// 信息分发
        /// </summary> 
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Serial_OnTrigger(object sender, string e)
        {
            string str = new string(e.ToList().Where(c => c != '\0').ToArray());
            if (str.Length != 1)
            {
                MySerial.WriteLine("接收无效信息：" + str, "Red");
                return;
            }

            int nRet = 0;
            switch (str[0])
            {
                case 'M':
                    nRet = MultiView.DictPanel[0].camera.camera.device.MV_CC_SetCommandValue_NET("TriggerSoftware"); break;
                case 'm':
                    nRet = MultiView.DictPanel[1].camera.camera.device.MV_CC_SetCommandValue_NET("TriggerSoftware"); break;
                //case '3':
                //    nRet = MultiView.DictPanel[2].camera.camera.device.MV_CC_SetCommandValue_NET("TriggerSoftware"); break;
                default:
                    break;
            }
            MySerial.WriteLine("接收信息：" + str);
        }

        /// <summary>
        /// 结果回复
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        private void SY_MV_OnSendMsg(object sender, string msg)
        {
            MySerial.SendCommand(msg);
        }

        #endregion
    }
}
