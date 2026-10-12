import * as signalR from '@microsoft/signalr'

export interface ApiResponse<T> {
  success: boolean
  code: string
  message: string
  traceId: string
  serverTime: string
  data: T
}

export interface DeviceStatus {
  deviceCode: string
  deviceName: string
  lineCode: string
  deviceStatus: string
  plcConnected: boolean
  clientVersion: string
  deviceTime: string
  lastSeenTime: string
  isOnline: boolean
  heartbeatCount: number
}

export interface DeviceSummary {
  totalCount: number
  onlineCount: number
  offlineCount: number
  plcConnectedCount: number
  serverTime: string
}

async function getJson<T>(url: string): Promise<T> {
  const response = await fetch(url, {
    credentials: 'same-origin',
    headers: { Accept: 'application/json' },
  })
  if (response.status === 401) throw new Error('UNAUTHORIZED')
  if (!response.ok) throw new Error(`请求失败：HTTP ${response.status}`)

  const body = (await response.json()) as ApiResponse<T>
  if (!body.success) throw new Error(body.message || body.code)
  return body.data
}

export const deviceMonitoringApi = {
  getDevices: () => getJson<DeviceStatus[]>('/api/v1/mes/devices'),
  getSummary: () => getJson<DeviceSummary>('/api/v1/mes/devices/summary'),
  createConnection: () => new signalR.HubConnectionBuilder()
    .withUrl('/hubs/operations')
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .configureLogging(signalR.LogLevel.Warning)
    .build(),
}
