

# WheelSimu

WheelSimu 是一款基于 Android 设备的赛车模拟器外设解决方案，通过将 Android 设备的传感器数据（方向盘角度、踏板输入、档位等）转换为 vJoy 虚拟手柄信号，实现与 PC 赛车游戏的完美兼容。

## 功能特性

- **方向盘模拟**：支持 540°/720° 方向盘角度检测，实时映射到 vJoy X 轴
- **三踏板系统**：独立模拟油门、刹车、离合器踏板，精度可调
- **手刹功能**：支持手刹开关信号输入
- **档位控制**：支持升档/降档操作
- **加速度计支持**：双传感器数据采集，提供更精准的动态反馈
- **网络连接**：支持 TCP/IP 直连和局域网自动发现
- **自动重连**：网络断开后自动尝试重连，确保游戏体验连续性
- **vJoy 集成**：通过 vJoy 虚拟手柄驱动，兼容各类赛车游戏

## 系统要求

### Android 端
- Android 4.3 (API 18) 及以上版本
- 设备需支持加速度计传感器
- 横屏模式推荐

### PC 端
- Windows 7/8/10/11 (64位)
- 已安装 [vJoy](https://sourceforge.net/projects/vjoy/) 虚拟手柄驱动
- .NET 6.0 运行时

## 安装说明

### 1. 安装 vJoy 驱动

1. 下载并安装 [vJoy SDK](https://sourceforge.net/projects/vjoy/) 
2. 配置 vJoy 设备：确保启用了以下轴和按钮
   - X Axis（方向盘）
   - Buttons 1-8（手刹、档位等）
   - 可选：Z Axis、RX Axis 等

### 2. 安装 PC 端服务器

位于 `Release/WheelSimuServer.exe`，双击运行即可。

### 3. 安装 Android 端应用

将 `Release/WheelSimu.apk` 安装到 Android 设备上。

## 使用方法

### 启动服务器

1. 运行 `WheelSimuServer.exe`
2. 服务器会自动广播自身 IP 地址
3. 确认 vJoy 状态为 "OWN"（已获取控制权）

### 连接 Android 应用

**方式一：自动发现**
1. 确保手机与 PC 在同一局域网
2. 点击 Android 端的"网络模式"按钮
3. 从列表中选择发现的服务器

**方式二：手动连接**
1. 在 Android 端输入 PC 的 IP 地址
2. 点击"连接"按钮

### 操作说明

| 功能 | 操作方式 |
|------|----------|
| 转向 | 倾斜设备或旋转物理方向盘（如有） |
| 油门/刹车/离合 | 按住对应踏板区域并滑动调整 |
| 手刹 | 切换手刹开关 |
| 升档/降档 | 点击升档/降档按钮 |
| 回正 | 点击"回正角度"按钮 |
| 开启转向 | 启用"转向使能"开关 |

## 项目结构

```
WheelSimu/
├── WheelSimu/                    # Android 应用项目
│   ├── MainActivity.cs           # 主界面与核心逻辑
│   ├── SteeringWheelView.cs      # 方向盘自定义视图
│   ├── PedalGaugeView.cs         # 踏板表盘自定义视图
│   ├── CommonCode.cs             # 公共代码
│   └── Resources/                # 资源文件（布局、图标、样式等）
│
├── WheelSimuServer/              # PC 服务器项目
│   ├── MainForm.cs               # 主窗体与业务逻辑
│   ├── VJoyDiag.cs               # vJoy 诊断工具
│   └── Program.cs                # 程序入口
│
└── Release/                      # 发布文件
    ├── WheelSimu.apk             # Android 安装包
    └── WheelSimuServer.exe       # Windows 服务器
```

## 技术架构

### 通信协议

服务器监听端口 8866，使用 TCP 协议。数据格式为单行 JSON 字符串：

```json
{
  "type": "control",
  "angle": 45.5,
  "throttle": 75,
  "brake": 0,
  "clutch": 0,
  "handbrake": 0,
  "gearUp": 0,
  "gearDown": 0
}
```

发现协议使用 UDP 广播，端口 12001，魔数为 `"WheelSimu"`。

### 核心类说明

- **MainActivity**：传感器管理、网络通信、数据发送
- **SteeringWheelView**：方向盘 UI 渲染，支持角度平滑过渡
- **PedalGaugeView**：踏板表盘 UI，支持触摸交互
- **MainForm (Server)**：vJoy 控制、TCP 服务端、客户端管理

## 编译说明

### Android 项目

```bash
# 需要安装 Xamarin.Android 开发环境
# 使用 Visual Studio 打开 WheelSimu.sln
# 选择 Release 配置，编译生成 APK
```

### 服务器项目

```bash
# 需要 .NET 6.0 SDK
cd WheelSimuServer
dotnet build -c Release
```

## 常见问题

**Q: vJoy 状态显示 MISS**
A: 检查 vJoy 驱动是否正确安装，尝试重新配置 vJoy 设备

**Q: Android 端无法发现服务器**
A: 确认防火墙允许 UDP 12001 端口通信，检查是否在同一网络

**Q: 方向盘反应迟缓**
A: 尝试降低 Android 端的传感器数据发送间隔

**Q: 踏板数值抖动**
A: 启用 PedalGaugeView 的平滑滤波功能

## 许可证

本项目基于 MIT 许可证开源。

## 致谢

- [vJoy](https://sourceforge.net/projects/vjoy/) - 虚拟手柄驱动
- [Xamarin.Android](https://dotnet.microsoft.com/en-us/apps/xamarin/android) - Android 应用开发框架