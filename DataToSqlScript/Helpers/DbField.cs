using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataToSqlScript.Helpers
{
    public class DbField : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string DbType { get; set; }
        public int? DbSize { get; set; }
        public int? DbScale { get; set; }
        public bool IsRequired { get; set; }
        public bool IsComputed { get; set; }
        public int? PK { get; set; }

        private bool m_IsSelect;
        public bool IsSelect { get => m_IsSelect; set { m_IsSelect = value; NotifyPropertyChanged(); } }

        private bool m_IsWhere;
        public bool IsWhere { get => m_IsWhere; set { m_IsWhere = value; NotifyPropertyChanged(); if (m_IsWhere) { IsSelect = false; } } }
        public string NameExt
        {
            get
            {
                return String.IsNullOrEmpty(Description) ? Name : Description;
            }
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

    }
}
