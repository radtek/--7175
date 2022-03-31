// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "pch.h"

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

#include <opencv.hpp>
#include <opencv2/imgproc/types_c.h>
#include "putText.h"
using namespace cv;
using namespace std;

struct BmpBuf
{
	unsigned char* data_Output;
	int size;
	unsigned char* data_Input;
	int h;
	int w;

};

void DirtyTestG1R1(BmpBuf &data, char** input_Parameter, float* output_Parameter_Float)
{
#pragma region 本地参数
	bool flag = false;
	stringstream str;
	vector<Vec4i> hierarchy;
	vector<vector<Point2i>> contours;
	int _area = 0, err1 = 0, err2 = 0, tmm = -1, num;
	Mat src, temp, dst, labels, stats, centroids, toBlur, Polared, Blured, Diff, show, DePolared, mask, ROI;
	
	str << "G1R1" << endl;

	//src = Mat(data.h, data.w, CV_8UC1, data.data_Input);//
	src = imread("C:\\Users\\Administrator\\Desktop\\7175\\外观\\G1R1\\0324\\8.bmp", 0);
#pragma endregion

#pragma region 参数载入
	int ShowMode = atoi(input_Parameter[0]);//显示模式：0-正常显示 1-定位画面 2-线状脏污 3-点状脏污

	int LocThresh = atoi(input_Parameter[1]);	//定位-灰度阈值
	int MaxRadius = atoi(input_Parameter[2]);	//定位-最大半径
	int MinRadius = atoi(input_Parameter[3]);	//定位-最小半径

	int Radius = atoi(input_Parameter[4]);		//区域-镜片外径
	int eRadius = atoi(input_Parameter[5]);		//区域-有效径

	int D2Thresh = atoi(input_Parameter[7]);	//点状脏污识别-分割阈值
	int D2sizeMax = atoi(input_Parameter[8]);	//点状脏污识别-脏污最大面积
	int D2sizeMin = atoi(input_Parameter[9]);	//点状脏污识别-脏污最小面积


	int  D1AdapSize = atoi(input_Parameter[10]);	//脏污1-强度
	D1AdapSize = D1AdapSize * 2 + 1;
	int  D1AdapC = atoi(input_Parameter[11]);		//脏污1-容差
	float DoR = atof(input_Parameter[12]);		//脏污1-圆度上限 0-1
	int D1SizeMax = atoi(input_Parameter[14]);	//脏污1-面积上限
	int D1SizeMin = atoi(input_Parameter[15]);		//脏污1-面积下限
#pragma endregion

#pragma region 定位
	cvtColor(src, dst, COLOR_GRAY2RGB);

	threshold(src, temp, LocThresh, 255, THRESH_BINARY);
	findContours(temp, contours, hierarchy, CV_RETR_TREE, CV_CHAIN_APPROX_NONE, Point(0, 0));

	if (ShowMode == 1)
	{
		cvtColor(temp, dst, COLOR_GRAY2RGB);
	}

	for (size_t i = 0; i < contours.size(); i++)
	{
		vector<Point2i> tmpCont;
		convexHull(contours[i], tmpCont);
		double tmpArea = contourArea(tmpCont, 0);
		if (tmpArea > CV_PI*MaxRadius*MaxRadius || tmpArea < CV_PI*MinRadius*MinRadius)continue;
		if (tmpArea > _area)
		{
			_area = (long)tmpArea;
			tmm = i;
		}
	}

#pragma endregion

	if (tmm >= 0)
	{

#pragma region 圆心获取
		RotatedRect ell = fitEllipse(contours[tmm]);
		ellipse(dst, ell, Scalar(255, 0, 0), 2);

		circle(dst, ell.center, MaxRadius, Scalar(0, 255, 0), 2);
		circle(dst, ell.center, MinRadius, Scalar(0, 255, 0), 2);
		circle(dst, ell.center, Radius, Scalar(0, 0, 255), 2);
		circle(dst, ell.center, eRadius, Scalar(0, 255, 255), 2);
#pragma endregion

#pragma region 预处理
		warpPolar(src, Polared, Size(Radius, Radius * 2 * CV_PI), ell.center, Radius, WARP_POLAR_LINEAR);

		toBlur = Polared;
		blur(toBlur, Blured, Size(7, 101));
		Diff = toBlur - Blured;

		//按区域进行放大
		Diff(Rect(0, 0, eRadius, Radius * 2 * CV_PI)) *= 7;
		Diff(Rect(eRadius + 1, 0, Radius - eRadius - 1, Radius * 2 * CV_PI)) *= 4;
		warpPolar(Diff, DePolared, src.size(), ell.center, Radius, WARP_INVERSE_MAP);

		mask = Mat::zeros(DePolared.size(), DePolared.type());
		circle(mask, ell.center, Radius - 1, Scalar(255), -1);

		DePolared.copyTo(ROI, mask);
#pragma endregion

#pragma region 亮线识别

		adaptiveThreshold(ROI, show, 255, ADAPTIVE_THRESH_MEAN_C, THRESH_BINARY, D1AdapSize, D1AdapC);

		if (ShowMode == 2)
		{
			cvtColor(show, dst, COLOR_GRAY2RGB);
		}
		//threshold(DePolared, show, 180, 255, THRESH_BINARY);

		//vector<Vec4i> lines;
		//HoughLinesP(show, lines, 1, CV_PI / 180, 80, 60, 10);
		//for (size_t i = 0; i < lines.size(); i++)
		//{
		//	Vec4i l = lines[i];
		//	line(dst, Point(l[0], l[1]), Point(l[2], l[3]), Scalar(55, 100, 195), 2);
		//}

		Mat kernel = getStructuringElement(MORPH_CROSS, Size(7, 7));
		morphologyEx(show, show, MORPH_DILATE, kernel);

		kernel = getStructuringElement(MORPH_CROSS, Size(5, 5));
		morphologyEx(show, show, MORPH_ERODE, kernel);

		kernel = getStructuringElement(MORPH_CROSS, Size(3, 3));
		morphologyEx(show, show, MORPH_CLOSE, kernel);

		//kernel = getStructuringElement(MORPH_CROSS, Size(5, 5));
		//morphologyEx(show, show, MORPH_OPEN, kernel);

#if 0
		//利用圆度及面积筛选点状脏污
		num = connectedComponentsWithStats(show, labels, stats, centroids);

		//生成随机颜色，用于区分不同连通域
		RNG rng(10086);
		vector<Vec3b> colors;
		for (int i = 0; i < num; i++)
		{
			//使用均匀分布的随机数确定颜色
			Vec3b vec3 = Vec3b(rng.uniform(0, 256), rng.uniform(0, 256), rng.uniform(0, 256));
			colors.push_back(vec3);
		}

		for (size_t i = 0; i < num; i++)
		{
			int x = stats.at<int>(i, CC_STAT_LEFT);
			int y = stats.at<int>(i, CC_STAT_TOP);
			int w = stats.at<int>(i, CC_STAT_WIDTH);
			int h = stats.at<int>(i, CC_STAT_HEIGHT);
			int a = stats.at<int>(i, CC_STAT_AREA);

			if (a * DoL < CV_PI *  w * h)
			{
				rectangle(dst, Point(x, y), Point(x + w, y + h), Scalar(255, 0, 0));
			}

			// 外接矩形
			Rect rect(x, y, w, h);
			rectangle(dst, rect, colors[i], 1, 8, 0);
		}
#endif // 0

		findContours(show, contours, hierarchy, CV_RETR_TREE, CV_CHAIN_APPROX_NONE, Point(0, 0));

		for (size_t i = 0; i < contours.size(); i++)
		{
			RotatedRect box = minAreaRect(contours[i]);

			float area = contourArea(contours[i]);
			float len = arcLength(contours[i], 1);
			float roundness = (4 * CV_PI*area) / (len*len);

			float w = box.size.width;
			float h = box.size.height;
			float a = box.size.area();

			//float minrectArea = w * h;
			//float Rectangularity;
			//if (minrectArea == 0)
			//{
			//	Rectangularity = 0;
			//}
			//else
			//{
			//	Rectangularity = area / minrectArea;
			//}

			if (w < 1 || h < 1)continue;
			if (a < D1SizeMin || a>D1SizeMax)continue;

			if (roundness < DoR)
			{
				Point2f rect[4];
				box.points(rect);

				for (size_t i = 0; i < 4; i++)
				{
					line(dst, rect[i], rect[(i + 1) % 4], Scalar(255, 0, 0), 1);
				}

				err1++;
			}
		}
#pragma endregion

#pragma region 落尘识别
		threshold(ROI, show, D2Thresh, 255, THRESH_BINARY);

		if (ShowMode == 3)
		{
			cvtColor(show, dst, COLOR_GRAY2RGB);
		}

		kernel = getStructuringElement(MORPH_CROSS, Size(3, 3));
		morphologyEx(show, show, MORPH_OPEN, kernel);

		num = connectedComponentsWithStats(show, labels, stats, centroids);
		for (size_t i = 0; i < num; i++)
		{
			int x = stats.at<int>(i, CC_STAT_LEFT);
			int y = stats.at<int>(i, CC_STAT_TOP);
			int w = stats.at<int>(i, CC_STAT_WIDTH);
			int h = stats.at<int>(i, CC_STAT_HEIGHT);
			int a = stats.at<int>(i, CC_STAT_AREA);

			if (a > D2sizeMin && a < D2sizeMax)
			{
				rectangle(dst, Point(x, y), Point(x + w, y + h), Scalar(255, 255, 0));
				err2++;

				//stringstream str;
				//str << "面积" << a << endl;
				//putTextZH(dst, str.str().c_str(), Point(x + w, y + h), Scalar(0, 255, 0), 15, "黑体", 0);
			}

		}
#pragma endregion

		str << "毛丝数：" << err1 << endl;
		str << "落尘数：" << err2 << endl;

		if (err2 < 1 && err1 < 1)
		{
			flag = true;
		}
	}
	else
	{
		str << "未找到镜头" << endl;
	}


#pragma region 文字输入
	//字体大小
	int text_Size;
	text_Size = ((data.w* data.h / 10000 - 30) * 0.078 + 25) * 2;
	//位置
	Point text_Localtion01;
	text_Localtion01.x = text_Size / 3;
	text_Localtion01.y = text_Size / 3;
	Point text_Localtion02;
	text_Localtion02.x = text_Size / 3;
	text_Localtion02.y = data.h - text_Size * 4;
	Point text_Localtion03;
	text_Localtion03.x = text_Size / 3;
	text_Localtion03.y = data.h - text_Size * 3;

	Scalar fontColor = Scalar(0, 255, 0);
	if (!flag)fontColor = Scalar(0, 0, 255);

	std::string text = str.str();
	putTextZH(dst, text.c_str(), text_Localtion01, fontColor, text_Size, "黑体", 0);
#pragma endregion

#pragma region 结果返回
	output_Parameter_Float[0] = flag;
	output_Parameter_Float[1] = err1;
	output_Parameter_Float[2] = err2;
#pragma endregion

#pragma region 图片返回
	int size = dst.total() * dst.elemSize();
	data.size = size;
	data.h = dst.rows;
	data.w = dst.cols;

	data.data_Output = (uchar *)calloc(size, sizeof(uchar));
	std::memcpy(data.data_Output, dst.data, size * sizeof(BYTE));
#pragma endregion
}

