# QuickHotkeyLauncher（Windows 应用快捷键工具）

一个面向 Windows 10/11 的轻量级本地工具：为常用应用绑定全局快捷键，实现一键启动、置前、再次触发最小化。

## 功能特性

- 全局快捷键绑定（`RegisterHotKey`）
- 应用管理：添加、移除、重设、清除快捷键
- 添加来源：
  - 已安装应用列表（开始菜单 + 注册表）
  - 自定义 `.exe` 路径
- 触发逻辑：
  - 应用未运行：启动应用
  - 应用运行但不在前台：置前显示
  - 应用在前台：快速最小化
- 支持中英文界面（跟随系统/中文/英文）
- 托盘菜单与开机启动

## 运行环境

- Windows 10 / Windows 11
- .NET 8 SDK（开发与构建）

## 项目结构

- `src/QuickHotkeyLauncher/Program.cs`：程序入口与单实例
- `src/QuickHotkeyLauncher/Forms/`：主窗体与弹窗
- `src/QuickHotkeyLauncher/Services/`：热键、窗口调度、配置、开机启动等服务
- `src/QuickHotkeyLauncher/Localization/`：语言切换逻辑
- `src/QuickHotkeyLauncher/UI/`：UI 主题与控件
- `docs/`：设计与任务文档

## 本地开发

```powershell
cd src/QuickHotkeyLauncher
dotnet restore
dotnet build -c Release
dotnet run -c Release
```

## 配置文件

默认存储路径：

`%LocalAppData%\QuickHotkeyLauncher\config.json`

## 首个发布版本

- 版本：`v1.0.0`
- 发布产物：`releases/v1.0.0/QuickHotkeyLauncher-v1.0.0-win-x64.zip`

## 许可证

本项目使用 [MIT License](LICENSE)。

