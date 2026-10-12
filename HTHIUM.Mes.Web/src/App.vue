<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { Bell, Camera, Connection, Cpu, DataAnalysis, Delete, Histogram, HomeFilled, Lock, Monitor, Plus, Refresh, Search, SwitchButton, TrendCharts, User } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { HubConnection } from '@microsoft/signalr'
import { deviceMonitoringApi, type DeviceStatus, type DeviceSummary } from './api/deviceMonitoring'
import { identityApi, type CreateUserRequest, type CurrentUser, type UserAccount } from './api/identity'
import MesOverview from './views/MesOverview.vue'
import AlarmCenter from './views/AlarmCenter.vue'
import AssetHealth from './views/AssetHealth.vue'
import SnapshotCenter from './views/SnapshotCenter.vue'
import DataQuality from './views/DataQuality.vue'

type ViewName = 'overview' | 'devices' | 'alarms' | 'cylinders' | 'servo' | 'snapshots' | 'quality' | 'users'

const currentUser = ref<CurrentUser | null>(null)
const sessionReady = ref(false)
const loginLoading = ref(false)
const loginForm = reactive({ username: 'admin', password: '' })
const currentView = ref<ViewName>('overview')
const users = ref<UserAccount[]>([])
const userDialogVisible = ref(false)
const createUserLoading = ref(false)
const createUserForm = reactive<CreateUserRequest>({ username: '', displayName: '', password: '', role: 'Operator' })

const devices = ref<DeviceStatus[]>([])
const summary = ref<DeviceSummary>({ totalCount: 0, onlineCount: 0, offlineCount: 0, plcConnectedCount: 0, serverTime: new Date().toISOString() })
const loading = ref(false)
const keyword = ref('')
const statusFilter = ref<'all' | 'online' | 'offline'>('all')
const realtimeConnected = ref(false)
const lastRefreshTime = ref<Date | null>(null)
const clock = ref(Date.now())
// 第一版原型固定展示模拟数据；接入真实MES接口时只需切换为 false。
const prototypeMode = true
let hubConnection: HubConnection | null = null
let refreshTimer: number | undefined
let clockTimer: number | undefined

