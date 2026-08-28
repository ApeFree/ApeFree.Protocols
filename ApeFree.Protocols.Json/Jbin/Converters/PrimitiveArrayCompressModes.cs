namespace ApeFree.Protocols.Json.Jbin
{
    /// <summary>
    /// 32位整数数组序列化与压缩模式
    /// </summary>
    public enum Int32ArrayCompressMode : byte
    {
        /// <summary>
        /// 原始连续内存块 (Buffer.BlockCopy)
        /// </summary>
        Raw = 0,

        /// <summary>
        /// Frame of Reference (FoR) 基准帧压缩算法
        /// </summary>
        FrameOfReference = 1,
    }

    /// <summary>
    /// 单精度浮点数组序列化与压缩模式
    /// </summary>
    public enum SingleArrayCompressMode : byte
    {
        /// <summary>
        /// 原始连续内存块 (Buffer.BlockCopy)
        /// </summary>
        Raw = 0,

        /// <summary>
        /// Gorilla 浮点时序压缩算法
        /// </summary>
        Gorilla = 1,
    }

    /// <summary>
    /// 双精度浮点数组序列化与压缩模式
    /// </summary>
    public enum DoubleArrayCompressMode : byte
    {
        /// <summary>
        /// 原始连续内存块 (Buffer.BlockCopy)
        /// </summary>
        Raw = 0,

        /// <summary>
        /// Gorilla 浮点时序压缩算法
        /// </summary>
        Gorilla = 1,
    }

    /// <summary>
    /// 64位整数数组序列化与压缩模式
    /// </summary>
    public enum Int64ArrayCompressMode : byte
    {
        /// <summary>
        /// 原始连续内存块 (Buffer.BlockCopy)
        /// </summary>
        Raw = 0,

        /// <summary>
        /// Simple-8b 整数压缩算法
        /// </summary>
        Simple8b = 1,
    }

    /// <summary>
    /// 16位整数数组序列化与压缩模式
    /// </summary>
    public enum Int16ArrayCompressMode : byte
    {
        /// <summary>
        /// 原始连续内存块 (Buffer.BlockCopy)
        /// </summary>
        Raw = 0,

        /// <summary>
        /// Delta + BitPacking 差分位压缩算法
        /// </summary>
        DeltaBitPacking = 1,
    }

    /// <summary>
    /// 字符串数组序列化与压缩模式
    /// </summary>
    public enum StringArrayCompressMode : byte
    {
        /// <summary>
        /// 字典去重索引模式
        /// </summary>
        Dictionary = 0,

        /// <summary>
        /// Deflate 紧凑流压缩模式
        /// </summary>
        Deflate = 1,
    }
}
