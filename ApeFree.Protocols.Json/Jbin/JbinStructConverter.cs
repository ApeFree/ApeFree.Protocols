using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ApeFree.Protocols.Json.Jbin
{
    public class JbinStructConverter : JbinConverter<object>
    {
        public readonly static Type[] SupportedTypes = { typeof(Point), typeof(PointF), typeof(Size), typeof(SizeF), typeof(Color) };

        public override bool CanConvert(Type objectType)
        {
            return SupportedTypes.Contains(objectType);
        }

        protected override object ConvertBytesToValue(byte[] bytes, Type objType)
        {
            if (objType == typeof(Point))
            {
                var x = BitConverter.ToInt32(bytes, 0);
                var y = BitConverter.ToInt32(bytes, 4);
                return new Point(x, y);
            }

            if (objType == typeof(PointF))
            {
                var x = BitConverter.ToSingle(bytes, 0);
                var y = BitConverter.ToSingle(bytes, 4);
                return new PointF(x, y);
            }

            if (objType == typeof(Size))
            {
                var w = BitConverter.ToInt32(bytes, 0);
                var h = BitConverter.ToInt32(bytes, 4);
                return new Size(w, h);
            }

            if (objType == typeof(SizeF))
            {
                var w = BitConverter.ToSingle(bytes, 0);
                var h = BitConverter.ToSingle(bytes, 4);
                return new SizeF(w, h);
            }

            if (objType == typeof(Color))
            {
                var arbg = BitConverter.ToInt32(bytes, 0);
                var color = Color.FromArgb(arbg);
                return color;
            }

            throw new NotSupportedException($"未实现类型[{objType.FullName}]的序列化实现。");
        }

        protected override byte[] ConvertValueToBytes(object value)
        {

            if (value is Point p)
            {
                byte[] bytes = new byte[8];
                var x = BitConverter.GetBytes(p.X);
                var y = BitConverter.GetBytes(p.Y);
                Array.Copy(x, 0, bytes, 0, 4);
                Array.Copy(y, 0, bytes, 4, 4);
                return bytes;
            }

            if (value is PointF pf)
            {
                byte[] bytes = new byte[8];
                var x = BitConverter.GetBytes(pf.X);
                var y = BitConverter.GetBytes(pf.Y);
                Array.Copy(x, 0, bytes, 0, 4);
                Array.Copy(y, 0, bytes, 4, 4);
                return bytes;
            }

            if (value is Size s)
            {
                byte[] bytes = new byte[8];
                var w = BitConverter.GetBytes(s.Width);
                var h = BitConverter.GetBytes(s.Height);
                Array.Copy(w, 0, bytes, 0, 4);
                Array.Copy(h, 0, bytes, 4, 4);
                return bytes;
            }

            if (value is SizeF sf)
            {
                byte[] bytes = new byte[8];
                var w = BitConverter.GetBytes(sf.Width);
                var h = BitConverter.GetBytes(sf.Height);
                Array.Copy(w, 0, bytes, 0, 4);
                Array.Copy(h, 0, bytes, 4, 4);
                return bytes;
            }

            if (value is Color color)
            {
                var arbg = BitConverter.GetBytes(color.ToArgb());
                return arbg;
            }

            throw new NotSupportedException($"未实现类型[{value.GetType().FullName}]的序列化实现。");
        }
    }
}
