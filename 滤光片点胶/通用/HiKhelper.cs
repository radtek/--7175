using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using MvCamCtrl.NET;
using System.Collections.ObjectModel;
using HandyControl.Controls;

namespace 滤光片点胶
{
    public class HiKhelper : DispatcherObject
    {
        ~HiKhelper()
        {
            if (isConnect == true)
            {
                Disconnect();
            }
        }

        /// <summary>
        /// 海康实例
        /// </summary>
        public MyCamera device = new MyCamera();

        /// <summary>
        /// 判断相机是否连接成功 true表示相机连接成功
        /// </summary>
        public bool isConnect = false;

        /// <summary>
        /// 判断相机是否处于触发模式 true表示处于触发模式
        /// </summary>
        public bool isTrigger = false;

        /// <summary>
        /// 相机通讯方式
        /// </summary>
        static uint nTLayerType = MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE;

        /// <summary>
        /// 设备列表
        /// </summary>
        static MyCamera.MV_CC_DEVICE_INFO_LIST stDevList;

        /// <summary>
        /// 回调函数
        /// </summary>
        MyCamera.cbOutputExdelegate pCallBackFunc;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string err = "";

        /// <summary>
        /// 相机信息，用于下拉框显示
        /// </summary>
        public struct CamInfo
        {
            public string Name { set; get; }
            public int ID { set; get; }
        }

        /// <summary>
        /// 相机信息列表
        /// </summary>
        public static ObservableCollection<CamInfo> CamInfos = new ObservableCollection<CamInfo>();

        /// <summary>
        /// 搜索本地相机
        /// </summary>
        static public void Search()
        {
            int nRet = MyCamera.MV_OK;
            string err = "";

            nRet = MyCamera.MV_CC_EnumDevices_NET(nTLayerType, ref stDevList);
            if (MyCamera.MV_OK != nRet)
            {
                //err = "相机枚举失败\n";
                err = String.Format("相机枚举失败,错误码:{0:X}\n", nRet);
                Growl.Error(err);
                //return nRet;
            }

            if (0 == stDevList.nDeviceNum)
            {
                err = String.Format("未找到相机\n");
                Growl.Error(err);
            }

            string name = "";
            for (int i = 0; i < stDevList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO stDevInfo;
                stDevInfo = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(stDevList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));

                if (stDevInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(stDevInfo.SpecialInfo.stGigEInfo, 0);
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    if (gigeInfo.chUserDefinedName != "")
                    {
                        name = ("Camera" + i.ToString() + " :GigE: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")");
                        //Growl.Info(("Camera" + i.ToString() + " :GigE: " + gigeInfo.chUserDefinedName + " (" + gigeInfo.chSerialNumber + ")"));
                    }
                    else
                    {
                        name = ("Camera" + i.ToString() + " :GigE: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")");
                        //Growl.Info(("Camera" + i.ToString() + " :GigE: " + gigeInfo.chManufacturerName + " " + gigeInfo.chModelName + " (" + gigeInfo.chSerialNumber + ")"));
                    }
                }
                else if (stDevInfo.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(stDevInfo.SpecialInfo.stUsb3VInfo, 0);
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chUserDefinedName != "")
                    {
                        name = ("Camera" + i.ToString() + " :USB: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")");
                        //Growl.Info(("Camera" + i.ToString() + " :USB: " + usbInfo.chUserDefinedName + " (" + usbInfo.chSerialNumber + ")"));
                    }
                    else
                    {
                        name = ("Camera" + i.ToString() + " :USB: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")");
                        //Growl.Info(("Camera" + i.ToString() + " :USB: " + usbInfo.chManufacturerName + " " + usbInfo.chModelName + " (" + usbInfo.chSerialNumber + ")"));
                    }
                }

