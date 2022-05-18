using DataToSqlScript.Helpers;
using DataToSqlScript.Main.Interfaces;
using Prism.Commands;
using Rk.Client.Wizards;
using Rk.Client.Wizards.Interfaces;
using Rk.Common;
using Rk.Common.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DataToSqlScript.Main
{
    public class MainPM : WizardPMBase, IMainPM, INotifyPropertyChanged
    {
        private string m_ConnString;
        private string m_DbSchema;
        public MainPM(IMainView view)
            : base(null, view)
        {
            m_ConnString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            m_DbSchema = CommonProc.ReadAppSettings("DbSchema", "dbo");

            InitCommands();
            InitEnums();
            InitLists();

            PropertyChanged += MainPM_PropertyChanged;
        }

        private void MainPM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "ScriptType":
                    {
                        setFieldsDefaults(ScriptType);
                        break;
                    }
            }
        }

        #region IMainPM Members

        public new IMainView View => base.View as IMainView;

        private List<string> m_TableNameList;
        public List<string> TableNameList
        {
            get
            {
                return m_TableNameList;
            }
            set
            {
                m_TableNameList = value;
                NotifyPropertyChanged();
            }
        }

        private string m_TableName;
        public string TableName
        {
            get
            {
                return m_TableName;
            }
            set
            {
                m_TableName = value;
                NotifyPropertyChanged();
            }
        }

        private string m_TableNames;
        public string TableNames
        {
            get
            {
                return m_TableNames;
            }
            set
            {
                m_TableNames = value;
                NotifyPropertyChanged();
            }
        }

        private IList<DbField> m_DbFields;
        public IList<DbField> DbFields { get => m_DbFields; set { m_DbFields = value; NotifyPropertyChanged(); } }

        private ScriptType[] m_ScriptTypes;
        public ScriptType[] ScriptTypes { get => m_ScriptTypes; set { m_ScriptTypes = value; NotifyPropertyChanged(); } }

        private ScriptType m_ScriptType;
        public ScriptType ScriptType { get => m_ScriptType; set { m_ScriptType = value; NotifyPropertyChanged(); } }

        private DelegateCommand<object> m_InvertCommand;
        public DelegateCommand<object> InvertCommand { get => m_InvertCommand; set { m_InvertCommand = value; NotifyPropertyChanged(); } }

        private DataTable m_Data;
        public DataTable Data { get => m_Data; set { m_Data = value; NotifyPropertyChanged(); } }


        private string m_Script;
        public string Script { get => m_Script; set { m_Script = value; NotifyPropertyChanged(); } }

        #endregion

        #region IWizardViewBase Members

        IWizardViewBase IWizardPMBase.View => base.View;

        #endregion

        private bool m_IsBusy;
        public bool IsBusy
        {
            get
            {
                return m_IsBusy;
            }
            set
            {
                m_IsBusy = value;
                NotifyPropertyChanged();
            }
        }

        protected override void InitCommands()
        {
            base.InitCommands();
            InvertCommand = new DelegateCommand<object>(Invert, CanInvert);
        }

        void InitEnums()
        {
            ScriptTypes = EnumExtensions.GetValues<ScriptType>();
            ScriptType = ScriptType.Insert;
        }

        void InitLists()
        {
            (View as Window).Cursor = Cursors.Wait;
            try
            {
                using (var conn = new SqlConnection(m_ConnString))
                {
                    var command = new SqlCommand(String.Format(@"
select table_name TableName from INFORMATION_SCHEMA.TABLES ist
where
  (ist.TABLE_SCHEMA='{0}') and
  (left(ist.TABLE_NAME,1) <> '_')
order by table_name", m_DbSchema
          ), conn);
                    var table = new DataTable("Tables");
                    var da = new SqlDataAdapter(command);
                    da.Fill(table);
                    var list = new List<string>();
                    foreach (DataRow item in table.Rows)
                    {
                        list.Add(item["TableName"].ToString());
                    }
                    TableNameList = list;
                    //if (TableNameList.Any())
                    //  TableName = TableNameList[0];
                }
            }
            finally
            {
                (View as Window).Cursor = Cursors.Arrow;
            }
        }

        protected override void TcMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            base.TcMain_SelectionChanged(sender, e);

            TabItem tabItemOld = null;
            TabItem tabItemNew = null;
            if (e.RemovedItems.Count > 0)
                tabItemOld = e.RemovedItems[0] as TabItem;
            if (e.AddedItems.Count > 0)
                tabItemNew = e.AddedItems[0] as TabItem;

            if (tabItemNew == View.TiTable)
            {

            }
            else if (tabItemNew == View.TiFields && tabItemOld == View.TiTable)
            {
                var dbFields = loadDbFields();
                var pkFields = getPrimaryKey();
                int index = 1;
                foreach (var pk in pkFields)
                {
                    var field = dbFields.FirstOrDefault(i => i.Name == pk);
                    if (field != null)
                    {
                        field.PK = index;
                        index++;
                    }
                }
                DbFields = dbFields;
                setFieldsDefaults(ScriptType);
            }
            else if (tabItemNew == View.TiData && tabItemOld == View.TiFields)
            {
                Data = loadData();
            }
            //else if (tabItemNew == View.TiOptions && tabItemOld == View.TiData)
            //{
            //}
            else if (tabItemNew == View.TiScript && tabItemOld == View.TiData)
            {
                CreateScript();
            }
        }

        List<DbField> loadDbFields()
        {
            using (SqlConnection conn = new SqlConnection(m_ConnString))
            {
                SqlCommand command = new SqlCommand(String.Format(@"
select a.Column_Name Field_Name, a.Data_Type Field_Type, a.Character_Maximum_Length String_Length, a.Numeric_Precision, a.Numeric_Scale, a.Is_Nullable IsNotNull, a.table_schema, prop.Value Description, COALESCE(co.is_computed,0) IsComputed
from INFORMATION_SCHEMA.COLUMNS a
inner join sys.objects so on (so.name=a.table_name)
left outer join sys.columns co on (co.object_id=so.object_id) and (co.column_id=a.ordinal_position)
LEFT JOIN sys.extended_properties prop ON prop.major_id = co.object_id AND prop.minor_id = co.column_id and prop.NAME = 'MS_Description'
where
  (a.Table_Name='{0}')
order by a.ordinal_position
", TableName), conn);
                DataTable dataTable = new DataTable("Fields");
                SqlDataAdapter da = new SqlDataAdapter(command);
                da.Fill(dataTable);

                var result = new List<DbField>();
                foreach (DataRow row in dataTable.Rows)
                {
                    var field = new DbField();
                    result.Add(field);
                    field.Name = row["Field_Name"].ToString();
                    field.Description = row["Description"] != DBNull.Value ? row["Description"].ToString() : null;
                    field.IsRequired = row["IsNotNull"].ToString() == "NO" ? true : false;
                    field.IsComputed = Convert.ToBoolean(row["IsComputed"]);
                    string dbType;
                    int? dbSize;
                    int? dbScale;
                    getDbFieldFromSql(row, out dbType, out dbSize, out dbScale);
                    field.DbType = dbType;
                    field.DbSize = dbSize;
                    field.DbScale = dbScale;
                }
                return result;
            }
        }

        private void getDbFieldFromSql(DataRow row, out string dbType, out int? dbSize, out int? dbScale)
        {
            dbSize = null;
            dbScale = null;
            switch (row["Field_Type"].ToString())
            {
                case "uniqueidentifier":
                    {
                        dbType = "DbGuid";
                        break;
                    }
                case "char":
                    {
                        dbType = "DbChar";
                        dbSize = Convert.ToInt32(row["String_Length"]);
                        break;
                    }
                case "nchar":
                    {
                        dbType = "DbNChar";
                        dbSize = Convert.ToInt32(row["String_Length"]);
                        break;
                    }
                case "varchar":
                    {
                        dbType = "DbVarchar";
                        dbSize = Convert.ToInt32(row["String_Length"]);
                        break;
                    }
                case "nvarchar":
                    {
                        dbType = "DbNVarchar";
                        dbSize = Convert.ToInt32(row["String_Length"]);
                        break;
                    }
                case "bit":
                    {
                        dbType = "DbBool";
                        break;
                    }
                case "tinyint":
                    {
                        dbType = "DbByte";
                        break;
                    }
                case "smallint":
                    {
                        dbType = "DbSmallint";
                        break;
                    }
                case "int":
                    {
                        dbType = "DbInteger";
                        break;
                    }
                case "bigint":
                    {
                        dbType = "DbBigint";
                        break;
                    }
                case "float":
                    {
                        dbType = "DbFloat";
                        break;
                    }
                case "real":
                    {
                        dbType = "DbDouble";
                        break;
                    }
                case "numeric":
                    {
                        dbType = "DbNumeric";
                        dbSize = Convert.ToInt32(row["Numeric_Precision"]);
                        dbScale = Convert.ToInt32(row["Numeric_Scale"]);
                        break;
                    }
                case "decimal":
                    {
                        dbType = "DbDecimal";
                        dbSize = Convert.ToInt32(row["Numeric_Precision"]);
                        dbScale = Convert.ToInt32(row["Numeric_Scale"]);
                        break;
                    }
                case "money":
                case "smallmoney":
                    {
                        dbType = "DbMoney";
                        dbSize = Convert.ToInt32(row["Numeric_Precision"]);
                        dbScale = Convert.ToInt32(row["Numeric_Scale"]);
                        break;
                    }
                case "date":
                    {
                        dbType = "DbDate";
                        break;
                    }
                case "datetime":
                case "smalldatetime":
                case "datetime2":
                    {
                        dbType = "DbDatetime";
                        break;
                    }
                case "timestamp":
                    {
                        dbType = "DbTimestamp";
                        break;
                    }
                case "text":
                    {
                        dbType = "DbText";
                        break;
                    }
                case "ntext":
                    {
                        dbType = "DbNText";
                        break;
                    }
                case "binary":
                    {
                        dbType = "DbBinary";
                        dbSize = Convert.ToInt32(row["String_Length"]);
                        break;
                    }
                case "varbinary":
                    {
                        dbType = "DbVarbinary";
                        dbSize = Convert.ToInt32(row["String_Length"]);
                        break;
                    }
                case "image":
                    {
                        dbType = "DbImage";
                        break;
                    }
                case "xml":
                    {
                        dbType = "DbXml";
                        break;
                    }
                default:
                    {
                        throw new ApplicationException(String.Format("Nepodporovaný typ pole {0}.", row["Field_Name"]));
                    }
            }
        }

        List<string> getPrimaryKey()
        {
            using (SqlConnection conn = new SqlConnection(m_ConnString))
            {
                SqlCommand command = new SqlCommand(String.Format(@"
SELECT column_name as Column_Name
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
    ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' 
    AND TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME 
    AND KU.table_name='{0}'
ORDER BY 
     KU.TABLE_NAME
    ,KU.ORDINAL_POSITION", TableName), conn);
                DataTable dataTable = new DataTable("PK");
                SqlDataAdapter da = new SqlDataAdapter(command);
                da.Fill(dataTable);

                var result = new List<string>();
                foreach (DataRow row in dataTable.Rows)
                {
                    result.Add(row["Column_Name"].ToString());
                }
                return result;
            }
        }

        void setFieldsDefaults(ScriptType scriptType)
        {
            if (DbFields != null)
            {
                foreach (var item in DbFields)
                {
                    if (item.PK.HasValue)
                    {
                        item.IsSelect = true;
                        item.IsWhere = true;
                        if (scriptType == ScriptType.Update)
                        {
                            item.IsSelect = false;
                        }
                    }
                    else
                    {
                        item.IsSelect = true;
                    }
                }
            }
        }


        DataTable loadData()
        {
            using (SqlConnection conn = new SqlConnection(m_ConnString))
            {
                SqlCommand command = new SqlCommand(String.Format(@"
select {0}
from {1}
where {2}
", getRowFields(), TableName, getWhere(), getOrder()), conn);
                DataTable dataTable = new DataTable("Data");
                SqlDataAdapter da = new SqlDataAdapter(command);
                da.Fill(dataTable);
                return dataTable;
            }
        }

        private string getRowFields()
        {
            string result = string.Empty;
            foreach (var item in DbFields)
            {
                string odd = string.Empty;
                if (!string.IsNullOrEmpty(result))
                {
                    odd = ", ";
                }
                if (item.IsSelect || (ScriptType == ScriptType.Update && item.IsWhere))
                {
                    result += odd + item.Name;
                }
            }
            return result;
        }

        private object? getWhere()
        {
            return "0=0";
        }

        private object? getOrder()
        {
            return "Id";
        }

        private void CreateScript()
        {
            var sql = string.Empty;
            if ((View.DgData.SelectedItems != null) && (View.DgData.SelectedItems.Count > 0))
            {
                foreach (DataRowView item in View.DgData.SelectedItems)
                {
                    sql += (CreateScriptOneRow(item.Row)) + Environment.NewLine;
                }
            }
            else
            {
                foreach (DataRowView item in View.DgData.Items)
                {
                    sql += (CreateScriptOneRow(item.Row)) + Environment.NewLine;
                }
            }

            Script = sql;

            //using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(sql)))
            //{

            //}
        }

        private string CreateScriptOneRow(DataRow row)
        {
            if (ScriptType == ScriptType.Insert)
            {
                return CreateScriptInsert(row);
            }
            else if (ScriptType == ScriptType.Update)
            {
                return CreateScriptUpdate(row);
            }
            return null;
        }

        private string CreateScriptInsert(DataRow row)
        {
            string sqlFields = string.Empty;
            string sqlValues = string.Empty;
            foreach (DataColumn item in row.Table.Columns)
            {
                var value = row[item.ColumnName];
                var field = DbFields.FirstOrDefault(i => i.Name == item.ColumnName);
                if (field != null && field.IsSelect)
                {
                    string odd = string.Empty;
                    if (!string.IsNullOrEmpty(sqlFields))
                    {
                        odd = ", ";
                    }
                    sqlFields += odd + $"{item.ColumnName}";
                    sqlValues += odd + $"{valueToString(value, field)}";
                }
            }
            string sql = $"insert into {TableName} ({sqlFields}) values ({sqlValues});";
            return sql;
        }

        private string CreateScriptUpdate(DataRow row)
        {
            string sqlFields = string.Empty;
            foreach (DataColumn item in row.Table.Columns)
            {
                var value = row[item.ColumnName];
                var field = DbFields.FirstOrDefault(i => i.Name == item.ColumnName);
                if (field != null && field.IsSelect)
                {
                    string odd = string.Empty;
                    if (!string.IsNullOrEmpty(sqlFields))
                    {
                        odd = ", ";
                    }
                    sqlFields += odd + $"{item.ColumnName}={valueToString(value, field)}";
                }
            }
            string sql = $"update {TableName} set {sqlFields} where {getUpdatePK(row)};";
            return sql;
        }

        private string getUpdatePK(DataRow row)
        {
            string result = string.Empty;
            foreach (var item in DbFields.OrderBy(i => i.PK))
            {
                if (item.IsWhere)
                {
                    var value = row[item.Name];
                    string odd = string.Empty;
                    if (!string.IsNullOrEmpty(result))
                    {
                        odd = ", ";
                    }
                    result += odd + $"{item.Name}={valueToString(value, item)}";
                }
            }
            return result;
        }

        private string valueToString(object value, DbField item)
        {
            if (value == null || value is DBNull)
            {
                return "null";
            }

            if (value is string || value is Guid)
            {
                return $"'{value?.ToString().Replace("'", "''")}'";
            }
            else if (value is DateTime)
            {
                string format = "yyyy-MM-dd HH:mm:ss";
                if (item.DbType == "DbDate")
                {
                    format = "yyyy-MM-dd";
                }
                return $"'{((DateTime)value).ToString(format)}'";
            }
            else if (value is bool)
            {
                return $"{Convert.ToInt32(value)}";
            }

            if (item.DbType == "DbDecimal")
            {
                return $"{value?.ToString().Replace(",", ".")}";
            }
            return $"{value?.ToString()}";
        }


        #region Commands

        protected virtual bool CanInvert(object obj)
        {
            return true;
        }

        protected virtual void Invert(object obj)
        {
            if ((View.DataGrid.SelectedItems != null) && (View.DataGrid.SelectedItems.Count > 0))
            {
                foreach (DbField item in View.DataGrid.SelectedItems)
                {
                    item.IsSelect = !item.IsSelect;
                }
            }
            else
            {
                foreach (var item in DbFields)
                {
                    item.IsSelect = !item.IsSelect;
                }
            }
        }

        #endregion
    }
}
