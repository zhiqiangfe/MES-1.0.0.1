<script setup lang="ts">
import { computed, ref } from 'vue'
import { alarms, snapshotParameters } from '../data/mockMes'

const selectedAlarm = ref(alarms.find(a => a.snapshot)?.id ?? '')
const selectedParameter = ref(0)
const snapshotAlarms = alarms.filter(a => a.snapshot)
const activeAlarm = computed(() => snapshotAlarms.find(a => a.id === selectedAlarm.value) ?? snapshotAlarms[0])
const activeParameter = computed(() => snapshotParameters[selectedParameter.value])
function curvePoints(values: number[]) { const min = Math.min(...values); const max = Math.max(...values); return values.map((v, i) => `${20 + i * 66},${130 - ((v - min) / Math.max(1, max - min)) * 90}`).join(' ') }
</script>

<template>
  <div class="prototype-view snapshot-view">
    <section class="snapshot-layout">
      <aside class="panel snapshot-events"><div class="section-heading"><div><h3>报警事件索引</h3><p>选择事件加载快照证据</p></div></div><button v-for="alarm in snapshotAlarms" :key="alarm.id" type="button" :aria-pressed="selectedAlarm === alarm.id" :class="['snapshot-event', { active: selectedAlarm === alarm.id }]" @click="selectedAlarm = alarm.id"><div><span :class="alarm.level === '高' ? 'high' : 'medium'"></span><time>{{ alarm.time }}</time><el-tag size="small" :type="alarm.level === '高' ? 'danger' : 'warning'">{{ alarm.level }}</el-tag></div><strong>{{ alarm.component }} / {{ alarm.name }}</strong><small>{{ alarm.device }} / 21帧</small></button></aside>
      <main class="snapshot-main">
        <section class="panel snapshot-info"><div><span>产线</span><strong>{{ activeAlarm?.line }}</strong></div><div><span>设备</span><strong>{{ activeAlarm?.device }}</strong></div><div><span>报警</span><strong class="red">{{ activeAlarm?.code }}</strong></div><div><span>触发时间</span><strong>2026-09-20 {{ activeAlarm?.time }}.420</strong></div><div><span>采样窗口</span><strong>-1s ～ +1s</strong></div><el-tag type="success" effect="light">数据质量 Good</el-tag></section>
        <section class="panel snapshot-chart"><div class="section-heading"><div><h3>参数帧曲线</h3><p>{{ activeParameter?.name }} / 100ms 每帧</p></div><span class="simulation-badge">报警触发点 0ms</span></div><svg role="img" :aria-label="`${activeParameter?.name ?? '参数'}报警前后帧曲线`" viewBox="0 0 700 170" preserveAspectRatio="none"><line v-for="y in [40,85,130]" :key="y" x1="20" :y1="y" x2="680" :y2="y" stroke="#edf0f5" /><line x1="350" y1="20" x2="350" y2="145" stroke="#ef4d5b" stroke-dasharray="5 5" /><polyline :points="curvePoints(activeParameter?.values ?? [])" fill="none" :stroke="activeParameter?.color" stroke-width="4" stroke-linecap="round" stroke-linejoin="round" /></svg><div class="snapshot-axis"><span>-1s</span><span>-500ms</span><b>0ms 报警</b><span>+500ms</span><span>+1s</span></div></section>
        <section class="panel snapshot-parameters"><div class="section-heading"><div><h3>PLC点位冻结对比</h3><p>点击参数切换上方完整帧曲线</p></div><span class="abnormal-count">异常 4 / 总点位 5</span></div><div class="parameter-table"><div class="parameter-row header"><span>参数名称</span><span>报警前</span><span>触发时</span><span>报警后</span><span>变化判断</span></div><button v-for="(parameter, index) in snapshotParameters" :key="parameter.name" type="button" :aria-pressed="selectedParameter === index" :class="['parameter-row', { active: selectedParameter === index }]" @click="selectedParameter = index"><strong>{{ parameter.name }}</strong><span>{{ parameter.before }}{{ parameter.unit }}</span><b :class="{ abnormal: parameter.abnormal }">{{ parameter.trigger }}{{ parameter.unit }}</b><span>{{ parameter.after }}{{ parameter.unit }}</span><em :class="parameter.abnormal ? 'danger' : 'ok'">{{ parameter.abnormal ? '触发异常' : '保持稳定' }}</em></button></div></section>
      </main>
      <aside class="panel evidence-panel"><div class="section-heading"><div><h3>证据包与判断</h3><p>报警过程自动分析结果</p></div></div><div class="evidence-score"><span>根因可信度</span><strong>92%</strong><div><i style="width:92%"></i></div></div><div class="root-cause"><span>初步根因</span><strong>气压下降导致气缸伸出动作时间延长</strong><p>触发前200ms气压开始下降，动作时间同步上升；报警后气压逐步恢复。</p></div><div class="evidence-stats"><div><span>PLC快照</span><strong>21 帧</strong></div><div><span>监控参数</span><strong>5 点</strong></div><div><span>采样周期</span><strong>100 ms</strong></div><div><span>数据质量</span><strong class="green">Good</strong></div></div><el-button type="primary">导出证据包</el-button></aside>
    </section>
  </div>
</template>
