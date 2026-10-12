<script setup lang="ts">
import { dataQualityRows } from '../data/mockMes'
</script>

<template>
  <div class="prototype-view">
    <section class="sub-metrics quality-metrics"><article><span>数据质量 Good</span><strong>16</strong><small>占接入设备 88.9%</small></article><article class="warning"><span>存在延迟</span><strong>1</strong><small>CAP-02 平均186ms</small></article><article class="danger"><span>通讯中断</span><strong>1</strong><small>PACK-07 已中断30分钟</small></article><article><span>待补传数据</span><strong>366</strong><small>恢复连接后自动补传</small></article><article><span>今日丢帧率</span><strong>0.18%</strong><small>目标低于0.5%</small></article></section>
    <section class="quality-layout">
      <article class="panel prototype-panel"><div class="section-heading"><div><h3>设备数据链路质量</h3><p>监测心跳、上传延迟、丢帧、时钟偏差和补传队列</p></div><el-tag type="info" effect="plain">模拟数据</el-tag></div><el-table :data="dataQualityRows" height="calc(100vh - 330px)"><el-table-column prop="device" label="设备" min-width="170" /><el-table-column prop="code" label="设备编码" width="120" /><el-table-column prop="heartbeat" label="最后心跳" width="120" /><el-table-column label="上传延迟" width="130"><template #default="{ row }"><span :class="{ 'text-warning': row.latency > 100 }">{{ row.latency ? `${row.latency} ms` : '-' }}</span></template></el-table-column><el-table-column label="丢帧率" width="110"><template #default="{ row }">{{ row.loss }}%</template></el-table-column><el-table-column label="时钟偏差" width="110"><template #default="{ row }">{{ row.clock }} ms</template></el-table-column><el-table-column prop="pending" label="待补传" width="100" /><el-table-column label="质量" width="110"><template #default="{ row }"><el-tag :type="row.quality === 'Good' ? 'success' : row.quality === 'Warning' ? 'warning' : 'danger'">{{ row.quality }}</el-tag></template></el-table-column></el-table></article>
      <aside class="panel quality-events"><div class="section-heading"><div><h3>链路异常事件</h3><p>最近24小时</p></div></div><div><span class="event-icon danger">!</span><p><strong>PACK-07 通讯中断</strong><small>本地缓存346条数据等待补传</small></p><time>09:12</time></div><div><span class="event-icon warning">!</span><p><strong>CAP-02 上传延迟升高</strong><small>连续5分钟超过150ms</small></p><time>09:36</time></div><div><span class="event-icon info">i</span><p><strong>设备 03 完成补传</strong><small>已补传18条报警快照</small></p><time>08:45</time></div></aside>
    </section>
  </div>
</template>