const isAdministrator = computed(() => currentUser.value?.role === 'Administrator')
const mockDevices: DeviceStatus[] = [
  { deviceCode: '01', deviceName: '密封钉设备 01', lineCode: 'L1 密封钉一线', deviceStatus: 'Running', plcConnected: true, clientVersion: '2.1.0', deviceTime: new Date().toISOString(), lastSeenTime: new Date().toISOString(), isOnline: true, heartbeatCount: 18246 },
  { deviceCode: '02', deviceName: '密封钉设备 02', lineCode: 'L1 密封钉一线', deviceStatus: 'Running', plcConnected: true, clientVersion: '2.1.0', deviceTime: new Date().toISOString(), lastSeenTime: new Date(Date.now() - 2000).toISOString(), isOnline: true, heartbeatCount: 17982 },
  { deviceCode: '03', deviceName: '密封钉设备 03', lineCode: 'L2 密封钉二线', deviceStatus: 'Warning', plcConnected: true, clientVersion: '2.0.9', deviceTime: new Date().toISOString(), lastSeenTime: new Date(Date.now() - 1000).toISOString(), isOnline: true, heartbeatCount: 16420 },
  { deviceCode: 'CAP-02', deviceName: '顶盖焊接机 02', lineCode: 'L3 顶盖装配线', deviceStatus: 'Running', plcConnected: true, clientVersion: '2.0.8', deviceTime: new Date().toISOString(), lastSeenTime: new Date(Date.now() - 6000).toISOString(), isOnline: true, heartbeatCount: 15108 },
  { deviceCode: 'PACK-07', deviceName: '包装设备 07', lineCode: 'L4 检测包装线', deviceStatus: 'Offline', plcConnected: false, clientVersion: '1.9.6', deviceTime: new Date().toISOString(), lastSeenTime: new Date(Date.now() - 1800000).toISOString(), isOnline: false, heartbeatCount: 12886 },
]
const filteredDevices = computed(() => {
  const searchText = keyword.value.trim().toLocaleLowerCase()
  const source = prototypeMode ? mockDevices : devices.value
  return source.filter((device) => {
    const statusMatched = statusFilter.value === 'all'
      || (statusFilter.value === 'online' && device.isOnline)
      || (statusFilter.value === 'offline' && !device.isOnline)
    const keywordMatched = !searchText || [device.deviceCode, device.deviceName, device.lineCode, device.clientVersion]
      .some(value => value.toLocaleLowerCase().includes(searchText))
    return statusMatched && keywordMatched
  })
})
const effectiveSummary = computed<DeviceSummary>(() => prototypeMode ? { totalCount: 18, onlineCount: 16, offlineCount: 2, plcConnectedCount: 15, serverTime: new Date().toISOString() } : summary.value)
const onlineRate = computed(() => effectiveSummary.value.totalCount === 0 ? 0 : Math.round(effectiveSummary.value.onlineCount * 100 / effectiveSummary.value.totalCount))
const viewMeta: Record<ViewName, { title: string; subtitle: string }> = {
  overview: { title: '生产运行总览', subtitle: '全厂设备、报警与关键部件健康态势' },
  devices: { title: '在线设备监控', subtitle: '集中查看设备心跳、PLC连接和运行状态' },
  alarms: { title: '报警事件中心', subtitle: '跨设备报警聚合、检索与证据追溯' },
  cylinders: { title: '气缸健康分析', subtitle: '动作时间、气压和到位信号劣化监控' },
  servo: { title: '伺服健康分析', subtitle: '负载、温度和跟随误差趋势监控' },
  snapshots: { title: 'PLC报警快照', subtitle: '回放报警前后过程数据并形成证据链' },
  quality: { title: '数据质量中心', subtitle: '监测采集延迟、丢帧、断线与补传状态' },
  users: { title: '账号管理', subtitle: '创建和管理设备监控系统登录账号' },
}

async function login() {
  if (!loginForm.username || !loginForm.password) {
    ElMessage.warning('请输入用户名和密码')
    return
  }
  loginLoading.value = true
  try {
    currentUser.value = await identityApi.login(loginForm.username, loginForm.password)
    loginForm.password = ''
    await startAuthenticatedSession()
  } catch (error) {
    ElMessage.error(error instanceof Error && error.message !== 'UNAUTHORIZED' ? error.message : '用户名或密码错误')
  } finally {
    loginLoading.value = false
  }
}

async function logout() {
  try { await identityApi.logout() } catch { /* 会话可能已经过期 */ }
  if (hubConnection) await hubConnection.stop()
  hubConnection = null
  realtimeConnected.value = false
  currentUser.value = null
  devices.value = []
  users.value = []
  currentView.value = 'overview'
}

async function startAuthenticatedSession() {
  await refresh()
  await connectRealtime()
}

async function refresh(showError = true) {
  loading.value = true
  try {
    const [deviceList, monitorSummary] = await Promise.all([deviceMonitoringApi.getDevices(), deviceMonitoringApi.getSummary()])
    devices.value = deviceList
    summary.value = monitorSummary
    lastRefreshTime.value = new Date()
  } catch (error) {
    if (error instanceof Error && error.message === 'UNAUTHORIZED') {
      await logout()
    } else if (showError) {
      ElMessage.error(error instanceof Error ? error.message : '设备状态加载失败')
    }
  } finally { loading.value = false }
}

async function connectRealtime() {
  if (hubConnection) await hubConnection.stop()
  hubConnection = deviceMonitoringApi.createConnection()
  hubConnection.on('deviceStatusChanged', () => refresh(false))
  hubConnection.onreconnecting(() => { realtimeConnected.value = false })
  hubConnection.onreconnected(() => { realtimeConnected.value = true; refresh(false) })
  hubConnection.onclose(() => { realtimeConnected.value = false })
  try { await hubConnection.start(); realtimeConnected.value = true } catch { realtimeConnected.value = false }
}

