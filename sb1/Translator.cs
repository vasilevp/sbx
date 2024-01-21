using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sbx
{
    internal static class Translator
    {
        internal static string Translate(string key)
        {
            return App.Current?.FindResource(key) as string ?? "NULL";
        }

        internal static string Translate(string key, params object?[] args)
        {
            return string.Format(Translate(key), args);
        }
    }
}
