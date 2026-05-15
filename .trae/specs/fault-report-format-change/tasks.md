# 故障报告格式修改 - 实现计划

## [ ] Task 1: 修改FaultModel数据结构
- **Priority**: P0
- **Depends On**: None
- **Description**: 
  - 扩展FaultModel，添加完整信号值字典属性
  - 保留原有Time、Location、Type字段
- **Acceptance Criteria Addressed**: AC-1
- **Test Requirements**:
  - `programmatic` TR-1.1: FaultModel包含SignalValues字典属性
  - `programmatic` TR-1.2: 能够存储和检索任意信号值
- **Notes**: 需要考虑SignalValues的序列化方式

## [ ] Task 2: 修改FaultLogger写入逻辑
- **Priority**: P0
- **Depends On**: Task 1
- **Description**: 
  - 修改LogFault方法，支持动态列生成
  - 每行写入完整故障帧的所有信号值
  - 表头根据信号列表动态生成
- **Acceptance Criteria Addressed**: AC-1, AC-2
- **Test Requirements**:
  - `programmatic` TR-2.1: Excel文件包含所有信号列
  - `programmatic` TR-2.2: 每行记录完整的信号状态
  - `human-judgement` TR-2.3: Excel格式清晰，易于阅读
- **Notes**: 需要处理表头顺序一致性问题

## [ ] Task 3: 修改FaultManager记录逻辑
- **Priority**: P0
- **Depends On**: Task 1, Task 2
- **Description**: 
  - 修改RecordFault方法，传入完整信号值字典
  - 确保异步写入时传递完整上下文
- **Acceptance Criteria Addressed**: AC-1
- **Test Requirements**:
  - `programmatic` TR-3.1: 故障记录时包含完整信号上下文
  - `programmatic` TR-3.2: 异步写入不丢失数据
- **Notes**: 需要协调与MainViewModel的数据传递

## [ ] Task 4: 修改MainViewModel故障检测逻辑
- **Priority**: P0
- **Depends On**: Task 3
- **Description**: 
  - 修改CheckForFaults方法，传递当前所有信号值
  - 更新RecordFault调用方式
- **Acceptance Criteria Addressed**: AC-1, AC-3
- **Test Requirements**:
  - `programmatic` TR-4.1: 故障检测时传递完整信号值
  - `programmatic` TR-4.2: 故障类型正确标记
- **Notes**: 需要确保SignalValues字典在传递时的线程安全

## [ ] Task 5: 测试与验证
- **Priority**: P1
- **Depends On**: Task 1-4
- **Description**: 
  - 验证故障记录功能正常工作
  - 验证Excel格式符合预期
- **Acceptance Criteria Addressed**: AC-1, AC-2, AC-3
- **Test Requirements**:
  - `programmatic` TR-5.1: 故障触发时正确生成Excel文件
  - `human-judgement` TR-5.2: Excel内容准确反映故障状态
- **Notes**: 需要手动验证Excel输出格式