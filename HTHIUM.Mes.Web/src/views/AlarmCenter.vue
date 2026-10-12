<script setup lang="ts">
import { computed, ref } from 'vue'
import { Search } from '@element-plus/icons-vue'
import { alarms } from '../data/mockMes'

const emit = defineEmits<{ openSnapshot: [alarmId: string] }>()
const keyword = ref('')
const category = ref('全部')
const filtered = computed(() => alarms.filter(a => (category.value === '全部' || a.category === category.value) && (!keyword.value || `${a.device}${a.code}${a.name}${a.component}`.toLowerCase().includes(keyword.value.toLowerCase()))))
</script>

<template>
  <div class="prototype-view">
    <section class="sub-metrics"><article><span>今日报警</span><strong>126</strong><small>较昨日 -18.2%</small></article><article class="danger"><span>当前未恢复</span><strong>6</strong><small>高优先级 2 条</small></article><article><span>重复报警</span><strong>38</strong><small>占比 30.1%</small></article><article class="warning"><span>最长未恢复</span><strong>30m</strong><small>包装设备 07</small></article><article><span>已有快照</span><strong>94</strong><small>证据完整率 98.7%</small></article></section>
    <section class="panel prototype-panel">
      <div class="section-heading"><div><h3>报警事件中心</h3><p>汇总所有HTHIUM上位机上传的报警事件</p></div><div class="prototype-filters"><el-input v-model="keyword" :prefix-icon="Search" clearable placeholder="搜索设备、代码或报警名称" /><el-select v-model="category"><el-option v-for="item in ['全部','气缸','伺服','PLC','通讯']" :key="item" :label="item" :value="item" /></el-select><el-date-picker type="daterange" start-placeholder="开始日期" end-placeholder="结束日期" /></div></div>
      <el-table :data="filtered" height="calc(100vh - 300px)">
        <el-table-column prop="time" label="时间" width="100" /><el-table-column prop="line" label="产线" min-width="150" /><el-table-column prop="device" label="设备" min-width="170" /><el-table-column prop="component" label="部件" width="90" />
        <el-table-column label="等级" width="85"><template #default="{ row }"><el-tag :type="row.level === '高' ? 'danger' : row.level === '中' ? 'warning' : 'info'" effect="light">{{ row.level }}</el-tag></template></el-table-column>
        <el-table-column prop="code" label="报警代码" min-width="135" /><el-table-column prop="name" label="报警名称" min-width="210" /><el-table-column prop="duration" label="持续" width="100" />
        <el-table-column label="状态" width="100"><template #default="{ row }"><span :class="['process-state', row.status === '未处理' ? 'danger' : row.status === '处理中' ? 'warning' : 'ok']">{{ row.status }}</span></template></el-table-column>
        <el-table-column label="证据" width="105"><template #default="{ row }"><el-button v-if="row.snapshot" type="primary" link @click="emit('openSnapshot', row.id)">查看快照</el-button><span v-else class="muted">无快照</span></template></el-table-column>
      </el-table>
    </section>
  </div>
</template>
