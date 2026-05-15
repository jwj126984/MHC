# 故障报告格式修改 - 产品需求文档

## Overview
- **Summary**: 修改故障日志的Excel输出格式，从每行记录单个故障改为每行记录当前故障帧的所有信号状态，每列对应一种信号。
- **Purpose**: 提供更完整的故障上下文信息，便于分析故障发生时的系统状态。
- **Target Users**: 系统维护人员、故障分析工程师

## Goals
- 将故障日志从"每行单个故障"格式改为"每行完整故障帧"格式
- 每列对应一种信号类型，记录该信号在故障发生时的具体值
- 保留故障时间戳和故障类型信息

## Non-Goals (Out of Scope)
- 不修改故障检测逻辑
- 不修改故障状态跟踪机制
- 不修改CAN通信相关代码

## Background & Context
当前系统通过`FaultLogger`类将故障记录写入Excel文件，格式为：
- 故障发生时间 | 故障名称位置 | 故障种类类型 | 采样故障具体值

这种格式每次只记录单个故障信息，缺乏故障发生时的完整系统状态上下文。

## Functional Requirements
- **FR-1**: 每行记录一个故障帧，包含所有相关信号的状态值
- **FR-2**: 每列对应一种信号类型（如Cell1Vol、Cell1OverVoltageState等）
- **FR-3**: 保留故障时间戳和故障类型标识列
- **FR-4**: 支持动态添加新的信号列

## Non-Functional Requirements
- **NFR-1**: 保持异步写入，不阻塞UI线程
- **NFR-2**: 保持线程安全，支持并发写入
- **NFR-3**: 保持向后兼容的文件存储路径

## Constraints
- **Technical**: 使用NPOI库操作Excel，.NET 10框架
- **Dependencies**: 依赖MainViewModel中的信号定义

## Assumptions
- 信号列表在MainViewModel中定义，包含所有需要记录的信号
- 故障发生时所有信号值都已更新到SignalValues字典中

## Acceptance Criteria

### AC-1: 故障帧完整记录
- **Given**: 系统检测到故障
- **When**: 触发故障记录
- **Then**: Excel中每行包含该故障帧的所有信号值
- **Verification**: `programmatic`

### AC-2: 信号列动态生成
- **Given**: MainViewModel中定义了新信号
- **When**: 系统启动并初始化
- **Then**: Excel表头自动包含该新信号列
- **Verification**: `programmatic`

### AC-3: 故障类型标识
- **Given**: 发生多种类型故障
- **When**: 记录故障帧
- **Then**: 每行标记对应的故障类型
- **Verification**: `programmatic`

## Open Questions
- [ ] 是否需要在每行标记具体哪些信号触发了故障？
- [ ] 是否需要保留历史故障记录格式？