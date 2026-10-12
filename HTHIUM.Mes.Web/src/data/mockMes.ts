export type HealthLevel = 'healthy' | 'warning' | 'critical'

export interface MockAlarm {
  id: string
  time: string
  line: string
  device: string
  component: string
  category: '气缸' | '伺服' | 'PLC' | '通讯'
  code: string
  name: string
  level: '高' | '中' | '低'
  duration: string
  status: '未处理' | '处理中' | '已恢复'
  snapshot: boolean
}

export interface MockAsset {
  code: string
  name: string
  line: string
  device: string
  health: number
  level: HealthLevel
  status: string
  metric1: string
  metric2: string
  alarmCount: number
  trend: number[]
}

export const overviewMetrics = [
  { label: '接入设备', value: '18', suffix: '台', hint: '16台在线', tone: 'blue' },
  { label: '运行设备', value: '13', suffix: '台', hint: '综合开机率 88.9%', tone: 'green' },
  { label: '当前报警', value: '6', suffix: '条', hint: '2条高优先级', tone: 'red' },
  { label: 'PLC异常', value: '1', suffix: '台', hint: 'PACK-07 通讯中断', tone: 'orange' },
  { label: '气缸预警', value: '8', suffix: '个', hint: '较昨日增加2个', tone: 'violet' },
  { label: '伺服预警', value: '3', suffix: '轴', hint: '1轴健康度低于70%', tone: 'cyan' },
]

export const lineStates = [
  { code: 'L1', name: '密封钉一线', devices: 6, running: 5, alarms: 1, rate: 92, state: '运行' },
  { code: 'L2', name: '密封钉二线', devices: 5, running: 4, alarms: 2, rate: 84, state: '运行' },
  { code: 'L3', name: '顶盖装配线', devices: 4, running: 3, alarms: 2, rate: 76, state: '预警' },
  { code: 'L4', name: '检测包装线', devices: 3, running: 1, alarms: 1, rate: 58, state: '换型' },
]

export const alarms: MockAlarm[] = [
  { id: 'ALM-20260920-0182', time: '09:42:18', line: 'L1 密封钉一线', device: '密封钉设备 01', component: 'CY-21', category: '气缸', code: 'HMI-CYL-021', name: '气缸伸出动作偏慢', level: '高', duration: '1m 42s', status: '未处理', snapshot: true },
  { id: 'ALM-20260920-0181', time: '09:38:05', line: 'L2 密封钉二线', device: '密封钉设备 03', component: 'SV-X3', category: '伺服', code: 'HMI-SV-203', name: '上料横轴跟随误差过大', level: '高', duration: '38s', status: '处理中', snapshot: true },
  { id: 'ALM-20260920-0179', time: '09:26:44', line: 'L3 顶盖装配线', device: '顶盖焊接机 02', component: 'CY-08', category: '气缸', code: 'HMI-CYL-008', name: '缩回到位超时', level: '中', duration: '12s', status: '已恢复', snapshot: true },
  { id: 'ALM-20260920-0176', time: '09:12:31', line: 'L4 检测包装线', device: '包装设备 07', component: 'PLC-01', category: '通讯', code: 'SYS-PLC-001', name: 'PLC通讯中断', level: '中', duration: '30m 07s', status: '未处理', snapshot: false },
  { id: 'ALM-20260920-0172', time: '08:54:17', line: 'L2 密封钉二线', device: '密封钉设备 04', component: 'SV-Z1', category: '伺服', code: 'HMI-SV-118', name: '下压轴负载率偏高', level: '中', duration: '22s', status: '已恢复', snapshot: true },
  { id: 'ALM-20260920-0168', time: '08:31:09', line: 'L1 密封钉一线', device: '密封钉设备 02', component: 'CY-15', category: '气缸', code: 'HMI-CYL-015', name: '工作气压低于下限', level: '低', duration: '8s', status: '已恢复', snapshot: true },
]

