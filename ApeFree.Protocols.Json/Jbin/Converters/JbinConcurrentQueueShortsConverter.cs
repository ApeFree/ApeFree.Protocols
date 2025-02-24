using System;
using System.Collections.Concurrent;
using System.Linq;

namespace ApeFree.Protocols.Json.Jbin
{
    //public class JbinConcurrentQueueShortsConverter : JbinSerializer<ConcurrentQueue<short[]>>
    //{
    //    protected override ConcurrentQueue<short[]> ConvertBytesToValue(byte[] bytes, Type defineType, Type realType)
    //    {
    //        var row = BitConverter.ToInt32(bytes, 0);
    //        var col = (bytes.Length - 4) / 2 / row;

    //        var queue = new ConcurrentQueue<short[]>();

    //        // 从第4个字节开始，每2个字节为一个short值
    //        for (int i = 0; i < row; i++)
    //        {
    //            short[] currentRow = new short[col];
    //            for (int j = 0; j < col; j++)
    //            {
    //                int startIndex = 4 + (i * col + j) * 2;
    //                short currentShort = BitConverter.ToInt16(bytes, startIndex);
    //                currentRow[j] = currentShort;
    //            }
    //            queue.Enqueue(currentRow);
    //        }

    //        return queue;

    //    }

    //    protected override byte[] ConvertValueToBytes(ConcurrentQueue<short[]> value)
    //    {
    //        var len = value.Sum(x => x.Length) * 2;
    //        var bytes = new byte[len];
    //        var col = value.ElementAt(0).Length;

    //        // 计算总行数,将行数存到头部
    //        var row = BitConverter.GetBytes(len);
    //        Array.Copy(row, bytes, 4);

    //        for (int i = 0; i < value.Count; i++)
    //        {
    //            for (int j = 0; j < col; j++)
    //            {
    //                var index = i * 2 + j + 4;
    //                var vb = BitConverter.GetBytes(value.ElementAt(i)[j]);
    //                bytes[index] = vb[0];
    //                bytes[index + 1] = vb[1];
    //            }
    //        }

    //        return bytes;
    //    }
    //}
}
            //return value.SelectMany(x => x.SelectMany(b => BitConverter.GetBytes(b))).ToArray();
