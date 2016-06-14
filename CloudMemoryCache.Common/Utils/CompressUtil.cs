﻿using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using CloudMemoryCache.Common.TransientFaultHandling;

namespace CloudMemoryCache.Common.Utils
{
    // http://www.codeproject.com/Articles/27203/GZipStream-Compress-Decompress-a-string
    public static class CompressUtil
    {
        public static string Compress(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            using (var msi = new MemoryStream(bytes))
            {
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(mso, CompressionMode.Compress))
                    {
                        CopyTo(msi, gs);
                    }
                    return Convert.ToBase64String(mso.ToArray());
                }
            }

        }

        public static string Decompress(string value)
        {
            return TransientFaultHandlingUtil.SafeExecute(() =>
            {
                byte[] bytes = Convert.FromBase64String(value);
                using (var msi = new MemoryStream(bytes))
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                    {
                        CopyTo(gs, mso);
                    }
                    return Encoding.UTF8.GetString(mso.ToArray());
                }
            }).Value;
        }
        private static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

    }
}
