<script setup lang="ts">
import { alarms, lineStates, overviewMetrics } from '../data/mockMes'

const emit = defineEmits<{ navigate: [view: string] }>()
const trend = [31, 24, 38, 29, 44, 36, 27]
</script>

<template>
  <div class="prototype-view dashboard-view">
    <section class="overview-hero">
      <div><span class="page-kicker">制造运行控制台</span><h2>生产运行总览</h2><p>汇总各产线设备、报警与关键部件健康状态</p></div>
      <div class="hero-time"><span>当前班次</span><strong>白班 08:00-20:00</strong><small>当前为演示数据</small></div>
    </section>
    <section class="mes-metric-grid">
      <article v-for="item in overviewMetrics" :key="item.label" :class="['mes-metric', item.tone]">
        <span>{{ item.label }}</span><div><strong>{{ item.value }}</strong><em>{{ item.suffix }}</em></div><small>{{ item.hint }}</small>
      </article>
    </section>
    <section class="dashboard-main-grid">
      <article class="panel factory-panel">
        <div class="section-heading"><div><h3>产线运行态势</h3><p>点击产线查看设备明细</p></div><button type="button" @click="emit('navigate', 'devices')">查看全部设备</button></div>
        <div class="line-state-grid">
          <button v-for="line in lineStates" :key="line.code" type="button" class="line-state-card" @click="emit('navigate', 'devices')">
            <div class="line-card-head"><span>{{ line.code }}</span><i :class="line.state === '运行' ? 'ok' : 'warn'"></i><em>{{ line.state }}</em></div>
            <strong>{{ line.name }}</strong><div class="line-numbers"><span><b>{{ line.running }}</b>/{{ line.devices }} 运行</span><span :class="{ danger: line.alarms > 1 }">{{ line.alarms }} 条报警</span></div>
            <div class="line-progress"><i :style="{ width: `${line.rate}%` }"></i></div><small>综合可用率 {{ line.rate }}%</small>
          </button>
        </div>
      </article>
      <article class="panel realtime-alarm-panel">
        <div class="section-heading"><div><h3>实时报警流</h3><p>当前最需要关注的异常</p></div><button type="button" @click="emit('navigate', 'alarms')">进入报警中心</button></div>
        <div class="alarm-stream">
          <button v-for="alarm in alarms.slice(0, 4)" :key="alarm.id" type="button" class="alarm-stream-row" @click="emit('navigate', alarm.snapshot ? 'snapshots' : 'alarms')">
            <span :class="['alarm-level-dot', alarm.level === '高' ? 'high' : 'medium']"></span><div><strong>{{ alarm.name }}</strong><small>{{ alarm.device }} · {{ alarm.component }}</small></div><time>{{ alarm.time }}</time>
          </button>
        </div>
      </article>
      <article class="panel trend-panel">
        <div class="section-heading"><div><h3>近7日报警趋势</h3><p>报警总量与高优先级变化</p></div><span class="trend-down">↓ 18.2%</span></div>
        <div class="bar-chart"><div v-for="(value, index) in trend" :key="index" class="bar-column"><span>{{ value }}</span><i :style="{ height: `${value * 2.2}px` }"></i><small>{{ ['14','15','16','17','18','19','今天'][index] }}</small></div></div>
      </article>
      <article class="panel attention-panel">
        <div class="section-heading"><div><h3>重点关注部件</h3><p>按健康度与重复报警综合排序</p></div></div>
        <div class="attention-list">
          <button type="button" @click="emit('navigate', 'servo')"><b>01</b><span><strong>SV-X3 上料横移轴</strong><small>跟随误差持续上升</small></span><em class="critical">58%</em></button>
          <button type="button" @click="emit('navigate', 'cylinders')"><b>02</b><span><strong>CY-21 压紧气缸</strong><small>动作时间超过基线</small></span><em class="critical">61%</em></button>
          <button type="button" @click="emit('navigate', 'servo')"><b>03</b><span><strong>SV-Z1 压装轴</strong><small>峰值负载偏高</small></span><em class="warning">72%</em></button>
        </div>
      </article>
    </section>
  </div>
</template>
