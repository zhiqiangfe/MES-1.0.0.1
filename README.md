# HTHIUM MES

## 版本信息

- 当前版本：`v1.0.0.1`
- 版本阶段：第一版，可运行原型
- 发布日期：2026-10-12
- 目标：完成单机上位机接入、在线设备监控、账号管理和浏览器监控界面的基础闭环

第一阶段已经实现在线设备监控闭环：HTHIUM发送心跳，ASP.NET Core维护设备在线状态，Vue浏览器页面通过HTTP查询和SignalR实时刷新。

浏览器端同时提供MES多设备监控UI原型，包含生产运行总览、设备中心、报警中心、气缸健康、伺服健康、PLC报警快照和数据质量页面。当前原型数据集中维护在 `HTHIUM.Mes.Web/src/data/mockMes.ts`，后续可逐页替换为真实API。

## 第一版功能模块

| 模块 | 当前能力 | 数据状态 |
| --- | --- | --- |
| 用户登录与会话 | 管理员/操作员登录、HttpOnly Cookie会话、退出登录 | 真实数据库 |
| 账号管理 | 管理员新增、查询和删除账号，密码哈希保存 | 真实数据库 |
| 边缘设备接入 | 接收HTHIUM单机上位机心跳，按设备编码更新状态 | 真实接口 |
| 在线设备监控 | 设备在线/离线、PLC连接状态、版本和最后心跳时间 | 真实接口与数据库 |
| 实时状态推送 | SignalR推送设备状态变化，前端自动重连并定时补偿查询 | 真实链路 |
| 离线判定 | 超过配置时间未收到心跳自动标记离线 | 真实后台服务 |
| 生产运行总览 | 产线、设备、报警和健康状态综合看板 | 第一版模拟数据 |
| 报警事件中心 | 报警统计、筛选和事件展示 | 第一版模拟数据 |
| 气缸健康分析 | 设备选择、气缸状态和健康趋势界面 | 第一版模拟数据 |
| 伺服健康分析 | 设备选择、伺服状态和健康趋势界面 | 第一版模拟数据 |
| PLC报警快照 | 快照事件、参数帧和证据链展示 | 第一版模拟数据 |
| 数据质量中心 | 延迟、断线、丢帧和补传状态展示 | 第一版模拟数据 |
| 健康检查 | `/health/live`、`/health/ready`服务检查 | 真实接口 |
| 工程文档与测试 | 架构、API、数据库、可靠性文档和设备监控单元测试 | 已提供 |

## 工程结构

```text
HTHIUM.Mes.sln
src/HTHIUM.Mes.Api             ASP.NET Core API与SignalR
src/HTHIUM.Mes.Application     在线设备监控与账号管理用例
src/HTHIUM.Mes.Domain          设备状态与用户领域模型
src/HTHIUM.Mes.Infrastructure  MySQL持久化、建库与仓储实现
src/HTHIUM.Mes.Contracts       API契约
tests/HTHIUM.Mes.Tests.Unit    单元测试
HTHIUM.Mes.Web                 Vue 3浏览器前端
```

## 当前接口

```text
POST /api/v1/edge/devices/heartbeat
POST /api/v1/auth/login
GET  /api/v1/auth/session
POST /api/v1/auth/logout
GET  /api/v1/admin/users
POST /api/v1/admin/users
DELETE /api/v1/admin/users/{userId}
GET  /api/v1/mes/devices
GET  /api/v1/mes/devices/{deviceCode}
GET  /api/v1/mes/devices/summary
GET  /health/live
GET  /health/ready
WS   /hubs/operations
```

设备超过 `DeviceMonitoring:OnlineTimeoutSeconds` 未收到心跳时自动标记离线，默认15秒。除边缘设备心跳和健康检查外，MES页面、查询接口及SignalR通道均要求登录；账号管理接口仅允许管理员访问。

## 独立数据库与初始管理员

MES使用独立MySQL数据库 `hthium_mes`，首次以Development环境启动时自动创建数据库和数据表。初始管理员仅在用户表为空时创建，密码必须由环境变量提供，源码和配置文件不保存明文密码：

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ConnectionStrings__MesDatabase='server=127.0.0.1;database=hthium_mes;uid=root;pwd=你的数据库密码;Port=3306;SslMode=None;AllowPublicKeyRetrieval=True;'
$env:BootstrapAdmin__Password='请替换为强密码'
dotnet run --project src/HTHIUM.Mes.Api --urls http://127.0.0.1:5180
```

仓库中的数据库密码仅为 `CHANGE_ME` 占位符。开发、测试和部署环境均应通过环境变量或安全配置中心提供真实连接字符串，不要把密码提交到Git。

默认管理员用户名为 `admin`。密码在数据库中通过ASP.NET Core PasswordHasher保存为不可逆哈希。管理员登录后可在“账号管理”页面新增管理员或操作员账号。

## 本地构建

```powershell
dotnet test HTHIUM.Mes.sln -c Release

cd HTHIUM.Mes.Web
pnpm install
pnpm build

cd ..
dotnet run --project src/HTHIUM.Mes.Api --urls http://127.0.0.1:5180
```

浏览器访问：`http://127.0.0.1:5180`

## Windows后台启动与日志

在项目根目录双击 `启动MES服务器.cmd` 后，API会在隐藏窗口中运行。启动脚本显示成功后可以安全关闭脚本窗口，服务不会随窗口关闭。需要停止时双击 `关闭MES服务器.cmd`，输入 `Y` 确认。

运行记录保存在项目根目录的 `logs` 文件夹：

```text
logs/mes-api-yyyyMMdd.log       API启动、请求异常、后台任务异常和致命退出
logs/mes-launcher-output.log    dotnet启动器标准输出
logs/mes-launcher-error.log     启动失败、编译失败等标准错误
```

API日志默认按天创建，单文件达到20MB后自动分卷，并清理30天以前的日志。参数可通过 `FileLogging` 配置节调整。进程被系统断电或强制终止时，操作系统可能来不及写入最后一条日志。

## 当前阶段限制

- 当前持久化设备最新状态和MES账号，完整心跳历史、审计日志和账号停用/重置密码功能属于后续阶段。
- `DeviceAccess:RequireApiKey` 在本机联调配置中暂未启用，正式局域网部署前必须配置每台设备凭据和HTTPS。
- 当前只实现在线设备监控，没有伪造报警、生产和质量接口。

## HTHIUM连接

局域网联调中，HTHIUM的 `CentralServer:BaseUrl` 指向当前MES主机 `http://172.20.10.2:5180`，心跳地址使用正式接口 `/api/v1/edge/devices/heartbeat`。MES默认监听 `http://0.0.0.0:5180`，浏览器和单机上位机应使用MES主机的实际局域网IP访问。当前地址来自WiFi DHCP，正式部署前应固定主机IP并同步更新此配置。
