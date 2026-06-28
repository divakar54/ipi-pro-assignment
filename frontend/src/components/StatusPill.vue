<script setup>
import { computed } from 'vue';

const props = defineProps({
  status: {
    type: String,
    required: true,
  },
});

const pillClass = computed(() => {
  if (!props.status) return '';
  const cleanStatus = props.status.toLowerCase().replace(/[\s_]+/g, '');
  if (cleanStatus === 'added') {
    return 'pill--received'; // Map 'added' (off-manifest) to same received green styling
  }
  return `pill--${cleanStatus}`;
});

const displayText = computed(() => {
  if (!props.status) return '';
  const cleanStatus = props.status.toLowerCase().replace(/[\s_]+/g, '');
  if (cleanStatus === 'received' || cleanStatus === 'added') {
    return '✓ Received';
  }
  if (cleanStatus === 'intransit') {
    return 'In transit';
  }
  return props.status;
});
</script>

<template>
  <span class="pill" :class="pillClass">{{ displayText }}</span>
</template>

<style scoped>
.pill {
  display: inline-block;
  padding: 3px 10px;
  border-radius: var(--radius-sm);
  font-size: 0.7rem;
  font-weight: 700;
  white-space: nowrap;
  letter-spacing: 0.01em;
}

.pill--pending {
  background-color: #f1f3f4;
  color: #5f6368;
}

.pill--received {
  background-color: #e6f4ea;
  color: #137333;
}

.pill--flagged {
  background-color: #fce8e6;
  color: #c5221f;
}

.pill--intransit {
  background-color: #e8f0fe;
  color: #1a73e8;
}

.pill--open {
  background-color: #fef9c3;
  color: #854d0e;
}

.pill--closed {
  background-color: #e6f4ea;
  color: #137333;
}

.pill--closedwithdiscrepancy {
  background-color: #fce8e6;
  color: #c5221f;
}
</style>
