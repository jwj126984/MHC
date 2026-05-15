# CAN配置适配 - 产品需求文档

## Overview
- **Summary**: 根据其他项目的CAN通信实现，修改当前项目的CAN配置，使其区分USBCANFD200U和USBCAN1的参数配置。
- **Purpose**: 确保系统能够正确配置和使用不同类型的CAN设备，特别是USBCANFD200U和USBCAN1。
- **Target Users**: 系统维护人员和技术支持人员。

## Goals
- 修改当前项目的CAN配置逻辑，区分USBCANFD200U和USBCAN1的参数配置。
- 确保USBCANFD200U设备能够正确初始化和工作。
- 确保USBCAN1设备的配置逻辑保持不变，继续正常工作。

## Non-Goals (Out of Scope)
- 不修改其他CAN设备类型的配置逻辑。
- 不修改CAN通信的核心功能。
- 不添加新的CAN设备类型。

## Background & Context
- 当前项目的CAN配置逻辑可能没有充分区分不同设备类型的参数配置。
- 其他项目的CanCommunication.cs文件提供了一个很好的参考，展示了如何区分不同设备类型的配置。
- USBCANFD200U是CAN FD设备，需要特殊的配置参数。
- USBCAN1是传统CAN设备，需要不同的配置参数。

## Functional Requirements
- **FR-1**: 修改当前项目的CAN配置逻辑，区分USBCANFD200U和USBCAN1的参数配置。
- **FR-2**: 确保USBCANFD200U设备能够正确初始化和工作。
- **FR-3**: 确保USBCAN1设备的配置逻辑保持不变，继续正常工作。

## Non-Functional Requirements
- **NFR-1**: 配置修改后，系统应能够正确识别和使用USBCAN1和USBCANFD200U设备。
- **NFR-2**: 配置过程应简单明了，用户能够轻松选择所需的设备类型。
- **NFR-3**: 系统应能够处理设备连接失败的情况，并给出明确的错误提示。

## Constraints
- **Technical**: 使用C#和WPF实现，基于周立功的CAN API。
- **Business**: 只支持USBCAN1和USBCANFD200U两种设备类型。
- **Dependencies**: 依赖周立功的CAN API库。

## Assumptions
- 系统已经正确安装了周立功的CAN API库。
- USBCAN1和USBCANFD200U设备已经正确连接到计算机。
- 用户已经了解基本的CAN设备配置知识。

## Acceptance Criteria

### AC-1: 设备类型能够正确区分
- **Given**: 用户选择不同的设备类型
- **When**: 系统尝试初始化设备
- **Then**: 系统能够根据设备类型使用正确的配置参数
- **Verification**: `programmatic`
- **Notes**: 确保USBCANFD200U和USBCAN1使用不同的配置参数

### AC-2: USBCANFD200U设备能够正确初始化
- **Given**: 用户选择USBCANFD200U设备并点击连接按钮
- **When**: 系统尝试初始化设备
- **Then**: 设备能够成功初始化，状态显示为连接成功
- **Verification**: `programmatic`
- **Notes**: 确保设备初始化过程符合周立功官方协议

### AC-3: USBCAN1设备能够正常工作
- **Given**: 用户选择USBCAN1设备并点击连接按钮
- **When**: 系统尝试初始化设备
- **Then**: 设备能够成功初始化，状态显示为连接成功
- **Verification**: `programmatic`
- **Notes**: 确保USBCAN1设备的配置逻辑没有受到影响

### AC-4: 设备连接失败时给出明确的错误提示
- **Given**: 用户选择一个不存在的设备或设备未正确连接
- **When**: 点击连接按钮
- **Then**: 系统显示明确的错误提示，说明连接失败的原因
- **Verification**: `human-judgment`
- **Notes**: 错误提示应清晰易懂，帮助用户快速定位问题

## Open Questions
- [ ] 当前项目的CAN配置逻辑是如何实现的？
- [ ] 当前项目是否已经支持USBCANFD200U和USBCAN1设备？