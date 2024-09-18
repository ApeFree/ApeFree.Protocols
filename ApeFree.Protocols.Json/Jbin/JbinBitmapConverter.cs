using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ApeFree.Protocols.Json.Jbin
{

    public class JbinBitmapConverter : JbinConverter<Bitmap>
    {
        protected override Bitmap ConvertBytesToValue(byte[] bytes, Type objectType)
        {
            //var w = BitConverter.ToInt32(bytes, 0);
            //var h = BitConverter.ToInt32(bytes, 4);
            //var f = (PixelFormat)BitConverter.ToInt32(bytes, 8);

            //Bitmap bitmap = new Bitmap(w, h, f);

            //// 锁定图像的像素数据
            //Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            //BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            //try
            //{
            //    // 获取指向像素数据的指针
            //    IntPtr ptr = bitmapData.Scan0;

            //    // 从指针处复制数据到我们的字节数组
            //    Marshal.Copy(bytes, 0, ptr, bytes.Length);
            //}
            //finally
            //{
            //    // 释放像素数据的锁定
            //    bitmap.UnlockBits(bitmapData);
            //}

            //return bitmap;

            using (MemoryStream ms = new MemoryStream(bytes))
            {
                return (Bitmap)Image.FromStream(ms);
            }
        }

        protected override byte[] ConvertValueToBytes(Bitmap bitmap)
        {
            //List<byte[]> lst = new List<byte[]>();

            //lst.Add(BitConverter.GetBytes(bitmap.Width));
            //lst.Add(BitConverter.GetBytes(bitmap.Height));
            //lst.Add(BitConverter.GetBytes((int)bitmap.PixelFormat));
            //lst.Add(GetBitmapDataBytes(bitmap));

            //var bytes = JbinObject.ConcatByteArrays(lst);
            //return bytes;

            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png); // 可以根据需要选择图像格式，如 Jpeg、Bmp 等
                return ms.ToArray();
            }
        }

        public byte[] GetBitmapDataBytes(Bitmap bitmap)
        {
            // 计算所需的字节数
            int stride = bitmap.Width * Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            int bufferSize = stride * bitmap.Height;

            // 创建一个字节数组来保存图像数据
            byte[] buffer = new byte[bufferSize];

            // 锁定图像的像素数据
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);

            try
            {
                // 获取指向像素数据的指针
                IntPtr ptr = bitmapData.Scan0;

                // 从指针处复制数据到我们的字节数组
                Marshal.Copy(ptr, buffer, 0, bufferSize);
            }
            finally
            {
                // 释放像素数据的锁定
                bitmap.UnlockBits(bitmapData);
            }

            return buffer;
        }
    }
}