void DirtyTestP2R1(BmpBuf &data, char** input_Parameter, float* output_Parameter_Float)
{
#pragma region 本地参数申明

	bool flag = false;
	stringstream str;
	vector<Vec4i> hierarchy;
	vector<vector<Point2i>> contours;
	int _area = 0, tmm = -1, err1 = 0, err2 = 0, num;
	Mat	src, dst, temp, labels, stats, centroids, toBlur, Polared, Blured, Diff, show, DePolared, mask, ROI, kernel;
	
	str << "P2R1" << endl;
	//src = Mat(data.h, data.w, CV_8UC1, data.data_Input);//默认判胶相机使用的是彩色相机
	src = imread("C:\\Users\\Administrator\\Desktop\\7175\\外观\\P2R1\\0325\\2\\26.bmp", 0);
	cvtColor(src, dst, COLOR_GRAY2RGB);
#pragma endregion

#pragma region 参数导入
	

	int ShowMode = atoi(input_Parameter[0]);//显示模式：0-正常显示 1-定位画面 2-线状脏污 3-点状脏污

	int LocThresh = atoi(input_Parameter[1]);//定位-灰度阈值
	int MaxRadius = atoi(input_Parameter[2]);//定位-最大半径
	int MinRadius = atoi(input_Parameter[3]);	//定位-最小半径


	int Radius = atoi(input_Parameter[4]);//镜片有效半径
	int nMaxRadius = atoi(input_Parameter[5]);//屏蔽区域-半径上限
	int nMinRadius = atoi(input_Parameter[6]);//屏蔽区域-半径下限

	int D1thresh = atoi(input_Parameter[7]);	//脏污1-灰度阈值
	int D1SizeMax = atoi(input_Parameter[8]);	//脏污1-面积上限
	int D1SizeMin = atoi(input_Parameter[9]);	//脏污1-面积下限
	

	int AdapSize = atoi(input_Parameter[10]);			//脏污2-强度
	AdapSize = AdapSize * 2 + 1;
	int AdapC = atoi(input_Parameter[11]);				//脏污2-容差
	float RoundnessMin = atof(input_Parameter[12]);		//脏污2-圆度下限
	float RectangularityMin = atof(input_Parameter[13]);//脏污2-矩形度下限
	int D2sizeMax = atoi(input_Parameter[14]);			//脏污2-最大面积
	int D2sizeMin = atoi(input_Parameter[15]);			//脏污2-最小面积
#pragma endregion

#pragma region 轮廓大小定位
	threshold(src, temp, LocThresh, 255, THRESH_BINARY);
	findContours(temp, contours, hierarchy, CV_RETR_TREE, CV_CHAIN_APPROX_NONE, Point(0, 0));

	if (ShowMode == 1)
	{
		cvtColor(temp, dst, COLOR_GRAY2RGB);
	}

	for (size_t i = 0; i < contours.size(); i++)
	{
		vector<Point2i> tmpCont;
		convexHull(contours[i], tmpCont);
		double tmpArea = contourArea(tmpCont, 0);
		if (tmpArea > CV_PI*MaxRadius*MaxRadius || tmpArea < CV_PI*MinRadius*MinRadius)continue;
		if (tmpArea > _area)
		{
			_area = (long)tmpArea;
			tmm = i;
		}
	}
#pragma endregion

	if (tmm >= 0)
	{
#pragma region 圆心获取

		RotatedRect ell = fitEllipse(contours[tmm]);
		ellipse(dst, ell, Scalar(255, 0, 0), 2);

		circle(dst, ell.center, MaxRadius, Scalar(0, 255, 0), 2);
		circle(dst, ell.center, MinRadius, Scalar(0, 255, 0), 2);
		circle(dst, ell.center, nMinRadius, Scalar(0, 0, 255), 2);
		circle(dst, ell.center, nMaxRadius, Scalar(0, 0, 255), 2);
		circle(dst, ell.center, Radius, Scalar(0, 0, 255), 2);

#pragma endregion

#pragma region 预处理
		warpPolar(src, Polared, Size(Radius, Radius * 2 * CV_PI), ell.center, Radius, WARP_POLAR_LINEAR);

		toBlur = Polared;
		blur(toBlur, Blured, Size(3, 101));

		Diff = toBlur - Blured;
		Diff *= 5;

		rectangle(Diff, Point(nMinRadius, 0), Point(nMaxRadius, Radius * 2 * CV_PI), Scalar(0), -1);
		warpPolar(Diff, DePolared, src.size(), ell.center, Radius, WARP_INVERSE_MAP);

		mask = Mat::zeros(DePolared.size(), DePolared.type());
		circle(mask, ell.center, Radius - 1, Scalar(255), -1);

		DePolared.copyTo(ROI, mask);
#pragma endregion

#pragma region 亮线识别
		threshold(ROI, show, D1thresh, 255, THRESH_BINARY);

		if (ShowMode == 2)
		{
			cvtColor(show, dst, COLOR_GRAY2RGB);
		}

		num = connectedComponentsWithStats(show, labels, stats, centroids);
		for (size_t i = 0; i < num; i++)
		{
			int x = stats.at<int>(i, CC_STAT_LEFT);
			int y = stats.at<int>(i, CC_STAT_TOP);
			int w = stats.at<int>(i, CC_STAT_WIDTH);
			int h = stats.at<int>(i, CC_STAT_HEIGHT);
			int a = stats.at<int>(i, CC_STAT_AREA);

			if (a > D1SizeMin && a < D1SizeMax)
			{
				rectangle(dst, Point(x, y), Point(x + w, y + h), Scalar(0, 255, 255));
				err1++;
			}

		}
#pragma endregion

#pragma region 落尘识别
		adaptiveThreshold(ROI, show, 255, ADAPTIVE_THRESH_GAUSSIAN_C, THRESH_BINARY, AdapSize * 2 + 1, AdapC);

		kernel = getStructuringElement(MORPH_ELLIPSE, Size(3, 3));
		morphologyEx(show, show, MORPH_CLOSE, kernel);

		if (ShowMode == 3)
		{
			cvtColor(show, dst, COLOR_GRAY2RGB);
		}

		findContours(show, contours, hierarchy, CV_RETR_TREE, CV_CHAIN_APPROX_NONE, Point(0, 0));

		for (size_t i = 0; i < contours.size(); i++)
		{
			RotatedRect box = minAreaRect(contours[i]);

			float area = contourArea(contours[i]);
			float len = arcLength(contours[i], 1);
			float Roundness = (4 * CV_PI*area) / (len*len);

			float w = box.size.width;
			float h = box.size.height;
			float a = box.size.area();

			float minrectArea = w * h;
			float Rectangularity;
			if (minrectArea == 0)
			{
				Rectangularity = 0;
			}
			else
			{
				Rectangularity = area / minrectArea;
			}

			if (w < 1 || h < 1)continue;
			if (Rectangularity < RectangularityMin)continue;
			if (Roundness < RoundnessMin)continue;

			if (a > D2sizeMin && a < D2sizeMax)
			{
				Point2f rect[4];
				box.points(rect);

				for (size_t i = 0; i < 4; i++)
				{
					line(dst, rect[i], rect[(i + 1) % 4], Scalar(255, 255, 0), 1);
				}

				//stringstream str;
				//str << "面积" << a << endl;
				//str << "圆度" << Roundness << endl;
				//str << "矩形度" << Rectangularity << endl;
				//putTextZH(dst, str.str().c_str(), box.center, Scalar(0, 255, 0), 15, "黑体", 0);

				err2++;
			}
		}
#pragma endregion

		str << "不良类型1：" << err1 << endl;
		str << "不良类型2：" << err2 << endl;


		if (err2 <1 && err1 <1)
		{
			flag = true;
		}
	}
	else
	{
		str << "未找到镜头！";
	}

#pragma region 文字输入
	//字体大小
	int text_Size;
	text_Size = ((data.w* data.h / 10000 - 30) * 0.078 + 25) * 2;
	//位置
	Point text_Localtion01;
	text_Localtion01.x = text_Size / 3;
	text_Localtion01.y = text_Size / 3;
	Point text_Localtion02;
	text_Localtion02.x = text_Size / 3;
	text_Localtion02.y = data.h - text_Size * 4;
	Point text_Localtion03;
	text_Localtion03.x = text_Size / 3;
	text_Localtion03.y = data.h - text_Size * 3;

	Scalar fontColor = Scalar(0, 255, 0);
	if (!flag)fontColor = Scalar(0, 0, 255);

	std::string text = str.str();
	putTextZH(dst, text.c_str(), text_Localtion01, fontColor, text_Size, "黑体", 0);
#pragma endregion

#pragma region 结果返回
	output_Parameter_Float[0] = flag;
	output_Parameter_Float[1] = err1;
	output_Parameter_Float[2] = err2;
#pragma endregion

#pragma region 图片返回
	int size = dst.total() * dst.elemSize();
	data.size = size;
	data.h = dst.rows;
	data.w = dst.cols;

	data.data_Output = (uchar *)calloc(size, sizeof(uchar));
	std::memcpy(data.data_Output, dst.data, size * sizeof(BYTE));
#pragma endregion
}

