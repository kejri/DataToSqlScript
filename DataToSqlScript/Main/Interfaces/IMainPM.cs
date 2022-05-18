using DataToSqlScript.Helpers;
using Prism.Commands;
using Rk.Client.Wizards.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataToSqlScript.Main.Interfaces
{
    public interface IMainPM : IWizardPMBase
    {
        new IMainView View { get; }
        List<string> TableNameList { get; set; }
        string TableName { get; set; }
        string TableNames { get; set; }
        IList<DbField> DbFields { get; set; }
        ScriptType[] ScriptTypes { get; set; }
        ScriptType ScriptType { get; set; }
        DelegateCommand<object> InvertCommand { get; set; }
        DataTable Data { get; set; }
        string Script { get; set; }
    }
}