async function showUsers() {
  if (!isAdministrator.value) return
  currentView.value = 'users'
  try { users.value = await identityApi.getUsers() }
  catch (error) { ElMessage.error(error instanceof Error ? error.message : '账号加载失败') }
}

function openCreateUser() {
  Object.assign(createUserForm, { username: '', displayName: '', password: '', role: 'Operator' })
  userDialogVisible.value = true
}

async function createUser() {
  createUserLoading.value = true
  try {
    await identityApi.createUser(createUserForm)
    users.value = await identityApi.getUsers()
    userDialogVisible.value = false
    ElMessage.success('账号创建成功')
  } catch (error) {
    ElMessage.error(error instanceof Error ? error.message : '账号创建失败')
  } finally { createUserLoading.value = false }
}

async function deleteUser(user: UserAccount) {
  if (user.username === currentUser.value?.username) {
    ElMessage.warning('不能删除当前正在登录的账号')
    return
  }

  try {
    await ElMessageBox.confirm(
      `确定删除账号“${user.displayName}（${user.username}）”吗？删除后该账号将无法登录。`,
      '删除账号',
      { confirmButtonText: '确认删除', cancelButtonText: '取消', type: 'warning' },
    )
    await identityApi.deleteUser(user.id)
    users.value = await identityApi.getUsers()
    ElMessage.success('账号已删除')
  } catch (error) {
    if (error === 'cancel' || error === 'close') return
    ElMessage.error(error instanceof Error ? error.message : '账号删除失败')
  }
}

function formatDateTime(value?: string | null) {
  if (!value) return '-'
  const time = new Date(value)
  return Number.isNaN(time.getTime()) ? '-' : time.toLocaleString('zh-CN', { hour12: false })
}
function heartbeatAge(value: string) {
  const seconds = Math.max(0, Math.floor((clock.value - new Date(value).getTime()) / 1000))
  if (!Number.isFinite(seconds)) return '-'
  if (seconds < 60) return `${seconds} 秒前`
  const minutes = Math.floor(seconds / 60)
  return minutes < 60 ? `${minutes} 分钟前` : `${Math.floor(minutes / 60)} 小时前`
}
function deviceStatusText(device: DeviceStatus) {
  if (!device.isOnline) return '离线'
  if (device.deviceStatus.toLowerCase() === 'running') return '运行中'
  if (device.deviceStatus.toLowerCase() === 'disconnected') return '通讯异常'
  return device.deviceStatus || '未知'
}

onMounted(async () => {
  try { currentUser.value = await identityApi.getSession(); await startAuthenticatedSession() }
  catch { currentUser.value = null }
  finally { sessionReady.value = true }
  refreshTimer = window.setInterval(() => { if (currentUser.value) refresh(false) }, 10000)
  clockTimer = window.setInterval(() => { clock.value = Date.now() }, 1000)
})
onBeforeUnmount(async () => {
  if (refreshTimer) window.clearInterval(refreshTimer)
  if (clockTimer) window.clearInterval(clockTimer)
  if (hubConnection) await hubConnection.stop()
})
</script>

