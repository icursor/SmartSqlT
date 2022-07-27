using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using HandyControl.Data;
using SmartSQL.Framework;
using SmartSQL.Framework.Exporter;
using SmartSQL.Framework.PhysicalDataModel;
using SmartSQL.Framework.SqliteModel;
using SmartSQL.Framework.Util;
using SmartSQL.Annotations;
using SmartSQL.DocUtils;
using SmartSQL.Helper;
using SmartSQL.UserControl;
using SmartSQL.UserControl.Connect;
using SqlSugar;
using DbType = SqlSugar.DbType;

namespace SmartSQL.Views
{
    //定义委托
    public delegate void ConnectChangeRefreshHandlerExt(ConnectConfigs connectConfig);
    /// <summary>
    /// GroupManage.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectManage : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event ConnectChangeRefreshHandlerExt ChangeRefreshEvent;

        #region DependencyProperty

        public static readonly DependencyProperty DataListProperty = DependencyProperty.Register(
            "DataList", typeof(List<ConnectConfigs>), typeof(ConnectManage), new PropertyMetadata(default(List<ConnectConfigs>)));
        public List<ConnectConfigs> DataList
        {
            get => (List<ConnectConfigs>)GetValue(DataListProperty);
            set
            {
                SetValue(DataListProperty, value);
                OnPropertyChanged(nameof(DataList));
            }
        }

        public static readonly DependencyProperty MainContentProperty = DependencyProperty.Register(
            "MainContent", typeof(System.Windows.Controls.UserControl), typeof(ConnectManage), new PropertyMetadata(default(System.Windows.Controls.UserControl)));
        /// <summary>
        /// 主界面用户控件
        /// </summary>
        public System.Windows.Controls.UserControl MainContent
        {
            get => (System.Windows.Controls.UserControl)GetValue(MainContentProperty);
            set => SetValue(MainContentProperty, value);
        }
        #endregion

        public ConnectManage()
        {
            InitializeComponent();
            DataContext = this;
            MainContent = new ConnectMainUC();
        }

        /// <summary>
        /// 加载连接信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            if (listBox.SelectedItems.Count > 0)
            {
                OprToolGrid.Visibility = Visibility.Visible;
                var connect = (ConnectConfigs)listBox.SelectedItems[0];
                switch (connect.DbType)
                {
                    case DbType.SqlServer:
                        var ucSqlServer = new SqlServerUC();
                        ucSqlServer.ConnectConfig = connect;
                        ucSqlServer.ChangeRefreshEvent += ChangeRefreshEvent;
                        MainContent = ucSqlServer;
                        break;
                    case DbType.MySql:
                        var ucMySql = new MySqlUC();
                        ucMySql.ConnectConfig = connect;
                        MainContent = ucMySql;
                        break;
                    case DbType.PostgreSQL:
                        var ucPostgreSql = new PostgreSqlUC();
                        ucPostgreSql.ConnectConfig = connect;
                        MainContent = ucPostgreSql;
                        break;
                }
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSave_OnClick(object sender, RoutedEventArgs e)
        {
            #region MyRegion
            var tag = ((Button)sender).Tag;
            var isConnect = tag != null && (string)tag == $"Connect";
            //SqlServer
            if (MainContent is SqlServerUC ucSqlServer)
            {
                ucSqlServer.SaveForm(isConnect);
            }
            //MySql
            if (MainContent is MySqlUC ucMySql)
            {
                ucMySql.SaveForm(isConnect);
            }
            //PostgreSql
            if (MainContent is PostgreSqlUC ucPostgreSql)
            {
                ucPostgreSql.SaveForm(isConnect);
            }
            #endregion
        }

        /// <summary>
        /// 取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDelete_OnClick(object sender, RoutedEventArgs e)
        {
            #region MyRegion
            var sqLiteHelper = new SQLiteHelper();
            if (ListConnects.SelectedItem == null)
            {
                Growl.WarningGlobal(new GrowlInfo { Message = $"请选择需要删除的连接", WaitTime = 1, ShowDateTime = false });
                return;
            }
            var selectedConnect = (ConnectConfigs)ListConnects.SelectedItem;
            Task.Run(() =>
            {
                sqLiteHelper.db.Delete<ConnectConfigs>(selectedConnect.ID);
                var datalist = sqLiteHelper.db.Table<ConnectConfigs>().
                   ToList();
                Dispatcher.Invoke(() =>
                {
                    ResetData();
                    DataList = datalist;
                    if (ChangeRefreshEvent != null)
                    {
                        //ChangeRefreshEvent();
                    }
                });
            });
            #endregion
        }

        /// <summary>
        /// 添加/重置表单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnReset_OnClick(object sender, RoutedEventArgs e)
        {
            ResetData();
        }

        private void ResetData()
        {
            MainContent = new ConnectMainUC();
            ListConnects.SelectedItem = null;
        }

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnTestConnect_OnClick(object sender, RoutedEventArgs e)
        {
            #region MyRegion
            //测试SqlServer
            if (MainContent is SqlServerUC ucSqlServer)
            {
                ucSqlServer.TestConnect(true);
            }
            //测试MySql
            if (MainContent is MySqlUC ucMySql)
            {
                ucMySql.TestConnect(true);
            }
            //测试PostgreSql
            if (MainContent is PostgreSqlUC ucPostgreSql)
            {
                ucPostgreSql.TestConnect(true);
            }
            #endregion
        }

        private void ConnectManage_OnLoaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var sqLiteHelper = new SQLiteHelper();
                var datalist = sqLiteHelper.db.Table<ConnectConfigs>().ToList();
                Dispatcher.Invoke(() =>
                {
                    DataList = datalist;
                });
            });
        }
    }
}
