# DBC信号绑定排查 - 产品需求文档

## Overview
- **Summary**: 根据DBC文件排查每个数据的绑定和所有指令的控制是否有问题，并根据DBC修改有问题的部分。
- **Purpose**: 确保UI显示的数据与DBC文件中定义的信号一致，确保指令控制符合DBC定义的格式。
- **Target Users**: 系统维护人员和技术支持人员。

## Goals
- 分析DBC文件，了解所有信号和指令的定义。
- 检查当前代码中数据绑定的实现是否与DBC一致。
- 检查当前代码中指令控制的实现是否与DBC一致。
- 找出并修复所有与DBC不一致的问题。

## Non-Goals (Out of Scope)
- 不修改DBC文件本身。
- 不添加新的功能或信号。
- 不修改UI布局和样式。

## Background & Context
- 当前系统使用DBC文件定义CAN信号和指令。
- UI通过数据绑定显示这些信号。
- 系统通过CAN通信发送指令控制设备。
- 需要确保代码中的实现与DBC文件定义一致，避免数据显示错误或指令发送失败。

## Functional Requirements
- **FR-1**: 分析DBC文件，列出所有信号和指令的定义。
- **FR-2**: 检查当前代码中数据绑定的实现是否与DBC一致。
- **FR-3**: 检查当前代码中指令控制的实现是否与DBC一致。
- **FR-4**: 修复所有与DBC不一致的问题。

## Non-Functional Requirements
- **NFR-1**: 修复后的代码应该与DBC文件完全一致。
- **NFR-2**: 修复过程应该不影响现有功能。
- **NFR-3**: 实现应该稳定可靠，能够正确处理各种CAN报文。

## Constraints
- **Technical**: 使用C#和WPF实现，基于现有的CAN通信框架。
- **Business**: 修复应该在不影响现有功能的前提下进行。
- **Dependencies**: 依赖现有的CAN通信模块和DBC文件。

## Assumptions
- DBC文件是最新的，包含所有需要的信号和指令。
- CAN设备已经正确连接到计算机。
- 用户已经了解基本的CAN通信知识。

## Acceptance Criteria

### AC-1: DBC文件分析
- **Given**: 系统运行，DBC文件存在
- **When**: 分析DBC文件
- **Then**: 应该列出所有信号和指令的定义，包括信号名称、起始位、长度、数据类型等
- **Verification**: `human-judgment`
- **Notes**: 确保所有信号和指令都被正确识别

### AC-2: 数据绑定检查
- **Given**: 系统运行，DBC文件分析完成
- **When**: 检查当前代码中数据绑定的实现
- **Then**: 应该确保每个UI元素绑定的信号名称、起始位、长度等与DBC一致
- **Verification**: `human-judgment`
- **Notes**: 检查所有电容监控、电机监控、内部监控等UI元素的绑定

### AC-3: 指令控制检查
- **Given**: 系统运行，DBC文件分析完成
- **When**: 检查当前代码中指令控制的实现
- **Then**: 应该确保每个指令的信号名称、起始位、长度等与DBC一致
- **Verification**: `human-judgment`
- **Notes**: 检查充电、放电、休眠等指令的实现

### AC-4: 问题修复
- **Given**: 系统运行，问题已被识别
- **When**: 修复所有与DBC不一致的问题
- **Then**: 修复后的代码应该与DBC文件完全一致
- **Verification**: `human-judgment`
- **Notes**: 确保修复后系统能够正常运行

## Open Questions
- [ ] 当前代码中哪些信号的绑定与DBC不一致？
- [ ] 当前代码中哪些指令的控制与DBC不一致？
- [ ] 修复过程中需要注意哪些潜在的问题？