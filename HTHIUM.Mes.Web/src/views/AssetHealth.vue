<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { TrendCharts } from '@element-plus/icons-vue'
import { cylinderAssets, servoAssets, type MockAsset } from '../data/mockMes'

const props = defineProps<{ type: 'cylinder' | 'servo' }>()
const emit = defineEmits<{ openSnapshot: [asset: MockAsset] }>()
const selectedLine = ref('')
const selectedDevice = ref('')
const selectedCode = ref('')
const assets = computed(() => props.type === 'cylinder' ? cylinderAssets : servoAssets)
const lines = computed(() => [...new Set(assets.value.map(x => x.line))])
const devices = computed(() => [...new Set(assets.value.filter(x => x.line === selectedLine.value).map(x => x.device))])
const filteredAssets = computed(() => assets.value.filter(x => x.line === selectedLine.value && x.device === selectedDevice.value))
const selected = computed(() => filteredAssets.value.find(x => x.code === selectedCode.value))
const title = computed(() => props.type === 'cylinder' ? '气缸健康分析' : '伺服健康分析')
const subtitle = computed(() => props.type === 'cylinder' ? '监控动作时间、气压与到位信号，识别气缸性能劣化' : '监控负载、温度与跟随误差，识别伺服轴异常趋势')
function points(values: number[]) { const min = Math.min(...values); const max = Math.max(...values); return values.map((v, i) => `${i * 44},${70 - ((v - min) / Math.max(1, max - min)) * 55}`).join(' ') }
watch(selectedLine, () => { selectedDevice.value = ''; selectedCode.value = '' })
watch(selectedDevice, (device) => {
  selectedCode.value = device ? (filteredAssets.value[0]?.code ?? '') : ''
})
</script>

<template>
  <div class="prototype-view health-view">
    <section class="health-layout">
      <article class="panel asset-list-panel"><div class="section-heading"><div><h3>{{ title }}</h3><p>{{ subtitle }}</p></div></div>
        <div class="asset-selector"><div><label><b>1</b> 选择产线</label><el-select v-model="selectedLine" placeholder="请选择产线" clearable><el-option v-for="line in lines" :key="line" :label="line" :value="line" /></el-select></div><span>→</span><div><label><b>2</b> 选择设备</label><el-select v-model="selectedDevice" placeholder="请先选择产线" clearable :disabled="!selectedLine"><el-option v-for="device in devices" :key="device" :label="device" :value="device" /></el-select></div></div>
        <div v-if="!selectedLine || !selectedDevice" class="selection-empty"><div class="selection-illustration"><span>产线</span><i></i><span>设备</span><i></i><span>{{ type === 'cylinder' ? '气缸' : '伺服轴' }}</span></div><h4>请先选择产线和设备</h4><p>系统不会自动加载其他设备的数据，完成选择后才显示该设备下的{{ type === 'cylinder' ? '气缸' : '伺服轴' }}列表。</p></div>
        <template v-else><div class="asset-list-caption"><span>设备部件 / {{ filteredAssets.length }} 个</span><small>已加载首个部件，点击切换健康趋势</small></div><div class="asset-grid"><button v-for="asset in filteredAssets" :key="asset.code" type="button" :aria-pressed="selected?.code === asset.code" :class="['asset-card', asset.level, { selected: selected?.code === asset.code }]" @click="selectedCode = asset.code"><div class="asset-card-head"><b>{{ asset.code }}</b><span>{{ asset.health }}%</span></div><strong>{{ asset.name }}</strong><small>{{ asset.device }}</small><div class="health-bar"><i :style="{ width: `${asset.health}%` }"></i></div><div class="asset-meta"><span>{{ asset.status }}</span><em>{{ asset.alarmCount }} 次报警</em></div></button></div><el-empty v-if="filteredAssets.length === 0" description="该设备暂无监控部件" /></template>
      </article>
      <aside class="panel asset-detail-panel"><div v-if="!selected" class="curve-empty"><div><el-icon><TrendCharts /></el-icon></div><h4>等待设备选择</h4><p v-if="!selectedDevice">选择左侧产线和设备后，系统将自动加载该设备首个{{ type === 'cylinder' ? '气缸' : '伺服轴' }}的健康趋势。</p><p v-else>该设备暂时没有可用的监控部件数据。</p></div><template v-else><div class="detail-status"><span :class="selected.level"></span><div><small>当前趋势</small><h3>{{ selected.code }} {{ selected.name }}</h3><p>{{ selected.line }} / {{ selected.device }}</p></div><strong>{{ selected.health }}<em>%</em></strong></div>
        <div class="detail-metrics"><div><span>关键指标一</span><strong>{{ selected.metric1 }}</strong></div><div><span>关键指标二</span><strong>{{ selected.metric2 }}</strong></div></div>
        <div class="mini-trend"><div><h4>近7日健康趋势</h4><span>健康度</span></div><svg viewBox="0 0 264 80" preserveAspectRatio="none"><polyline :points="points(selected.trend)" fill="none" :stroke="selected.level === 'critical' ? '#ef4d5b' : selected.level === 'warning' ? '#f39c24' : '#20ae72'" stroke-width="3" /><line x1="0" y1="70" x2="264" y2="70" stroke="#e9edf4" /></svg><div class="trend-labels"><span>7天前</span><span>今天</span></div></div>
        <div class="diagnosis-box"><h4>诊断建议</h4><p v-if="selected.level === 'critical'">指标已明显偏离正常基线，建议停机检查机械阻力、供气/润滑状态及传感器信号。</p><p v-else-if="selected.level === 'warning'">检测到持续劣化趋势，建议纳入下一维护窗口并继续观察。</p><p v-else>当前参数处于正常基线范围，继续按计划巡检。</p></div>
        <el-button type="primary" plain @click="emit('openSnapshot', selected)">查看最近报警快照</el-button></template>
      </aside>
    </section>
  </div>
</template>
