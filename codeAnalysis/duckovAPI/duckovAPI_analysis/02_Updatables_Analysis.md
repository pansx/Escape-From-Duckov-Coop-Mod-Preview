# Updatables 文件夹分析

## 概述

Updatables 文件夹定义了游戏中可更新组件的基础接口。

## 文件结构

- `IUpdatable.cs` - 可更新对象的基础接口

## 详细分析

### IUpdatable.cs

```csharp
public interface IUpdatable
{
    void OnUpdate();
}

```

## 关键特点

- 简洁的更新接口设计

- 使用 `Duckov.Utilities.Updatables` 命名空间

- 提供统一的更新机制抽象

## 设计模式

- 接口隔离原则：只定义必要的更新方法

- 策略模式：允许不同对象实现不同的更新逻辑

## 开发建议

- 所有需要定期更新的游戏对象都应实现此接口

- 可以配合 UpdatableManager 进行统一管理

- 适合用于游戏循环中的对象状态更新
