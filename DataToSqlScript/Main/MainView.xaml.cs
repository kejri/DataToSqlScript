using DataToSqlScript.Main.Interfaces;
using Rk.Client.Wizards.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DataToSqlScript.Main
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window, IMainView
    {
        public MainView()
        {
            InitializeComponent();
            Loaded += MainView_Loaded;
            Closing += MainView_Closing;
            Closed += MainView_Closed;
        }

        private void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainView_Loaded;
        }

        #region IMainView Members

        private IMainPM m_Model;
        public IMainPM Model
        {
            get
            {
                return m_Model;
            }
            set
            {
                m_Model = value;
                DataContext = m_Model;
            }
        }

        public TabItem TiTable { get => tiTables; }
        public TabItem TiFields { get => tiFields; }
        public TabItem TiData { get => tiData; }
        //public TabItem TiOptions { get => tiOptions; }
        public TabItem TiScript { get => tiScript; }
        public DataGrid DataGrid { get => dataGrid;}
        public DataGrid DgData { get => dgData; }

        #endregion

        #region IWizardPMBase

        IWizardPMBase IWizardViewBase.Model
        {
            get
            {
                return Model;
            }
            set
            {
                Model = value as IMainPM;
            }
        }

        public TabControl TcMain
        {
            get
            {
                return tcMain;
            }
        }

        #endregion

        #region IWindow Members

        public void ShowDlg()
        {
            ShowDialog();
        }

        public event EventHandler<CancelEventArgs> WindowClosing;

        protected virtual void OnWindowClosing(CancelEventArgs e)
        {
            if (WindowClosing != null)
            {
                WindowClosing(this, e);
            }
        }

        public event EventHandler<EventArgs> WindowClosed;

        protected virtual void OnWindowClosed(EventArgs e)
        {
            if (WindowClosed != null)
            {
                WindowClosed(this, e);
            }
        }

        #endregion

        private void MainView_Closing(object sender, CancelEventArgs e)
        {
            OnWindowClosing(e);
        }

        private void MainView_Closed(object? sender, EventArgs e)
        {
            OnWindowClosed(e);
        }
    }
}