void ErrOutput(BmpBuf &data, char** input_Parameter, float* output_Parameter_Float)
{
	Mat src = Mat(data.h, data.w, CV_8UC1, data.data_Input);//默认非点胶后相机提供的原始图像为黑白图像
	Mat	output;
	stringstream str;
	cvtColor(src, output, COLOR_GRAY2RGB);

	str << "算法异常退出！";

#pragma region 文字输入
	//字体大小
	int text_Size;
	text_Size = ((data.w* data.h / 10000 - 30) * 0.078 + 25) * 2;
	//位置
	Point text_Localtion01;
	text_Localtion01.x = text_Size / 3;
	text_Localtion01.y = text_Size / 3;
	Point text_Localtion02;
	text_Localtion02.x = text_Size / 3;
	text_Localtion02.y = data.h - text_Size * 4;
	Point text_Localtion03;
	text_Localtion03.x = text_Size / 3;
	text_Localtion03.y = data.h - text_Size * 3;

	Scalar fontColor = Scalar(0, 255, 255);

	std::string text = str.str();
	putTextZH(output, text.c_str(), text_Localtion01, fontColor, text_Size, "黑体", 0);
#pragma endregion

#pragma region 结果返回
	output_Parameter_Float[0] = false;
#pragma endregion

#pragma region 图片返回
	int size = output.total() * output.elemSize();
	data.size = size;
	data.h = output.rows;
	data.w = output.cols;

	data.data_Output = (uchar *)calloc(size, sizeof(uchar));
	std::memcpy(data.data_Output, output.data, size * sizeof(BYTE));
#pragma endregion

}

bool MV_EntryPoint(int type, BmpBuf &data, char** input_Parameter, float* output_Parameter_Float)
{
	try
	{
		switch (type)
		{
		case 0: DirtyTestG1R1(data, input_Parameter, output_Parameter_Float); break;
		case 1: DirtyTestP2R1(data, input_Parameter, output_Parameter_Float); break;
		default:
			break;
		}
	}
	catch (const std::exception&)
	{
		ErrOutput(data, input_Parameter, output_Parameter_Float);
	}
	

	return false;
}

bool MV_Release(BmpBuf &data)
{
	delete data.data_Output;
	data.data_Output = NULL;

	data.size = 0;
	return 0;
}