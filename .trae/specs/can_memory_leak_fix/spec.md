# CAN通信内存泄漏问题修复 - 产品需求文档

## Overview
- **Summary**: 软件在接收10ms周期的CAN报文连续运行半小时左右后闪退，经分析是由于消息队列无限制增长导致的内存泄漏问题。
- **Purpose**: 修复CAN通信模块中的内存泄漏问题，确保软件能够长时间稳定运行。
- **Target Users**: 电容监测系统的测试人员和最终用户

## Goals
- 修复消息队列无限增长导致的内存泄漏问题
- 确保软件在高负载CAN通信场景下能够稳定运行超过半小时
- 添加队列容量限制和溢出保护机制

## Non-Goals (Out of Scope)
- 不修改CAN通信协议本身
- 不修改UI界面布局
- 不添加新的功能模块

## Background & Context
- 当前系统使用`ConcurrentQueue<CANMessage>`存储接收到的CAN报文
- 接收定时器每5ms触发一次，没有流量控制
- 处理定时器每5ms触发一次，最多处理50条消息
- 10ms周期的报文意味着每秒100条消息，半小时约180,000条消息
- 由于处理速度跟不上接收速度，队列会无限增长导致内存耗尽

## Functional Requirements
- **FR-1**: 限制CAN消息队列的最大容量
- **FR-2**: 当队列满时，丢弃最旧的消息（或新消息）并记录日志
- **FR-3**: 优化消息处理流程，提高处理效率
- **FR-4**: 添加内存使用监控和告警机制

## Non-Functional Requirements
- **NFR-1**: 内存使用量在长时间运行后保持稳定
- **NFR-2**: 软件在10ms周期报文负载下稳定运行超过1小时
- **NFR-3**: 队列溢出时不影响正常消息处理

## Constraints
- **Technical**: 使用.NET 10框架，WPF应用
- **Dependencies**: 保持与现有ZLG CAN API的兼容性

## Assumptions
- 10ms周期的CAN报文是系统正常运行的最大负载
- 消息处理延迟在100ms内是可接受的

## Acceptance Criteria

### AC-1: 队列容量限制
- **Given**: CAN通信正在接收10ms周期的报文
- **When**: 队列达到最大容量
- **Then**: 新消息入队时自动移除最旧的消息，并记录警告日志
- **Verification**: `programmatic`

### AC-2: 内存稳定性
- **Given**: 软件连续运行1小时，接收10ms周期的CAN报文
- **When**: 监控内存使用情况
- **Then**: 内存使用量保持稳定，不持续增长
- **Verification**: `programmatic`

### AC-3: 消息处理不中断
- **Given**: 队列已满并发生溢出
- **When**: 继续接收新消息
- **Then**: 消息处理继续正常进行，不抛出异常
- **Verification**: `programmatic`

## Open Questions
- [ ] 是否需要保存溢出的消息到文件？
- [ ] 最大队列容量应该设置为多少？

---

## 问题分析

### 根本原因

在`CANCommunication.cs`中的`ProcessCallback`方法存在逻辑缺陷：

```csharp
private void ProcessCallback(object? state)
{
    // ...
    while (_messageQueue.TryDequeue(out CANMessage? message) && processedCount < 50)
    {
        long count = Interlocked.Read(ref _messageCount);
        if (count > MaxMessagesPerSecond)  // 问题：当超过限制时，消息被重新入队
        {
            _messageQueue.Enqueue(message);  // 重新入队导致队列无限增长
            break;
        }
        // ...
    }
}
```

**问题分析**：
1. `_messageCount`每秒重置一次
2. 当`count > MaxMessagesPerSecond`时，当前消息被重新入队，循环中断
3. 但`ReceiveCallback`继续以5ms间隔入队消息，没有限制
4. 长期运行导致队列无限增长，最终内存耗尽

### 解决方案

1. **限制队列最大容量**：设置`ConcurrentQueue`的最大容量，超过时丢弃旧消息
2. **优化处理逻辑**：移除不合理的消息重新入队逻辑
3. **添加溢出保护**：当队列满时记录警告日志