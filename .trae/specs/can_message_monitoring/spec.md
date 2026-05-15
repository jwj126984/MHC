# CAN报文监控修复 - 产品需求文档

## Overview
- **Summary**: 修复CAN报文监控功能，确保能够正确显示已接收到的报文，并且在只有CAN FD报文时也能正确处理。
- **Purpose**: 解决报文监控不显示已接收到的报文的问题，以及修复只有CAN FD报文时无法获取的问题。
- **Target Users**: 系统维护人员和技术支持人员。

## Goals
- 修复ReceiveCallback方法，确保即使没有标准CAN报文，也会继续检查和处理CAN FD报文。
- 确保CAN报文监控能够正确显示已接收到的报文。
- 验证修复后的功能能够正常工作。

## Non-Goals (Out of Scope)
- 不修改其他CAN通信功能。
- 不添加新的监控功能。
- 不修改其他UI元素。

## Background & Context
- 当前系统的CAN报文监控功能存在两个问题：
  1. 报文监控没有成功显示已接收到的报文
  2. 如果只有CAN FD报文，会在检查标准CAN报文数量时直接返回，导致获取不到CAN FD报文
- 这些问题会影响系统的调试和故障排查能力。

## Functional Requirements
- **FR-1**: 修复ReceiveCallback方法，确保即使没有标准CAN报文，也会继续检查和处理CAN FD报文。
- **FR-2**: 确保CAN报文监控能够正确显示已接收到的报文。
- **FR-3**: 验证修复后的功能能够正常工作。

## Non-Functional Requirements
- **NFR-1**: 修复后的功能应该稳定可靠，能够正确处理各种报文类型。
- **NFR-2**: 修复过程应该不影响其他CAN通信功能。
- **NFR-3**: 修复后的功能应该能够在各种网络条件下正常工作。

## Constraints
- **Technical**: 使用C#和WPF实现，基于周立功的CAN API。
- **Business**: 修复应该在不影响现有功能的前提下进行。
- **Dependencies**: 依赖周立功的CAN API库。

## Assumptions
- 系统已经正确安装了周立功的CAN API库。
- CAN设备已经正确连接到计算机。
- 用户已经了解基本的CAN通信知识。

## Acceptance Criteria

### AC-1: 修复ReceiveCallback方法
- **Given**: 系统接收到CAN FD报文
- **When**: 系统调用ReceiveCallback方法
- **Then**: 系统应该能够正确检查和处理CAN FD报文，即使没有标准CAN报文
- **Verification**: `programmatic`
- **Notes**: 确保ReceiveCallback方法不会在检查标准CAN报文数量为0时直接返回

### AC-2: 报文监控能够正确显示已接收到的报文
- **Given**: 系统接收到CAN报文
- **When**: 查看CAN报文监控界面
- **Then**: 界面应该显示已接收到的报文
- **Verification**: `human-judgment`
- **Notes**: 确保CANFrames集合能够正确添加和显示接收到的报文

### AC-3: 修复后的功能能够正常工作
- **Given**: 系统运行
- **When**: 发送和接收CAN报文
- **Then**: 系统应该能够正确处理和显示所有类型的CAN报文
- **Verification**: `programmatic`
- **Notes**: 测试不同类型的CAN报文，包括标准CAN报文和CAN FD报文

## Open Questions
- [ ] 为什么CAN报文监控没有成功显示已接收到的报文？
- [ ] CANCommunication类中的OnMessageReceived事件是否被正确触发？
- [ ] MainWindow.xaml.cs中的OnCANMessageReceived方法是否被正确订阅？