                CamInfos.Add(new CamInfo() { Name = name, ID = i });
            }
        }

        /// <summary>
        /// 海康相机连接 连接后默认进入外部触发模式(软触发)
        /// </summary>
        /// <returns></returns>
        public int Connect(int n)
        {
            if (isConnect)
            {
                Growl.Warning("重复连接相机");
                return -1;
            }

            if (stDevList.nDeviceNum <= n)
            {
                err = String.Format("相机数目异常，请检查后重新连接\n");
                Growl.Error(err);
                return -1;
            }

            int nRet = MyCamera.MV_OK;
            MyCamera.MV_CC_DEVICE_INFO stDevInfo;

            stDevInfo = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(stDevList.pDeviceInfo[n], typeof(MyCamera.MV_CC_DEVICE_INFO));

            nRet = device.MV_CC_CreateDevice_NET(ref stDevInfo);
            if (MyCamera.MV_OK != nRet)
            {
                //err = "设备创建失败\n";
                err = String.Format("设备创建失败,错误码:{0:X}\n", nRet);
                Growl.Error(err);
                return nRet;
            }

            // Open device
            nRet = device.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                //err = "开启失败\n";
                err = String.Format("开启失败,错误码:{0:X}\n", nRet);

                Growl.Error(err);
                return nRet;
            }

            // Register Exception Callback
            pCallBackFunc = new MyCamera.cbOutputExdelegate(CallbackRGB);
            nRet = device.MV_CC_RegisterImageCallBackEx_NET(pCallBackFunc, IntPtr.Zero);
            if (MyCamera.MV_OK != nRet)
            {
                //err = "回调设置失败\n";
                err = String.Format("回调设置失败,错误码:{0:X}\n", nRet);

                Growl.Error(err);
                return nRet;
            }
            GC.KeepAlive(pCallBackFunc);

            nRet = device.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                //err = "采集开启失败\n";
                err = String.Format("采集开启失败,错误码:{0:X}\n", nRet);

                Growl.Error(err);
                return nRet;
            }

            //软触发
            nRet += device.MV_CC_SetEnumValue_NET("TriggerMode", (uint)1);
            nRet += device.MV_CC_SetEnumValue_NET("TriggerSource", 7);
            nRet += device.MV_CC_SetBoolValue_NET("TriggerCacheEnable", true);
            nRet += device.MV_CC_SetFloatValue_NET("AcquisitionFrameRate", (float)2);
            nRet += device.MV_CC_SetIntValue_NET("GevHeartbeatTimeout", (uint)500);//心跳时间
            if (MyCamera.MV_OK != nRet)
            {
                //err = "触发模式开启失败\n";
                err = String.Format("触发模式开启失败,错误码:{0:X}\n", nRet);

                Growl.Error(err);
                return nRet;
            }
            isConnect = true;
            isTrigger = true;
            return nRet;
        }

        /// <summary>
        /// 海康相机断开
        /// </summary>
        /// <returns></returns>
        public int Disconnect()
        {
            int nRet = MyCamera.MV_OK;

            // Close device
            nRet = device.MV_CC_CloseDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                //err = "设备关闭失败\n";
                err = String.Format("设备关闭失败,错误码:{0:X}\n", nRet);

                return nRet;
            }

            // Destroy device
            nRet = device.MV_CC_DestroyDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                //err = "设备销毁失败\n";
                err = String.Format("设备销毁失败,错误码:{0:X}\n", nRet);

                return nRet;
            }
            isConnect = false;
            return nRet;
        }

        /// <summary>
        /// 相机触发模式选择
        /// </summary>
        public void TriggerMode()
        {
            int nRet = 0;

            MyCamera.MVCC_ENUMVALUE mode = new MyCamera.MVCC_ENUMVALUE();
            device.MV_CC_GetTriggerMode_NET(ref mode);
            
            nRet += device.MV_CC_SetEnumValue_NET("TriggerMode", (uint)(1 - mode.nCurValue));

            if (MyCamera.MV_OK != nRet)
            {
                err = String.Format("触发模式开启失败,错误码:{0:X}\n", nRet);

                Growl.Error(err);
            }
            isTrigger = !isTrigger;
        }


        #region 暂放置

        /// <summary>
        /// 图像结构体
        /// </summary>
        public struct MV_IM_INFO
        {
            public IntPtr pData;
            public int width;
            public int height;
            public int pUser;
            public int CameraNum;
            public int nFrameLen;

        }

        /// <summary>
        /// 路由事件，回调原始帧
        /// </summary>
        public event EventHandler<MV_IM_INFO> MV_OnOriFrameInvoked;

        /// <summary>
        /// 外部委托
        /// </summary>
        /// <param name="imInfo"></param>
        private void MV_Show(MV_IM_INFO imInfo)
        {
            MV_OnOriFrameInvoked?.Invoke(this, imInfo);
        }

        /// <summary>
        /// 外部委托
        /// </summary>
        /// <param name="imInfo"></param>
        private delegate void MV_MShow(MV_IM_INFO imInfo);

        /// <summary>
        /// 单相机回调函数
        /// </summary>
        /// <param name="pData"></param>
        /// <param name="pFrameInfo"></param>
        /// <param name="pUser"></param>
        private void CallbackRGB(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo, IntPtr pUser)
        {
            if ((int)pUser == 0)
            {
                lock (this)
                {
                    // 拷贝中间内存区域
                    if (buf == null)
                    {
                        buf = new byte[pFrameInfo.nFrameLen];
                        pBuf = Marshal.UnsafeAddrOfPinnedArrayElement(buf, 0);
                    }
                    Marshal.Copy(pData, buf, 0, (int)pFrameInfo.nFrameLen);
                }


                Dispatcher.BeginInvoke(new MV_MShow(MV_Show),
                              new MV_IM_INFO
                              {
                                  pData = pBuf,
                                  width = (int)pFrameInfo.nWidth,
                                  height = (int)pFrameInfo.nHeight,
                                  pUser = (int)0,
                                  nFrameLen = (int)pFrameInfo.nFrameLen
                              });
            }
        }

        private byte[] buf;
        private IntPtr pBuf;
        #endregion
        
    }
}
