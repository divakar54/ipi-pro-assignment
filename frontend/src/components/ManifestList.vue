<script setup>
import { computed } from 'vue';
import StatusPill from './StatusPill.vue';

const props = defineProps({
  manifests: {
    type: Array,
    required: true,
  },
  selectedId: {
    type: Number,
    default: null,
  },
});

const emit = defineEmits(['select']);

const handleSelect = (id) => {
  emit('select', id);
};

// Simple helper to calculate total expected (Pending + Received + Flagged + Added)
const getExpectedCount = (m) => {
  return m.pendingCount + m.receivedCount + m.flaggedCount + m.addedCount;
};

const getReceivedCount = (m) => {
  return m.receivedCount + m.addedCount;
};

// Check if manifest has any discrepancies
const getDiscrepancyText = (m) => {
  const discrepancyCount = m.flaggedCount + m.addedCount;
  if (discrepancyCount === 0) return null;
  return discrepancyCount === 1 ? '1 discrepancy' : `${discrepancyCount} discrepancies`;
};
</script>

<template>
  <div class="list-wrapper">
    <div v-if="manifests.length === 0" class="empty-list">
      No manifests found.
    </div>
    <div 
      v-else
      v-for="manifest in manifests" 
      :key="manifest.id"
      class="manifest-item"
      :class="{ 'manifest-item--selected': manifest.id === selectedId }"
      @click="handleSelect(manifest.id)"
    >
      <div class="item-top flex-row justify-between mb-2">
        <span class="manifest-code">{{ manifest.code }}</span>
        <span class="received-ratio">
          {{ getReceivedCount(manifest) }}/{{ getExpectedCount(manifest) }} received
        </span>
      </div>

      <div class="item-bottom flex-row justify-between">
        <span class="clinic-name">{{ manifest.clinicName }}</span>
        
        <!-- Render status pills matching screenshot -->
        <span v-if="getDiscrepancyText(manifest)" class="pill pill--discrepancy">
          {{ getDiscrepancyText(manifest) }}
        </span>
        <span v-else-if="manifest.status === 'InTransit'" class="pill pill--intransit">
          In transit
        </span>
        <span v-else-if="manifest.status === 'Closed' || manifest.status === 'ClosedWithDiscrepancy' || manifest.receivedCount === getExpectedCount(manifest)" class="pill pill--received">
          Received
        </span>
        <span v-else class="pill pill--pending">
          Pending
        </span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.list-wrapper {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  padding: 0.25rem 0;
}

.empty-list {
  text-align: center;
  color: var(--text-secondary);
  font-style: italic;
  padding: 2rem 0;
  font-size: 0.75rem;
}

.manifest-item {
  background: #ffffff;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  padding: 0.75rem;
  cursor: pointer;
  transition: var(--transition-smooth);
}

.manifest-item:hover {
  background: #f8fafc;
  border-color: #cbd5e1;
}

.manifest-item--selected {
  background: #f0f7ff !important;
  border-color: #93c5fd !important;
  box-shadow: 0 0 0 1px #93c5fd;
}

.manifest-code {
  font-weight: 700;
  color: #1e293b;
  font-size: 0.8rem;
}

.received-ratio {
  font-size: 0.7rem;
  color: var(--text-secondary);
  font-weight: 500;
}

.clinic-name {
  font-size: 0.7rem;
  color: var(--text-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  max-width: 140px;
}

/* Pills are globally loaded from index.css */
</style>
