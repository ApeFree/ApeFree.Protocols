using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ApeFree.Protocol.ApeFtp
{
    public static class Extensions
    {
        /// <summary>
        /// 获取字节数组的MD5值
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] GetMD5(this byte[] bytes)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(bytes);
            }
        }

        /// <summary>
        /// 获取文件的MD5值
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static byte[] GetMD5(this FileInfo fileInfo)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = fileInfo.OpenRead())
                {
                    return md5.ComputeHash(stream);
                }
            }
        }

        /// <summary>
        /// 将字节数组转换为十六进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToHexString(this byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 将字符串转换为字节数组
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] GetBytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        /// <summary>
        /// 将字节数组转换为字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string EncodeToString(this byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// 合并多个字节数组
        /// </summary>
        /// <param name="source"></param>
        /// <param name="arrays"></param>
        /// <returns></returns>
        public static byte[] Merge(this byte[] source, params byte[][] arrays)
        {
            int length = source.Length;
            foreach (var array in arrays)
            {
                length += array.Length;
            }

            byte[] result = new byte[length];
            int offset = 0;
            source.CopyTo(result, offset);
            offset += source.Length;

            foreach (var array in arrays)
            {
                array.CopyTo(result, offset);
                offset += array.Length;
            }

            return result;
        }

        /// <summary>
        /// 合并多个字节数组
        /// </summary>
        /// <param name="source"></param>
        /// <param name="arrays"></param>
        /// <returns></returns>
        public static byte[] Merge(this byte[] source, params System.Collections.Generic.IEnumerable<byte>[] arrays)
        {
            return source.Merge(arrays.Select(a => a.ToArray()).ToArray());
        }
    }
}