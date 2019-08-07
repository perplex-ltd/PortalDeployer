using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PortalDeployer.App
{
    public static class CheckSum
    {
        /// <summary>
        /// Returns a hash for an empty file/string.
        /// </summary>
        public static readonly string Empty = CalculateHash(new byte[0]);

        public static string CalculateHash(string content)
        {
            return string.IsNullOrEmpty(content) ? Empty : CalculateHash(Encoding.UTF8.GetBytes(content));
        }
        public static string CalculateHash(byte[] content)
        {
            using (HashAlgorithm hasher = new SHA1CryptoServiceProvider())
            {
                byte[] hash = hasher.ComputeHash(content);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        internal static string CalculateHashFromFile(string path)
        {
            if (!File.Exists(path))
                return Empty;
            else
                return CalculateHash(File.ReadAllBytes(path));
        }
    }
}
