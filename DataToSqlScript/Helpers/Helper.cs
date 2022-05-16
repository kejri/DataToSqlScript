using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataToSqlScript.Helpers
{
  public class Helper
  {
    public static System.IO.DirectoryInfo GetExecutingDirectory()
    {
      var location = new Uri(System.Reflection.Assembly.GetEntryAssembly().GetName().CodeBase);
      return new System.IO.FileInfo(location.AbsolutePath).Directory;
    }
  }
}