export const cylinderAssets: MockAsset[] = [
  { code: 'CY-21', name: '压紧气缸', line: 'L1 密封钉一线', device: '密封钉设备 01', health: 61, level: 'critical', status: '伸出偏慢', metric1: '1.42s 动作时间', metric2: '0.42MPa 气压', alarmCount: 12, trend: [92, 89, 86, 81, 76, 69, 61] },
  { code: 'CY-05', name: '上料阻挡气缸', line: 'L1 密封钉一线', device: '密封钉设备 01', health: 91, level: 'healthy', status: '运行正常', metric1: '0.41s 动作时间', metric2: '0.57MPa 气压', alarmCount: 1, trend: [93, 92, 92, 91, 92, 91, 91] },
  { code: 'CY-15', name: '推料气缸', line: 'L1 密封钉一线', device: '密封钉设备 02', health: 82, level: 'warning', status: '压力波动', metric1: '0.56s 动作时间', metric2: '0.48MPa 气压', alarmCount: 4, trend: [91, 90, 88, 86, 84, 83, 82] },
  { code: 'CY-03', name: '顶升气缸', line: 'L2 密封钉二线', device: '密封钉设备 03', health: 96, level: 'healthy', status: '运行正常', metric1: '0.44s 动作时间', metric2: '0.58MPa 气压', alarmCount: 0, trend: [95, 96, 95, 97, 96, 96, 96] },
  { code: 'CY-08', name: '定位气缸', line: 'L3 顶盖装配线', device: '顶盖焊接机 02', health: 74, level: 'warning', status: '偶发超时', metric1: '0.88s 动作时间', metric2: '0.51MPa 气压', alarmCount: 7, trend: [88, 86, 84, 80, 82, 77, 74] },
  { code: 'CY-11', name: '夹持气缸', line: 'L3 顶盖装配线', device: '顶盖焊接机 01', health: 93, level: 'healthy', status: '运行正常', metric1: '0.38s 动作时间', metric2: '0.56MPa 气压', alarmCount: 1, trend: [92, 93, 94, 93, 94, 93, 93] },
]

export const servoAssets: MockAsset[] = [
  { code: 'SV-Y2', name: '移载轴', line: 'L1 密封钉一线', device: '密封钉设备 01', health: 81, level: 'warning', status: '温升偏快', metric1: '64% 峰值负载', metric2: '51.8°C 温度', alarmCount: 3, trend: [90, 89, 87, 86, 84, 82, 81] },
  { code: 'SV-X1', name: '进料轴', line: 'L1 密封钉一线', device: '密封钉设备 01', health: 95, level: 'healthy', status: '运行正常', metric1: '38% 峰值负载', metric2: '0.009mm 跟随误差', alarmCount: 0, trend: [94, 95, 94, 95, 96, 95, 95] },
  { code: 'SV-X3', name: '上料横移轴', line: 'L2 密封钉二线', device: '密封钉设备 03', health: 58, level: 'critical', status: '跟随误差偏高', metric1: '86% 峰值负载', metric2: '0.036mm 跟随误差', alarmCount: 9, trend: [90, 86, 82, 77, 71, 65, 58] },
  { code: 'SV-Z1', name: '压装轴', line: 'L2 密封钉二线', device: '密封钉设备 04', health: 72, level: 'warning', status: '负载率偏高', metric1: '78% 峰值负载', metric2: '45.1°C 温度', alarmCount: 6, trend: [85, 83, 80, 78, 76, 73, 72] },
  { code: 'SV-R1', name: '旋转轴', line: 'L3 顶盖装配线', device: '顶盖焊接机 02', health: 94, level: 'healthy', status: '运行正常', metric1: '42% 峰值负载', metric2: '0.012mm 跟随误差', alarmCount: 0, trend: [93, 94, 95, 94, 93, 94, 94] },
]

export const snapshotParameters = [
  { name: 'CY-21 当前状态', unit: '', before: '1', trigger: '3', after: '2', abnormal: true, color: '#ef4d5b', values: [1,1,1,1,2,3,3,3,2,2,2] },
  { name: 'CY-21 当前气压', unit: 'MPa', before: '0.56', trigger: '0.38', after: '0.51', abnormal: true, color: '#f39c24', values: [56,55,56,53,48,38,39,42,47,49,51] },
  { name: 'CY-21 单次动作时间', unit: 's', before: '0.54', trigger: '1.39', after: '0.66', abnormal: true, color: '#7a59e8', values: [54,55,53,56,68,139,122,104,82,71,66] },
  { name: 'CY-21 伸出到位信号', unit: '', before: '是', trigger: '否', after: '是', abnormal: true, color: '#356dff', values: [1,1,1,1,1,0,0,0,1,1,1] },
  { name: 'CY-21 缩回到位信号', unit: '', before: '否', trigger: '否', after: '否', abnormal: false, color: '#20ae72', values: [0,0,0,0,0,0,0,0,0,0,0] },
]

export const dataQualityRows = [
  { device: '密封钉设备 01', code: '01', heartbeat: '1秒前', latency: 38, loss: 0, clock: 12, pending: 0, quality: 'Good' },
  { device: '密封钉设备 02', code: '02', heartbeat: '2秒前', latency: 51, loss: 0.02, clock: 18, pending: 0, quality: 'Good' },
  { device: '密封钉设备 03', code: '03', heartbeat: '1秒前', latency: 64, loss: 0.08, clock: 32, pending: 2, quality: 'Good' },
  { device: '顶盖焊接机 02', code: 'CAP-02', heartbeat: '6秒前', latency: 186, loss: 1.24, clock: 86, pending: 18, quality: 'Warning' },
  { device: '包装设备 07', code: 'PACK-07', heartbeat: '30分钟前', latency: 0, loss: 100, clock: 0, pending: 346, quality: 'Bad' },
]
