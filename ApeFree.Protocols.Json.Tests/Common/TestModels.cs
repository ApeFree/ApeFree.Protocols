using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace ApeFree.Protocols.Json.Tests.Common
{
    public enum SampleStatus
    {
        None = 0,
        Running = 1,
        Completed = 2,
        Failed = 3
    }

    #region 基元类型数组包装模型
    public class PrimitiveArraysModel
    {
        public bool[] BoolArray { get; set; }
        public sbyte[] SbyteArray { get; set; }
        public short[] ShortArray { get; set; }
        public ushort[] UshortArray { get; set; }
        public int[] IntArray { get; set; }
        public uint[] UintArray { get; set; }
        public long[] LongArray { get; set; }
        public ulong[] UlongArray { get; set; }
        public float[] FloatArray { get; set; }
        public double[] DoubleArray { get; set; }
        public decimal[] DecimalArray { get; set; }
        public char[] CharArray { get; set; }
        public SampleStatus[] EnumArray { get; set; }
        public DayOfWeek[] DayOfWeekArray { get; set; }
    }

    public class SinglePrimitiveArrayModel<T>
    {
        public string Title { get; set; }
        public T[] Data { get; set; }
    }
    #endregion

    #region 字节容器模型
    public class BytesContainerModel
    {
        public byte[] SingleBytes { get; set; }
        public byte[][] JaggedBytes { get; set; }
        public List<byte[]> ByteList { get; set; }
    }
    #endregion

    #region 结构体与容器模型
    public class StructContainerModel
    {
        public Point Point { get; set; }
        public PointF PointF { get; set; }
        public Size Size { get; set; }
        public SizeF SizeF { get; set; }
        public Color Color { get; set; }
    }

    public class StructCollectionsModel
    {
        public Point[] PointArray { get; set; }
        public PointF[] PointFArray { get; set; }
        public Size[] SizeArray { get; set; }
        public SizeF[] SizeFArray { get; set; }
        public Color[] ColorArray { get; set; }

        public List<Point> PointList { get; set; }
        public List<PointF> PointFList { get; set; }
        public List<Size> SizeList { get; set; }
        public List<SizeF> SizeFList { get; set; }
        public List<Color> ColorList { get; set; }
        public List<byte[]> ByteArrayList { get; set; }
    }
    #endregion

    #region 字符串字典模型
    public class StringDictModel
    {
        public string Name { get; set; }
        public string[] Tags { get; set; }
        public string[] Categories { get; set; }
    }
    #endregion

    #region 复合综合模型
    public class ComplexJbinModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public TaskStatus Status { get; set; }
        public Point Location { get; set; }
        public SizeF[] Sizes { get; set; }
        public byte[] RawData { get; set; }
        public int[] Waveform { get; set; }
        public string[] Tags { get; set; }
        public List<ComplexJbinModel> Children { get; set; }
    }
    #endregion

    #region 多态模型
    public abstract class Animal
    {
        public string Name { get; set; }
        public abstract string Speak();
    }

    public class Dog : Animal
    {
        public int BarkVolume { get; set; }
        public byte[] BarkVoiceData { get; set; }

        public override string Speak() => "Woof!";
    }

    public class Cat : Animal
    {
        public int MeowPitch { get; set; }
        public Point FavouriteSpot { get; set; }

        public override string Speak() => "Meow!";
    }

    public class ZooModel
    {
        public string ZooName { get; set; }
        public Animal LeadAnimal { get; set; }
        public List<Animal> Animals { get; set; }
    }
    #endregion

    #region 转置测试模型
    public class DefectRecord
    {
        public int Id { get; set; }
        public string DefectType { get; set; }
        public double Area { get; set; }
        public float Score { get; set; }
        public bool IsValid { get; set; }
        public Point Position { get; set; }
    }
    #endregion

    #region RPC 反射测试模型
    public interface ICalculatorService
    {
        int Add(int a, int b);
        double ComputeWaveformMax(double[] waveform);
        byte[] ReverseBytes(byte[] data);
    }

    public class CalculatorService : ICalculatorService
    {
        public int Add(int a, int b) => a + b;

        public double ComputeWaveformMax(double[] waveform)
        {
            if (waveform == null || waveform.Length == 0) return 0;
            double max = waveform[0];
            for (int i = 1; i < waveform.Length; i++)
            {
                if (waveform[i] > max) max = waveform[i];
            }
            return max;
        }

        public byte[] ReverseBytes(byte[] data)
        {
            if (data == null) return null;
            byte[] rev = new byte[data.Length];
            Array.Copy(data, rev, data.Length);
            Array.Reverse(rev);
            return rev;
        }
    }
    #endregion
}