<template>
  <div v-if="!sessionReady" class="session-loading"><div class="brand-mark">H</div><span>正在连接 HTHIUM...</span></div>

  <main v-else-if="!currentUser" class="login-page">
    <section class="login-brand-panel">
      <div class="login-brand"><div class="brand-mark large">H</div><div><strong>HTHIUM</strong><span>设备监控系统</span></div></div>
      <div class="login-copy"><span class="eyebrow">制造运行控制台</span><h1>设备状态，一屏掌握</h1><p>连接单机上位机，集中查看设备在线、PLC 通讯、报警与关键部件健康状态。</p></div>
      <div class="login-feature-grid"><div><Connection /><span><strong>实时连接</strong><small>SignalR状态推送</small></span></div><div><Lock /><span><strong>安全访问</strong><small>账号和权限隔离</small></span></div></div>
      <footer>HTHIUM / EQUIPMENT MONITORING PLATFORM</footer>
    </section>
    <section class="login-form-panel">
      <div class="login-card">
        <div class="login-icon"><el-icon><User /></el-icon></div>
        <h2>欢迎登录</h2><p>请输入设备监控系统账号进入运行中心</p>
        <el-form label-position="top" @submit.prevent="login">
          <el-form-item label="用户名"><el-input v-model="loginForm.username" size="large" :prefix-icon="User" placeholder="请输入用户名" @keyup.enter="login" /></el-form-item>
          <el-form-item label="密码"><el-input v-model="loginForm.password" size="large" :prefix-icon="Lock" type="password" show-password placeholder="请输入密码" @keyup.enter="login" /></el-form-item>
          <el-button class="login-button" type="primary" size="large" :loading="loginLoading" @click="login">登录系统</el-button>
        </el-form>
        <div class="login-security"><el-icon><Lock /></el-icon><span>登录会话使用 HttpOnly Cookie 保护</span></div>
      </div>
    </section>
  </main>

  <el-container v-else class="app-shell">
    <el-aside class="sidebar" width="224px">
      <div class="brand"><div class="brand-mark">H</div><div><strong>HTHIUM</strong><span>设备监控系统</span></div></div>
      <nav class="navigation">
        <div class="nav-section">运行中心</div>
        <button type="button" :aria-current="currentView === 'overview' ? 'page' : undefined" :class="['nav-item', { active: currentView === 'overview' }]" @click="currentView = 'overview'"><el-icon><HomeFilled /></el-icon><span>生产运行总览</span><i></i></button>
        <button type="button" :aria-current="currentView === 'devices' ? 'page' : undefined" :class="['nav-item', { active: currentView === 'devices' }]" @click="currentView = 'devices'"><el-icon><Monitor /></el-icon><span>在线设备监控</span><i></i></button>
        <button type="button" :aria-current="currentView === 'alarms' ? 'page' : undefined" :class="['nav-item', { active: currentView === 'alarms' }]" @click="currentView = 'alarms'"><el-icon><Bell /></el-icon><span>报警事件中心</span><em>6</em><i></i></button>
        <div class="nav-section nav-section-spaced">健康分析</div>
        <button type="button" :aria-current="currentView === 'cylinders' ? 'page' : undefined" :class="['nav-item', { active: currentView === 'cylinders' }]" @click="currentView = 'cylinders'"><el-icon><Histogram /></el-icon><span>气缸健康分析</span><i></i></button>
        <button type="button" :aria-current="currentView === 'servo' ? 'page' : undefined" :class="['nav-item', { active: currentView === 'servo' }]" @click="currentView = 'servo'"><el-icon><TrendCharts /></el-icon><span>伺服健康分析</span><i></i></button>
        <button type="button" :aria-current="currentView === 'snapshots' ? 'page' : undefined" :class="['nav-item', { active: currentView === 'snapshots' }]" @click="currentView = 'snapshots'"><el-icon><Camera /></el-icon><span>PLC报警快照</span><i></i></button>
        <button type="button" :aria-current="currentView === 'quality' ? 'page' : undefined" :class="['nav-item', { active: currentView === 'quality' }]" @click="currentView = 'quality'"><el-icon><DataAnalysis /></el-icon><span>数据质量中心</span><i></i></button>
        <div class="nav-section nav-section-spaced">系统配置</div>
        <button v-if="isAdministrator" type="button" :aria-current="currentView === 'users' ? 'page' : undefined" :class="['nav-item', { active: currentView === 'users' }]" @click="showUsers"><el-icon><User /></el-icon><span>账号管理</span><i></i></button>
      </nav>
      <div class="sidebar-footer"><span :class="['connection-dot', { online: realtimeConnected }]"></span><div><strong>{{ realtimeConnected ? '实时通道已连接' : '实时通道重连中' }}</strong><small>SignalR 状态推送</small></div></div>
    </el-aside>

    <el-container>
      <el-header class="topbar" height="72px">
        <div><h1>{{ viewMeta[currentView].title }}</h1><p>{{ viewMeta[currentView].subtitle }}</p></div>
        <div class="topbar-right">
          <template v-if="currentView === 'devices'"><div class="refresh-time"><span>最后刷新</span><strong>{{ lastRefreshTime?.toLocaleTimeString('zh-CN', { hour12: false }) || '-' }}</strong></div><el-button :icon="Refresh" :loading="loading" @click="refresh()">刷新</el-button></template>
          <div v-else-if="currentView !== 'users'" class="simulation-badge">演示数据</div><div class="system-badge"><span></span>监控服务正常</div>
          <div class="current-user"><div class="user-avatar">{{ currentUser.displayName.slice(0, 1) }}</div><div><strong>{{ currentUser.displayName }}</strong><span>{{ currentUser.role === 'Administrator' ? '系统管理员' : '操作员' }}</span></div></div>
          <el-button :icon="SwitchButton" text @click="logout">退出</el-button>
        </div>
      </el-header>

      <el-main v-if="currentView === 'overview'" class="content prototype-content"><MesOverview @navigate="view => currentView = view as ViewName" /></el-main>

      <el-main v-else-if="currentView === 'devices'" class="content">
        <section class="metric-grid">
          <article class="metric-card blue"><div><span>设备总数</span><strong>{{ effectiveSummary.totalCount }}</strong><small>已登记接入设备</small></div><el-icon><Monitor /></el-icon></article>
          <article class="metric-card green"><div><span>在线设备</span><strong>{{ effectiveSummary.onlineCount }}</strong><small>15秒内收到心跳</small></div><el-icon><Connection /></el-icon></article>
          <article class="metric-card violet"><div><span>PLC 已连接</span><strong>{{ effectiveSummary.plcConnectedCount }}</strong><small>在线设备通讯状态</small></div><el-icon><Cpu /></el-icon></article>
          <article class="metric-card orange"><div><span>离线设备</span><strong>{{ effectiveSummary.offlineCount }}</strong><small>需要检查网络或程序</small></div><el-icon><Monitor /></el-icon></article>
        </section>
        <section class="overview-grid">
          <article class="panel availability-panel"><div class="panel-title"><div><h2>设备可用概况</h2><p>当前在线设备占全部设备比例</p></div><strong>{{ onlineRate }}%</strong></div><el-progress :percentage="onlineRate" :stroke-width="8" :show-text="false" color="#1f9d68" /><div class="availability-legend"><span><i class="online"></i>在线 {{ effectiveSummary.onlineCount }}</span><span><i class="offline"></i>离线 {{ effectiveSummary.offlineCount }}</span></div></article>
          <article class="panel server-panel"><div class="server-icon"><el-icon><Connection /></el-icon></div><div><span>中心服务时间</span><strong>{{ formatDateTime(effectiveSummary.serverTime) }}</strong><small>API / SignalR / MySQL</small></div><div class="server-state">运行中</div></article>
        </section>
        <section class="panel device-panel">
          <div class="device-panel-header"><div><h2>设备连接明细</h2><p>心跳变化通过实时通道自动刷新，并每10秒补偿查询</p></div><div class="filters"><el-input v-model="keyword" :prefix-icon="Search" clearable placeholder="搜索设备、产线或版本" /><el-select v-model="statusFilter" style="width:128px"><el-option label="全部状态" value="all" /><el-option label="仅在线" value="online" /><el-option label="仅离线" value="offline" /></el-select></div></div>
          <el-table v-loading="loading" :data="filteredDevices" height="calc(100vh - 500px)" empty-text="尚未收到设备心跳">
            <el-table-column label="连接" width="90"><template #default="{ row }"><el-tag :type="row.isOnline ? 'success' : 'info'" effect="light" round>{{ row.isOnline ? '在线' : '离线' }}</el-tag></template></el-table-column>
            <el-table-column prop="deviceCode" label="设备编码" min-width="145" /><el-table-column prop="deviceName" label="设备名称" min-width="180" /><el-table-column prop="lineCode" label="所属产线" min-width="130" />
            <el-table-column label="运行状态" min-width="120"><template #default="{ row }"><span :class="['run-state', { active: row.isOnline && row.deviceStatus.toLowerCase() === 'running' }]">{{ deviceStatusText(row) }}</span></template></el-table-column>
            <el-table-column label="PLC" width="110"><template #default="{ row }"><span :class="['plc-state', { connected: row.isOnline && row.plcConnected }]"><i></i>{{ row.isOnline && row.plcConnected ? '已连接' : '已断开' }}</span></template></el-table-column>
            <el-table-column prop="clientVersion" label="客户端版本" min-width="125" /><el-table-column label="最后心跳" min-width="190"><template #default="{ row }"><div class="heartbeat-time"><strong>{{ heartbeatAge(row.lastSeenTime) }}</strong><span>{{ formatDateTime(row.lastSeenTime) }}</span></div></template></el-table-column><el-table-column prop="heartbeatCount" label="心跳次数" width="100" align="right" />
          </el-table>
        </section>
      </el-main>

      <el-main v-else-if="currentView === 'alarms'" class="content prototype-content"><AlarmCenter @open-snapshot="() => currentView = 'snapshots'" /></el-main>
      <el-main v-else-if="currentView === 'cylinders'" class="content prototype-content"><AssetHealth type="cylinder" @open-snapshot="() => currentView = 'snapshots'" /></el-main>
      <el-main v-else-if="currentView === 'servo'" class="content prototype-content"><AssetHealth type="servo" @open-snapshot="() => currentView = 'snapshots'" /></el-main>
      <el-main v-else-if="currentView === 'snapshots'" class="content prototype-content"><SnapshotCenter /></el-main>
      <el-main v-else-if="currentView === 'quality'" class="content prototype-content"><DataQuality /></el-main>

      <el-main v-else-if="currentView === 'users'" class="content users-content">
        <section class="panel users-panel"><div class="device-panel-header"><div><h2>系统账号</h2><p>账号密码以不可逆哈希方式保存在独立MES数据库</p></div><el-button type="primary" :icon="Plus" @click="openCreateUser">添加账号</el-button></div>
          <el-table :data="users"><el-table-column prop="username" label="用户名" min-width="150" /><el-table-column prop="displayName" label="显示名称" min-width="170" /><el-table-column label="角色" width="140"><template #default="{ row }"><el-tag :type="row.role === 'Administrator' ? 'danger' : 'primary'" effect="light">{{ row.role === 'Administrator' ? '管理员' : '操作员' }}</el-tag></template></el-table-column><el-table-column label="状态" width="100"><template #default="{ row }"><el-tag :type="row.isEnabled ? 'success' : 'info'">{{ row.isEnabled ? '启用' : '停用' }}</el-tag></template></el-table-column><el-table-column label="创建时间" min-width="180"><template #default="{ row }">{{ formatDateTime(row.createdAt) }}</template></el-table-column><el-table-column label="最后登录" min-width="180"><template #default="{ row }">{{ formatDateTime(row.lastLoginAt) }}</template></el-table-column><el-table-column label="操作" width="110" fixed="right"><template #default="{ row }"><el-button type="danger" link :icon="Delete" :disabled="row.username === currentUser?.username" @click="deleteUser(row)">删除</el-button></template></el-table-column></el-table>
        </section>
      </el-main>
    </el-container>

    <el-dialog v-model="userDialogVisible" title="添加MES账号" width="480px" destroy-on-close><el-form label-position="top"><el-form-item label="用户名"><el-input v-model="createUserForm.username" placeholder="3到64个字符" /></el-form-item><el-form-item label="显示名称"><el-input v-model="createUserForm.displayName" placeholder="例如：设备工程师" /></el-form-item><el-form-item label="初始密码"><el-input v-model="createUserForm.password" type="password" show-password placeholder="至少8位，包含大小写字母和数字" /></el-form-item><el-form-item label="角色"><el-select v-model="createUserForm.role" style="width:100%"><el-option label="操作员" value="Operator" /><el-option label="管理员" value="Administrator" /></el-select></el-form-item></el-form><template #footer><el-button @click="userDialogVisible=false">取消</el-button><el-button type="primary" :loading="createUserLoading" @click="createUser">创建账号</el-button></template></el-dialog>
  </el-container>
</template>
