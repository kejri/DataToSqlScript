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
        TabItem TiTable { get; }
        TabItem TiFields { get; }
        TabItem TiData { get; }

        DataGrid DataGrid { get; }
    }
}
