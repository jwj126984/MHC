# 去掉队列重新实现报文接收逻辑 - 产品需求文档

## Overview
- **Summary**: 重新实现 CAN 通信模块的报文接收逻辑，去掉中间队列，直接在接收回调中处理报文，提高实时性和减少内存使用。
- **Purpose**: 解决设备报文停发后界面仍更新的问题，同时优化报文处理性能，减少系统资源占用。
- **Target Users**: 开发人员和最终用户，特别是需要实时监控 CAN 设备状态的场景。

## Goals
- 移除 CAN 通信模块中的消息队列，直接在接收回调中处理报文
- 保持报文解析和 UI 更新的功能完整性
- 提高报文处理的实时性和响应速度
- 减少内存使用和系统资源占用
- 解决设备停发后界面仍更新的问题

## Non-Goals (Out of Scope)
- 不改变 CAN 设备的通信协议和数据格式
- 不修改 UI 界面的显示逻辑
- 不影响现有的信号解析和故障检测功能
- 不改变现有的事件通知机制

## Background & Context
- 当前实现使用 `ConcurrentQueue<CANMessage>` 作为中间缓冲区，在接收回调中入队，在独立线程中出队处理
- 这种实现方式导致设备停发后，队列中仍有旧报文被处理，造成界面持续更新的问题
- 队列的使用增加了内存占用，并且在高流量场景下可能导致延迟

## Functional Requirements
- **FR-1**: 移除 `_messageQueue` 队列，直接在接收回调中处理报文
- **FR-2**: 保持报文解析功能，确保信号值正确解析
- **FR-3**: 保持 UI 更新机制，确保界面能够实时显示报文数据
- **FR-4**: 实现批量处理机制，避免频繁 UI 更新
- **FR-5**: 保持异常处理机制，确保系统稳定性

## Non-Functional Requirements
- **NFR-1**: 报文处理延迟不超过 100ms
- **NFR-2**: 内存使用不超过原实现的 50%
- **NFR-3**: CPU 占用不超过原实现的 80%
- **NFR-4**: 系统稳定性不低于原实现

## Constraints
- **Technical**: 必须保持与现有代码的兼容性，不破坏现有的事件通知机制
- **Business**: 实现时间不超过 2 个工作日
- **Dependencies**: 依赖 ZLG CAN 设备驱动库

## Assumptions
- CAN 设备的通信速率不会超过系统处理能力
- 报文处理逻辑的复杂度不会显著增加
- 移除队列后不会影响系统的稳定性

## Acceptance Criteria

### AC-1: 队列移除
- **Given**: 系统运行中
- **When**: 检查 CANCommunication 类的实现
- **Then**: 不存在 `_messageQueue` 队列，所有报文处理逻辑直接在接收回调中执行
- **Verification**: `programmatic`
- **Notes**: 确认队列相关代码已完全移除

### AC-2: 报文解析功能
- **Given**: CAN 设备发送报文
- **When**: 系统接收到报文
- **Then**: 报文被正确解析，信号值更新正确
- **Verification**: `programmatic`
- **Notes**: 验证解析后的信号值与原实现一致

### AC-3: UI 更新功能
- **Given**: CAN 设备发送报文
- **When**: 系统接收到报文
- **Then**: 界面实时显示报文数据，无延迟
- **Verification**: `human-judgment`
- **Notes**: 验证界面更新的实时性和准确性

### AC-4: 设备停发处理
- **Given**: CAN 设备停止发送报文
- **When**: 系统不再接收到新报文
- **Then**: 界面停止更新，不再显示旧数据
- **Verification**: `human-judgment`
- **Notes**: 验证设备停发后界面是否停止更新

### AC-5: 性能优化
- **Given**: 系统运行中
- **When**: 监测系统资源使用情况
- **Then**: 内存使用和 CPU 占用低于原实现
- **Verification**: `programmatic`
- **Notes**: 对比优化前后的资源使用情况

## Open Questions
- [ ] 批量处理的最佳大小和时间间隔是多少？
- [ ] 如何处理高流量场景下的报文积压？
- [ ] 是否需要保留某种形式的缓冲区以应对突发流量？