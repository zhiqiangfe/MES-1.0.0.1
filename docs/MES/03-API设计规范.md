# API 设计规范

## 1. 基本规则

- 基础路径：`/api/v1`。
- 接口由“HTTP方法 + 路由模板”唯一识别，不依赖C#方法名称。
- JSON字段采用camelCase。
- 时间使用ISO 8601并携带时区。
- 查询接口必须分页，不提供无上限全表返回。
- 破坏兼容性的协议变化升级主版本；新增可选字段不升级主版本。

## 2. 路由分区

### 设备接入

```text
/api/v1/edge/devices/heartbeat
/api/v1/edge/events
/api/v1/edge/alarms
/api/v1/edge/production-records
/api/v1/edge/quality-records
/api/v1/edge/plc-snapshots
/api/v1/edge/messages/batch
```

### MES业务查询

```text
/api/v1/mes/devices
/api/v1/mes/alarms
/api/v1/mes/production-records
/api/v1/mes/quality-records
/api/v1/mes/plc-snapshots
/api/v1/mes/dashboard
```

### 系统管理

```text
/api/v1/admin/sites
/api/v1/admin/lines
/api/v1/admin/devices
/api/v1/admin/users
/api/v1/admin/roles
/api/v1/admin/audit-logs
```

### 基础设施

```text
/health/live
/health/ready
/openapi/v1.json
/hubs/operations
```

## 3. HTTP方法语义

- GET：只查询，不改变业务状态。
- POST：创建记录、执行接收命令或复杂查询。
- PUT：完整替换可寻址资源。
- PATCH：部分更新资源。
- DELETE：软删除或禁用配置；生产记录通常禁止物理删除。

## 4. 统一成功响应

接入接口：

```json
{
  "success": true,
  "code": "ACCEPTED",
  "message": "消息已可靠接收",
  "traceId": "00-...",
  "serverTime": "2026-09-18T10:31:00.100+08:00",
  "data": {
    "messageId": "...",
    "duplicate": false
  }
}
```

分页查询：

```json
{
  "success": true,
  "code": "OK",
  "traceId": "00-...",
  "data": {
    "items": [],
    "pageIndex": 1,
    "pageSize": 50,
    "totalCount": 0,
    "totalPages": 0
  }
}
```

## 5. 统一错误响应

采用 Problem Details 思路，至少包含：

```json
{
  "type": "https://mes/errors/validation",
  "title": "请求参数错误",
  "status": 400,
  "code": "VALIDATION_FAILED",
  "traceId": "00-...",
  "errors": {
    "deviceCode": ["设备编码不能为空"]
  }
}
```

错误码应稳定，前端和HTHIUM根据 `code` 处理，不能依赖中文message进行流程判断。

## 6. 状态码约定

| 状态码 | 用途 |
|---:|---|
| 200 | 查询成功、幂等重复但已处理 |
| 201 | 创建资源成功 |
| 202 | 已可靠接收，后续异步处理 |
| 400 | DTO或业务字段不合法 |
| 401 | 未认证或设备凭据错误 |
| 403 | 已认证但无权限/设备被禁用 |
| 404 | 资源不存在 |
| 409 | 状态冲突，且客户端需要调整请求 |
| 413 | 请求体或快照过大 |
| 429 | 限流或服务端处理队列已满 |
| 500 | 未处理异常 |
| 503 | 数据库或关键依赖未就绪 |

## 7. 分页、筛选和排序

示例：

```text
GET /api/v1/mes/alarms?pageIndex=1&pageSize=50
    &deviceCode=SEALPIN-01
    &startTime=2026-09-18T00:00:00%2B08:00
    &endTime=2026-09-18T23:59:59%2B08:00
    &status=Active
    &sort=-occurredAt
```

规则：

- `pageSize` 默认50，最大500。
- 只允许白名单排序字段，防止任意SQL字段注入。
- 起止时间必须同时说明时区。
- 大数据导出采用异步导出任务，不通过普通查询一次返回全部。

## 8. Controller组织方式

```text
EdgeDeviceController
EdgeEventController
EdgeAlarmController
EdgeProductionController
MesDeviceQueryController
MesAlarmQueryController
AdminDeviceController
```

Controller只负责协议层工作：接收DTO、认证上下文、调用应用服务、映射响应。事务、幂等和业务规则放在Application/Domain层。

## 9. OpenAPI要求

- 每个接口声明摘要、用途、认证方式、请求示例和主要响应码。
- 构建阶段生成OpenAPI JSON并纳入版本产物。
- 前端从OpenAPI生成TypeScript客户端。
- HTHIUM可从OpenAPI生成或校验.NET客户端契约。
- 协议变更在合并代码前比较OpenAPI差异。

## 10. 兼容策略

当前原型接口：

```text
/api/v1/devices/heartbeat
/api/v1/events
```

迁移期可保留兼容端点，但内部必须转换到新Application用例。响应头或日志标记弃用，所有HTHIUM升级完成后再移除。

