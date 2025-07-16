using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    internal static class aa
    {
        static StringBuilder sb = new StringBuilder();
        internal static void clear_sb() { sb.Clear(); }
        internal static string get_sb() { return sb.ToString(); }
        internal static void append(string arg)
        {
            //タブ１つにつきスペース４つ
            sb.AppendLine(" ".PadLeft(8) + arg);
        }

        internal static Dictionary<string, bool> dicClasses = new Dictionary<string, bool>();

        internal static Dictionary<string, bool> needClasses = new Dictionary<string, bool>();

    }
}
