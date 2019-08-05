using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDeployer.App
{
    public static class ExtensionMethods
    {
        public static string ShortenRight(this String str, int maxLength)
        {
            return str.Shorten(maxLength, false);
        }

        public static string ShortenLeft(this String str, int maxLength)
        {
            return str.Shorten(maxLength, true);
        }

        private static string Shorten(this String str, int maxLength, bool left)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;
            str = str.Replace("\n", "");
            str = str.Replace("\r", "");
            if (str.Length <= maxLength) return str;
            if (left)
            {
                return "..." + str.Substring(str.Length - maxLength + 2);
                
            } else
            {
                return str.Substring(0, maxLength - 3) + "...";
            }
        }
    }
}
