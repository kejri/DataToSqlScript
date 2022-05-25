using Rk.Client.Wizards.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DataToSqlScript.Main.Interfaces
{
    public interface IMainView : IWizardViewBase
    {
        new IMainPM Model { get; set; }
        TabControl TcMain { get; }
        TabItem TiTable { get; }
        TabItem TiFields { get; }
        TabItem TiData { get; }
        //TabItem TiOptions { get; }
        TabItem TiScript { get; }

        DataGrid DgFields { get; }

        DataGrid DgData { get; }

        //TextBox tbTop { get; }
        //TextBox tbWhere { get; }
        //TextBox tbOrder { get; }
    }
}
