# Jbin 技术文档索引

本文档目录包含 **Jbin（JSON Binary Hybrid Serialization Protocol）** 的全套技术资料与开发指南。

## 目录结构

- [Jbin 技术白皮书与开发指南 (Jbin_Technical_Documentation.md)](file:///C:/Users/Administrator/Documents/ApeFree/ApeFree.Protocols/ApeFree.Protocols.Json/Jbin/doc/Jbin_Technical_Documentation.md)
  - **1. 概述与核心摘要**：Jbin 混合序列化模型总览与核心设计。
  - **2. 设计背景与技术目标**：解决工业大数组 Base64 膨胀、GC 压力与动态多态兼顾难题。
  - **3. Jbin 核心原理与设计思路**：JSON 拓扑骨架、物理帧布局（Wire Format）、64位魔数 CombinedId 指针规范、零拷贝内存直接映射、对象引用去重与 BlockId 共享机制（基于 .NET 8 宏定义支持）。
  - **4. 与其他序列化方案的多维度横向对比**：Jbin 与 JSON, BSON, MsgPack, Protobuf, FlatBuffers, Apache Arrow 的全方位特性与性能矩阵对比表。
  - **5. Jbin 体系架构与核心组件**：系统架构图（Mermaid）、`JbinObject`、`JbinHeader`、`JbinSerializeContext`、转换器责任链子系统、预置转换器支持矩阵、`JbinExtensions` 表达式树行列转置引擎、`AssemblyWhitelistSerializationBinder` 安全绑定器、`JbinRpcReflector`。
  - **6. Jbin 工作模式与执行流程**：序列化时序图（Mermaid）、反序列化自适应解包时序图（Mermaid）、流式读写模式。
  - **7. 开发者实战教程：自定义 Converter 开发指南**：接口规范、开发步骤、三维空间点云 `Vector3D[]` 自定义 Converter 完整实战代码与单元测试。
  - **8. 安全机制与生产级最佳实践**：多态防御白名单、超大数据流式操作规范、内存 `IDisposable` 生命周期管理。
  - **9. 总结与架构展望**。
