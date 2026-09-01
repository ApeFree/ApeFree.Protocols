# ApeFree.Protocol.ApeFtp

[![NuGet Version](https://img.shields.io/nuget/v/ApeFree.Protocol.ApeFtp.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ApeFree.Protocol.ApeFtp/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ApeFree.Protocol.ApeFtp.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ApeFree.Protocol.ApeFtp/)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg?style=flat-square)](https://github.com/ApeFree/ApeFree.Protocols/blob/main/LICENSE)
[![Target Frameworks](https://img.shields.io/badge/frameworks-netstandard2.0%20%7C%20net10.0-brightgreen.svg?style=flat-square)](https://www.nuget.org/packages/ApeFree.Protocol.ApeFtp/)

`ApeFree.Protocol.ApeFtp` 是一套专为**工业物联网、工控通信、上位机下位机交互以及全双工信道内嵌数据传输**设计的高性能、轻量级二进制文件分段传输协议引擎（Sans-I/O 架构）。

本库采用纯状态机与内存流式编解码设计，不绑定任何具体的网络层（如 TCP、UDP、串口、WebSocket、命名管道等），既可以独立作为轻量级 FTP 传输引擎运行，也可以无缝内嵌在任意主业务协议中作为专用数据传输子协议。

---

## 目录

- [🌟 核心特性](#-核心特性)
- [支持平台与环境](#支持平台与环境)
- [📦 安装方式](#-安装方式)
- [🚀 快速上手](#-快速上手)
  - [1. 纯内存与网络信道流式对接](#1-纯内存与网络信道流式对接)
  - [2. 大文件流式传输（避免 OOM）](#2-大文件流式传输避免-oom)
  - [3. 极速秒传（Fast Upload / Deduplication）](#3-极速秒传fast-upload--deduplication)
  - [4. 断点续传（Breakpoint Resume）](#4-断点续传breakpoint-resume)
  - [5. 数据校验与单分片错误重传](#5-数据校验与单分片错误重传)
- [协议帧结构与物理设计](#协议帧结构与物理设计)
- [架构组成与扩展点](#架构组成与扩展点)
- [📄 开源许可证](#-开源许可证)

---

## 🌟 核心特性

- **纯粹的 Sans-I/O 协议引擎**：彻底剥离具体网络传输依赖，输入输出仅面向二进制字节流（`ReadOnlySpan<byte>` / `byte[]`），可自由挂载在 TCP、串口 RS485、CAN、蓝牙或自定义复合协议载荷中。
- **动态突发窗口确认流控（Burst / Window ACK）**：告别传统的停等式（Stop-and-Wait）单包确认，在协商阶段约定突发窗口大小（`WindowSize`），支持多包连续突发发送与聚合确认，大幅提升网络吞吐量。
- **可变长度整型（VarInt）紧凑编码**：基于 LEB128 规范，针对包长、偏移量与分段序号进行动态压缩，小文件小分段仅占 1~2 字节，无缝扩展支持大于 4GB 的超大文件传输。
- **双层校验与单包差错恢复**：
  - **帧层**：`0xAF 0x46` 魔数帧头 + 载荷 CRC32，防止半包/粘包及误码串帧，支持丢包自动重同步。
  - **分片层**：单数据包内置 IEEE 802.3 CRC32 校验，分段损坏即时重传，无需全量推倒重来。
  - **任务层**：传输完成后执行全量 MD5 完整性比对并原子落盘提交。
- **任务状态与物理存储解耦**：
  - **`ITransferSessionStore`**：抽象会话状态与进度仓储，支持内存并发管理或持久化数据库/本地文件断点记录。
  - **`ITransferDataSource` / `ITransferDataSink`**：解耦数据读取与写入，内置内存（`MemoryDataSinkSource`）与流式文件（`FileDataSinkSource`）实现，支持乱序写入与秒传检测。

---

## 支持平台与环境

| 目标框架 | 说明 |
|---|---|
| **.NET Standard 2.0** | 兼容 .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5/6/7/8/9, Unity, Mono |
| **.NET 10.0** | 针对最新 .NET 运行时编译优化 |

---

## 📦 安装方式

### .NET CLI
```bash
dotnet add package ApeFree.Protocol.ApeFtp
```

### Package Manager
```powershell
Install-Package ApeFree.Protocol.ApeFtp
```

### PackageReference
```xml
<PackageReference Include="ApeFree.Protocol.ApeFtp" Version="0.1.0" />
```

---

## 🚀 快速上手

### 1. 纯内存与网络信道流式对接

以下示例展示了如何在发送端和接收端之间建立纯内存状态机对接（在实际网络应用中，只需将 `PacketReadyToSend` 发送到网络 Socket，收到网络字节后调用 `engine.Feed(...)` 即可）：

```csharp
using System;
using ApeFree.Protocol.ApeFtp.Engine;
using ApeFree.Protocol.ApeFtp.Storage;

// 1. 准备待发送的数据源与接收目标（基于内存）
byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes("Hello, ApeFtp Protocol!");
using var dataSource = new MemoryDataSource(fileBytes, "hello.txt");
using var dataSink = new MemoryDataSink(dataSource.TotalLength);

// 2. 初始化发送端与接收端协议引擎
using var sender = new ApeFtpSenderEngine(dataSource, defaultChunkSize: 1024, defaultWindowSize: 4);
using var receiver = new ApeFtpReceiverEngine(req => dataSink);

// 3. 对接两端的输入输出信道（此处以直接事件转发为例）
sender.PacketReadyToSend += (s, e) => receiver.Feed(e.EncodedFrame);
receiver.PacketReadyToSend += (s, e) => sender.Feed(e.EncodedFrame);

// 4. 监听传输事件
sender.ProgressChanged += (s, e) => Console.WriteLine($"发送进度: {e.ProgressPercentage:F1}%");
sender.Completed += (s, e) => Console.WriteLine($"发送端完成！是否秒传: {e.IsFastUpload}");
receiver.Completed += (s, e) => Console.WriteLine("接收端完成并校验通过！");

// 5. 启动传输协商
sender.Start();
```

---

### 2. 大文件流式传输（避免 OOM）

对于数百 MB 甚至数 GB 的大型文件，使用 `FileDataSource` 和 `FileDataSink` 可以以流式切片读写，避免全量读入内存：

```csharp
using ApeFree.Protocol.ApeFtp.Engine;
using ApeFree.Protocol.ApeFtp.Storage;

string sourceFilePath = @"C:\Data\LargeDataset.zip";
string targetFilePath = @"C:\Received\LargeDataset.zip";

// 创建流式文件数据源（内部流式计算 MD5，零全量内存加载）
using var fileSource = new FileDataSource(sourceFilePath);

// 发送端配置 64KB 单分片，8 包突发窗口
using var sender = new ApeFtpSenderEngine(fileSource, defaultChunkSize: 64 * 1024, defaultWindowSize: 8);

// 接收端配置：根据申请请求创建目标落盘 DataSink
using var receiver = new ApeFtpReceiverEngine(demandReq =>
{
    // 创建流式文件写入目标（写入 .part 临时文件，校验成功后原子重命名）
    return new FileDataSink(targetFilePath, demandReq.TotalLength);
});

// 对接实际网络/串口信道（例如 TCP Client / Server）
sender.PacketReadyToSend += (s, e) => tcpClient.Send(e.EncodedFrame);
tcpClient.OnDataReceived += (s, e) => sender.Feed(e.Data);

receiver.PacketReadyToSend += (s, e) => tcpServerClient.Send(e.EncodedFrame);
tcpServerClient.OnDataReceived += (s, e) => receiver.Feed(e.Data);

// 启动传输
sender.Start();
```

---

### 3. 极速秒传（Fast Upload / Deduplication）

当接收端的 `ITransferSessionStore` 或存储区中已存在相同哈希（MD5）且标记为已完成的任务时，申请阶段直接返回 `ResultCode.Completed`，触发秒传，无需重复传输任何分片：

```csharp
var store = new InMemoryTransferSessionStore();

// 预先向接收端仓储注册已存在文件的哈希记录
store.SaveOrUpdateSession(new TransferSessionRecord(fileHash, fileLength, 64 * 1024)
{
    State = ApeFree.Protocol.ApeFtp.Core.SessionState.Completed
});

using var receiver = new ApeFtpReceiverEngine(req => new MemoryDataSink(req.TotalLength), store);
using var sender = new ApeFtpSenderEngine(dataSource);

sender.Completed += (s, e) =>
{
    if (e.IsFastUpload)
    {
        Console.WriteLine("文件已在目标端存在，已秒传！");
    }
};

sender.Start();
```

---

### 4. 断点续传（Breakpoint Resume）

当网络发生异常中断重新连接时，接收端会根据已接收落盘的有效字节偏移量通过 `DemandResponse.ResumedOffset` 告知发送端，发送端自动从中断处继续发送：

```csharp
// 接收端会在会话仓储中持续维护 ReceivedBytes 进度
// 重新握手时，发送端自动从 ResumedOffset 偏移切片并推进进度
sender.ProgressChanged += (s, e) =>
{
    Console.WriteLine($"已传输: {e.TransferredBytes} / {e.TotalBytes} 字节");
};
```

---

### 5. 数据校验与单分片错误重传

在传输过程中，若某个分片由于传输误码导致 CRC32 校验失败，接收端会主动回复 `ResultCode.ChunkCrcMismatch`，发送端引擎会自动回退并仅重新发送损坏的分片，保障整体传输的高效性与可靠性。

---

## 协议帧结构与物理设计

所有传输帧采用统一定长/自描述变长二进制格式：

```
+------------------+-------------------+----------------------+------------------------+-------------------+
|  Magic (2 Bytes) |  PacketType (1B)  |  PayloadLen (VarInt) |   Payload (N Bytes)    | PayloadCRC32 (4B) |
|   0xAF, 0x46     |  0x01 ~ 0x06      |   1 ~ 10 Bytes       |   FileKey + Fields     |   IEEE 802.3      |
+------------------+-------------------+----------------------+------------------------+-------------------+
```

### 报文类型（PacketType）

| 类型代码 | 报文名称 | 发送方向 | 核心字段 |
|---|---|---|---|
| `0x01` | **`DemandRequest`** | Sender $\rightarrow$ Receiver | `FileKey(MD5)`, `TotalLength(VarInt)`, `ChunkSize(VarInt)`, `WindowSize(VarInt)`, `FileName` |
| `0x02` | **`DemandResponse`** | Receiver $\rightarrow$ Sender | `FileKey`, `ResultCode`, `AcceptedChunkSize`, `AcceptedWindowSize`, `ResumedOffset`, `Message` |
| `0x03` | **`DataPacket`** | Sender $\rightarrow$ Receiver | `FileKey`, `ChunkIndex`, `Offset`, `ChunkCrc32(4B)`, `DataLength`, `DataBytes` |
| `0x04` | **`AckResponse`** | Receiver $\rightarrow$ Sender | `FileKey`, `ResultCode`, `AckChunkIndex`, `AckCount`, `Message` |
| `0x05` | **`CancelRequest`** | Sender / Receiver | `FileKey`, `ReasonCode`, `Message` |
| `0x06` | **`CancelResponse`** | Sender / Receiver | `FileKey`, `ResultCode` |

---

## 架构组成与扩展点

```
ApeFree.Protocol.ApeFtp/
├── Core/
│   ├── Enums.cs                      # 数据包类型、结果码与会话状态定义
│   ├── VarInt.cs                     # 高性能可变长整型编解码（LEB128）
│   ├── Crc32.cs                      # IEEE 802.3 CRC32 循环冗余校验
│   └── Packets/                      # 强类型数据包实体
├── Codec/
│   ├── ApeFtpFrameEncoder.cs         # 二进制帧编码器
│   └── ApeFtpFrameDecoder.cs         # 流式帧解包器（含粘包、半包与重入防护）
├── Storage/
│   ├── ITransferSessionStore.cs      # 任务状态与断点进度仓储接口
│   ├── InMemoryTransferSessionStore.cs # 基于内存并发字典的仓储默认实现
│   ├── ITransferDataSource.cs        # 数据源读取接口
│   ├── ITransferDataSink.cs          # 数据接收写入与校验目标接口
│   ├── MemoryDataSinkSource.cs       # 内存字节数据存取实现
│   └── FileDataSinkSource.cs         # 文件流式切片与原子提交存取实现
└── Engine/
    ├── ApeFtpSenderEngine.cs         # 发送端协议状态机引擎
    ├── ApeFtpReceiverEngine.cs       # 接收端协议状态机引擎
    └── TransferEventArgs.cs          # 进度、完成、失败与发包事件参数
```

---

## 📄 开源许可证

本项目遵循 [Apache-2.0](https://github.com/ApeFree/ApeFree.Protocols/blob/main/LICENSE) 开源许可证。

Copyright © 2022-2026 ApeFree, All Rights Reserved.
