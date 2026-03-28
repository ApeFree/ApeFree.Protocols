using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// 常用结构体转换器 (Point, Size, Color 等)
    /// <para>1. 本类示范了如何将原本在 JSON 中会展开为多个键值对的“属性对象”压缩成极小的定长二级制序列。</para>
    /// <para>2. 例如 <c>Point</c> 本来占用 JSON 的 {"X":1,"Y":2} 十几个字符。在本转换器处理下，它仅占用 8 个字节的二进制空间。</para>
    /// <para>3. 它利用 <see cref="BitConverter"/> 进行精准的字节切分和组装。</para>
    /// </summary>
    public class JbinGenericStructConverter : JbinSerializer<object>, IJbinFieldConverter
    {
        public readonly static Type[] SupportedTypes = { typeof(Point), typeof(PointF), typeof(Size), typeof(SizeF), typeof(Color) };

        public bool CanDeserialize(Type defineType, Type realType)
        {
            return SupportedTypes.Contains(realType);
        }

        public override bool CanSerialize(Type objectType)
        {
            return SupportedTypes.Contains(objectType);
        }

        public object ConvertBytesToValue(byte[] bytes, Type defineType, Type realType)
        {
            if (realType == typeof(Point))
            {
                var x = BitConverter.ToInt32(bytes, 0);
                var y = BitConverter.ToInt32(bytes, 4);
                return new Point(x, y);
            }

            if (realType == typeof(PointF))
            {
                var x = BitConverter.ToSingle(bytes, 0);
                var y = BitConverter.ToSingle(bytes, 4);
                return new PointF(x, y);
            }

            if (realType == typeof(Size))
            {
                var w = BitConverter.ToInt32(bytes, 0);
                var h = BitConverter.ToInt32(bytes, 4);
                return new Size(w, h);
            }

            if (realType == typeof(SizeF))
            {
                var w = BitConverter.ToSingle(bytes, 0);
                var h = BitConverter.ToSingle(bytes, 4);
                return new SizeF(w, h);
            }

            if (realType == typeof(Color))
            {
                var arbg = BitConverter.ToInt32(bytes, 0);
                var color = Color.FromArgb(arbg);
                return color;
            }

            throw new NotSupportedException($"未实现类型[{realType.FullName}]的序列化实现。");
        }



        public override byte[] ConvertValueToBytes(Type type, object value)
        {
            // 预定义的序列化处理映射
            if (_serializers.TryGetValue(type, out var serializer))
            {
                return serializer(value);
            }

            throw new NotSupportedException($"未实现类型[{type.FullName}]的序列化实现。");
        }

        private static readonly Dictionary<Type, Func<object, byte[]>> _serializers = new()
        {
            [typeof(Point)] = obj =>
            {
                var p = (Point)obj;
                byte[] bytes = new byte[8];
                Buffer.BlockCopy(BitConverter.GetBytes(p.X), 0, bytes, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(p.Y), 0, bytes, 4, 4);
                return bytes;
            },

            [typeof(PointF)] = obj =>
            {
                var p = (PointF)obj;
                byte[] bytes = new byte[8];
                Buffer.BlockCopy(BitConverter.GetBytes(p.X), 0, bytes, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(p.Y), 0, bytes, 4, 4);
                return bytes;
            },

            [typeof(Size)] = obj =>
            {
                var s = (Size)obj;
                byte[] bytes = new byte[8];
                Buffer.BlockCopy(BitConverter.GetBytes(s.Width), 0, bytes, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(s.Height), 0, bytes, 4, 4);
                return bytes;
            },

            [typeof(SizeF)] = obj =>
            {
                var s = (SizeF)obj;
                byte[] bytes = new byte[8];
                Buffer.BlockCopy(BitConverter.GetBytes(s.Width), 0, bytes, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(s.Height), 0, bytes, 4, 4);
                return bytes;
            },

            [typeof(Color)] = obj =>
                BitConverter.GetBytes(((Color)obj).ToArgb())
        };
    }
}
