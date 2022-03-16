using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace 滤光片点胶
{
    public static class MultiView
    {
        public static int CurrentModel = 0;
        public static Dictionary<int, CellPanel> DictPanel = null;
        private static Grid OriginalGrid = null;
        private static Grid TempGrid1 = new Grid();
        private static Grid TempGrid2 = new Grid();
        private static int MaxCellNums = 0;

        /// <summary>
        /// 初始化页面
        /// </summary>
        /// <param name="grid">主体</param>
        /// <param name="cellNums">最大画面数</param>
        public static void InitGrid(Grid grid, int cellNums)
        {
            MultiView.OriginalGrid = grid;
            MultiView.MaxCellNums = cellNums;

            MultiView.DictPanel = new Dictionary<int, CellPanel>();
            for (int index = 0; index < MultiView.MaxCellNums; index++)
            {
                CellPanel cellPanel = new CellPanel();
                cellPanel.Index = index;
                MultiView.DictPanel.Add(index, cellPanel);
            }
        }

        /// <summary>
        /// 设置当前画面数
        /// </summary>
        /// <param name="model"></param>
        public static void SetCurrentModel(int model)
        {
            #region 弃用
#if false
             switch (model)
            {
                case 1:
                    MultiView.SetModelByColumnAndRow(1, 1);
                    break;
                case 3:
                    MultiView.SetModel3();
                    break;
                case 4:
                    MultiView.SetModelByColumnAndRow(2, 2);
                    break;
                case 6:
                    MultiView.SetModel6();
                    break;
                case 8:
                    MultiView.SetModel8();
                    break;
                case 9:
                    MultiView.SetModelByColumnAndRow(3, 3);
                    break;
                case 10:
                    MultiView.SetModel10();
                    break;
                case 16:
                    MultiView.SetModelByColumnAndRow(4, 4);
                    break;
                case 25:
                    MultiView.SetModelByColumnAndRow(5, 5);
                    break;
            }
#endif
            #endregion

            if (model <= 0)
            {
                Growl.Error("分屏数错误");
                return;
            }

            int num = (int)Math.Sqrt(model);
            if (model > num * num) num++;
            MultiView.CurrentModel = model;
            MultiView.SetModelByColumnAndRow(num, num);
        }

        /// <summary>
        /// 获取当前画面
        /// </summary>
        /// <returns></returns>
        public static CellPanel CurrentCellPanel()
        {
            for (int index = 1; index <= MultiView.MaxCellNums; index++)
            {
                if (MultiView.DictPanel[index].Selected)
                {
                    return MultiView.DictPanel[index];
                }
            }
            return null;
        }

        /// <summary>
        /// 通过行列数设置画面分割
        /// </summary>
        /// <param name="column">列</param>
        /// <param name="row">行</param>
        public static void SetModelByColumnAndRow(int column, int row)
        {
            MultiView.Clear();
            MultiView.CreateColumnAndRow(MultiView.OriginalGrid, row, column);

            int num = 0;
            for (int rowIndex = 0; rowIndex < row; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < column; columnIndex++)
                {
                    CellPanel cellPanel = MultiView.DictPanel[num];
                    Grid.SetColumn(cellPanel, columnIndex);
                    Grid.SetRow(cellPanel, rowIndex);
                    MultiView.OriginalGrid.Children.Add(cellPanel);
                    num++;

                    if (num >= CurrentModel) return;
                }
            }
        }

        #region 暂未使用

        /// <summary>
        /// 三画面
        /// </summary>
        public static void SetModel3()
        {
            MultiView.Clear();
            ColumnDefinition columnDefinition1 = new ColumnDefinition();
            ColumnDefinition columnDefinition2 = new ColumnDefinition();
            columnDefinition1.Width = new GridLength(0.618, GridUnitType.Star);
            columnDefinition2.Width = new GridLength(0.382, GridUnitType.Star);
            MultiView.OriginalGrid.ColumnDefinitions.Add(columnDefinition1);
            MultiView.OriginalGrid.ColumnDefinitions.Add(columnDefinition2);
            CellPanel cellPanel = MultiView.DictPanel[1];
            Grid.SetColumn(cellPanel, 0);
            MultiView.OriginalGrid.Children.Add(cellPanel);

            for (int index1 = 0; index1 < 2; ++index1)
            {
                cellPanel = MultiView.DictPanel[index1 + 2];
                MultiView.TempGrid1.RowDefinitions.Add(new RowDefinition());
                Grid.SetRow(cellPanel, index1);
                MultiView.TempGrid1.Children.Add(cellPanel);
            }

            Grid.SetColumn(MultiView.TempGrid1, 1);
            MultiView.OriginalGrid.Children.Add(MultiView.TempGrid1);
        }

        /// <summary>
        /// 六画面
        /// </summary>
        public static void SetModel6()
        {
            MultiView.Clear();
            MultiView.CreateColumnAndRow(MultiView.OriginalGrid, 2, 2);
            MultiView.OriginalGrid.ColumnDefinitions[0].Width = new GridLength(0.66, GridUnitType.Star);
            MultiView.OriginalGrid.ColumnDefinitions[1].Width = new GridLength(0.34, GridUnitType.Star);
            MultiView.OriginalGrid.RowDefinitions[0].Height = new GridLength(0.67, GridUnitType.Star);
            MultiView.OriginalGrid.RowDefinitions[1].Height = new GridLength(0.33, GridUnitType.Star);

            CellPanel cellPanel = MultiView.DictPanel[1];
            Grid.SetColumn(cellPanel, 0);
            Grid.SetRow(cellPanel, 0);
            MultiView.OriginalGrid.Children.Add(cellPanel);

            //p2,p3 画面
            Grid.SetColumn(MultiView.TempGrid1, 1);
            Grid.SetRow(MultiView.TempGrid1, 0);

            for (int index = 0; index < 2; ++index)
            {
                cellPanel = MultiView.DictPanel[index + 2];
                MultiView.TempGrid1.RowDefinitions.Add(new RowDefinition());
                Grid.SetRow(cellPanel, index);
                MultiView.TempGrid1.Children.Add(cellPanel);
            }
            MultiView.OriginalGrid.Children.Add(MultiView.TempGrid1);
            //p4,p5 画面
            Grid.SetColumn(MultiView.TempGrid2, 0);
            Grid.SetRow(MultiView.TempGrid2, 1);

            for (int index = 0; index < 2; ++index)
            {
                cellPanel = MultiView.DictPanel[index + 4];

                MultiView.TempGrid2.ColumnDefinitions.Add(new ColumnDefinition());
                Grid.SetColumn(cellPanel, index);
                MultiView.TempGrid2.Children.Add(cellPanel);
            }
            MultiView.OriginalGrid.Children.Add(MultiView.TempGrid2);

            cellPanel = MultiView.DictPanel[6];
            Grid.SetRow(cellPanel, 1);
            Grid.SetColumn(cellPanel, 1);
            MultiView.OriginalGrid.Children.Add(cellPanel);
        }

        /// <summary>
        /// 八画面
        /// </summary>
        public static void SetModel8()
        {
            MultiView.Clear();
            MultiView.CreateColumnAndRow(MultiView.OriginalGrid, 2, 2);
            MultiView.OriginalGrid.ColumnDefinitions[0].Width = new GridLength(0.75, GridUnitType.Star);
            MultiView.OriginalGrid.ColumnDefinitions[1].Width = new GridLength(0.25, GridUnitType.Star);
            MultiView.OriginalGrid.RowDefinitions[0].Height = new GridLength(0.75, GridUnitType.Star);
            MultiView.OriginalGrid.RowDefinitions[1].Height = new GridLength(0.25, GridUnitType.Star);

            CellPanel cellPanel = MultiView.DictPanel[1];
            Grid.SetColumn(cellPanel, 0);
            Grid.SetRow(cellPanel, 0);
            MultiView.OriginalGrid.Children.Add(cellPanel);

            //p2,p3,p4 画面
            Grid.SetColumn(MultiView.TempGrid1, 1);
            Grid.SetRow(MultiView.TempGrid1, 0);

            for (int index = 0; index < 3; ++index)
            {
                cellPanel = MultiView.DictPanel[index + 2];

                MultiView.TempGrid1.RowDefinitions.Add(new RowDefinition());
                Grid.SetRow(cellPanel, index);
                MultiView.TempGrid1.Children.Add(cellPanel);
            }
            MultiView.OriginalGrid.Children.Add(MultiView.TempGrid1);
            //p5,p6,p7 画面
            Grid.SetColumn(MultiView.TempGrid2, 0);
            Grid.SetRow(MultiView.TempGrid2, 1);

            for (int index = 0; index < 3; ++index)
            {
                cellPanel = MultiView.DictPanel[index + 5];

                MultiView.TempGrid2.ColumnDefinitions.Add(new ColumnDefinition());
                Grid.SetColumn(cellPanel, index);
                MultiView.TempGrid2.Children.Add(cellPanel);
            }
            MultiView.OriginalGrid.Children.Add(MultiView.TempGrid2);

            cellPanel = MultiView.DictPanel[8];
            Grid.SetRow(cellPanel, 1);
            Grid.SetColumn(cellPanel, 1);
            MultiView.OriginalGrid.Children.Add(cellPanel);
        }

        /// <summary>
        /// 十画面
        /// </summary>
        public static void SetModel10()
        {
            MultiView.Clear();
            MultiView.CreateColumnAndRow(MultiView.OriginalGrid, 2, 2);
            MultiView.OriginalGrid.ColumnDefinitions[0].Width = new GridLength(0.77, GridUnitType.Star);
            MultiView.OriginalGrid.ColumnDefinitions[1].Width = new GridLength(0.23, GridUnitType.Star);
            MultiView.OriginalGrid.RowDefinitions[0].Height = new GridLength(0.8, GridUnitType.Star);
            MultiView.OriginalGrid.RowDefinitions[1].Height = new GridLength(0.2, GridUnitType.Star);

            CellPanel cellPanel = MultiView.DictPanel[1];
            Grid.SetColumn(cellPanel, 0);
            Grid.SetRow(cellPanel, 0);
            MultiView.OriginalGrid.Children.Add(cellPanel);

            //p2,p3,p4,p5 画面
            Grid.SetColumn(MultiView.TempGrid1, 1);
            Grid.SetRow(MultiView.TempGrid1, 0);

            for (int index = 0; index < 4; ++index)
            {
                cellPanel = MultiView.DictPanel[index + 2];

                MultiView.TempGrid1.RowDefinitions.Add(new RowDefinition());
                Grid.SetRow(cellPanel, index);
                MultiView.TempGrid1.Children.Add(cellPanel);
            }
            MultiView.OriginalGrid.Children.Add(MultiView.TempGrid1);
            //p6,p7,p8,p9 画面
            Grid.SetColumn(MultiView.TempGrid2, 0);
            Grid.SetRow(MultiView.TempGrid2, 1);

            for (int index = 0; index < 4; ++index)
            {
                cellPanel = MultiView.DictPanel[index + 6];

                MultiView.TempGrid2.ColumnDefinitions.Add(new ColumnDefinition());
                Grid.SetColumn(cellPanel, index);
                MultiView.TempGrid2.Children.Add(cellPanel);
            }
            MultiView.OriginalGrid.Children.Add(MultiView.TempGrid2);

            cellPanel = MultiView.DictPanel[10];
            Grid.SetRow(cellPanel, 1);
            Grid.SetColumn(cellPanel, 1);
            MultiView.OriginalGrid.Children.Add(cellPanel);
        }

        #endregion

        /// <summary>
        /// 索引画面全屏
        /// </summary>
        /// <param name="index">画面索引</param>
        public static void SetFullScreen(int index)
        {
            MultiView.Clear();
            CellPanel cellPanel = MultiView.DictPanel[index];
            Grid.SetColumn(cellPanel, 0);
            Grid.SetRow(cellPanel, 0);
            MultiView.OriginalGrid.Children.Add(cellPanel);
        }

        /// <summary>
        /// 清空网格
        /// </summary>
        private static void Clear()
        {
            if (MultiView.OriginalGrid != null)
            {
                MultiView.OriginalGrid.RowDefinitions.Clear();
                MultiView.OriginalGrid.ColumnDefinitions.Clear();
                MultiView.OriginalGrid.Children.Clear();
            }

            MultiView.TempGrid1.Children.Clear();
            MultiView.TempGrid1.RowDefinitions.Clear();
            MultiView.TempGrid1.ColumnDefinitions.Clear();
            MultiView.TempGrid2.Children.Clear();
            MultiView.TempGrid2.RowDefinitions.Clear();
            MultiView.TempGrid2.ColumnDefinitions.Clear();

        }

        /// <summary>
        /// 行列创建
        /// </summary>
        /// <param name="grid">控件</param>
        /// <param name="row">行</param>
        /// <param name="column">列</param>
        private static void CreateColumnAndRow(Grid grid, int row, int column)
        {
            if (grid == null) return;

            for (int index = 0; index < row; ++index)
            {
                grid.RowDefinitions.Add(new RowDefinition());
            }

            for (int index = 0; index < column; ++index)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }
        }

        /// <summary>
        /// 被选中项
        /// </summary>
        /// <param name="item"></param>
        public static void SetSelectedItem(int item)
        {
            //通过绑定值的变化，更换画面显示
            OriginalGrid.Tag = item;
        }
    }
}
