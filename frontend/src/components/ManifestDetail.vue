<script setup>
import { ref, watch, computed } from 'vue';
import { getManifest, closeManifest } from '../services/api';
import SpecimenTable from './SpecimenTable.vue';

const props = defineProps({
  manifestId: {
    type: Number,
    default: null,
  },
});

const emit = defineEmits(['updated']);

const manifest = ref(null);
const loading = ref(false);
const error = ref(null);
const closing = ref(false);
const closeError = ref(null);

const formatSentDate = (dateStr) => {
  if (!dateStr) return '';
  const date = new Date(dateStr);
  const day = date.getDate();
  const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  const month = months[date.getMonth()];
  const year = date.getFullYear();
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `Sent ${day} ${month} ${year}, ${hours}:${minutes}`;
};

const loadManifest = async () => {
  if (!props.manifestId) {
    manifest.value = null;
    return;
  }
  loading.value = true;
  error.value = null;
  closeError.value = null;
  try {
    const data = await getManifest(props.manifestId);
    manifest.value = data;
  } catch (err) {
    error.value = err.message || 'Failed to load manifest details.';
    manifest.value = null;
  } finally {
    loading.value = false;
  }
};

const handleCloseManifest = async () => {
  if (!props.manifestId) return;
  closing.value = true;
  closeError.value = null;
  try {
    await closeManifest(props.manifestId);
    await loadManifest();
    emit('updated');
  } catch (err) {
    closeError.value = err.message || 'Failed to close manifest.';
  } finally {
    closing.value = false;
  }
};

watch(
  () => props.manifestId,
  () => {
    loadManifest();
  },
  { immediate: true }
);

// Metrics computations
const expectedCount = computed(() => {
  if (!manifest.value) return 0;
  return manifest.value.pendingCount + manifest.value.receivedCount + manifest.value.flaggedCount + manifest.value.addedCount;
});

const receivedCount = computed(() => {
  if (!manifest.value) return 0;
  return manifest.value.receivedCount + manifest.value.addedCount;
});

const pendingCount = computed(() => {
  if (!manifest.value) return 0;
  return manifest.value.pendingCount;
});

const flaggedCount = computed(() => {
  if (!manifest.value) return 0;
  return manifest.value.flaggedCount;
});

const canClose = computed(() => {
  if (!manifest.value) return false;
  const hasPending = manifest.value.pendingCount > 0;
  const hasOpenDiscrepancies = manifest.value.discrepancies && manifest.value.discrepancies.some(d => d.status === 'Open');
  return !hasPending && !hasOpenDiscrepancies && manifest.value.status !== 'Closed';
});

// Trigger a visual notification when clicking "Flag discrepancy"
const triggerFlagAlert = () => {
  const element = document.getElementById('specimen-table-container');
  if (element) {
    element.scrollIntoView({ behavior: 'smooth' });
    element.classList.add('highlight-flash');
    setTimeout(() => {
      element.classList.remove('highlight-flash');
    }, 2000);
  } else {
    alert("To flag a discrepancy, use the '⚑' button on a specific Pending specimen row below.");
  }
};

const formatResolvedDate = (dateStr) => {
  if (!dateStr) return '';
  const date = new Date(dateStr);
  const day = date.getDate();
  const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  const month = months[date.getMonth()];
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${day} ${month}, ${hours}:${minutes}`;
};

</script>

<template>
  <div class="detail-container">
    <!-- Empty state when no manifest selected -->
    <div v-if="!manifestId" class="no-selection">
      <div class="empty-graphic">🗂️</div>
      <h3>No Manifest Selected</h3>
      <p>Select a manifest from the sidebar to check in specimens and audit discrepancies.</p>
    </div>

    <!-- Loading state -->
    <div v-else-if="loading && !manifest" class="loading-state">
      <div class="spinner"></div>
      <p>Loading manifest details...</p>
    </div>

    <!-- Error state -->
    <div v-else-if="error" class="error-banner">
      <div class="error-icon">⚠️</div>
      <div class="error-content">
        <h4>Error Loading Manifest</h4>
        <p>{{ error }}</p>
        <button @click="loadManifest" class="btn btn-secondary mt-2">Retry</button>
      </div>
    </div>

    <!-- Manifest Detail View -->
    <div v-else-if="manifest" class="manifest-content">
      
      <!-- Top Header Area -->
      <div class="header-section flex-row justify-between mb-4">
        <div class="header-left">
          <div class="title-container flex-row gap-2 mb-2">
            <h2>Manifest {{ manifest.code }}</h2>
            <span class="workflow-badge">Fast Count</span>
          </div>
          <p class="subtitle">
            From <strong>{{ manifest.clinicName }}</strong> &middot; Bay 2 &middot; 
            {{ formatSentDate(manifest.sentAt) }} &middot; 
            <strong>{{ expectedCount }} specimens expected</strong> &middot; Khush Arya
          </p>
        </div>
        
        <!-- Header Action Buttons -->
        <div class="header-actions flex-row gap-2">
          <button 
            @click="triggerFlagAlert"
            class="btn btn-danger-outline flex-row gap-1"
          >
            <span class="flag-icon">⚑</span> Flag discrepancy
          </button>
          
          <button 
            @click="handleCloseManifest"
            :disabled="!canClose || closing"
            class="btn btn-primary"
          >
            <span v-if="closing" class="spinner-sm"></span>
            <span>{{ closing ? 'Closing…' : 'Mark Received & Close' }}</span>
          </button>
        </div>
      </div>

      <!-- Close error message (if any) -->
      <div v-if="closeError" class="close-error mb-4">
        Error closing manifest: {{ closeError }}
      </div>

      <!-- Metrics Grid Dashboard -->
      <section class="metrics-grid mb-6">
        <div class="metric-card">
          <span class="metric-number">{{ expectedCount }}</span>
          <span class="metric-label">EXPECTED</span>
        </div>
        <div class="metric-card">
          <span class="metric-number text-green">{{ receivedCount }}</span>
          <span class="metric-label">RECEIVED</span>
        </div>
        <div class="metric-card">
          <span class="metric-number">{{ pendingCount }}</span>
          <span class="metric-label">PENDING</span>
        </div>
        <div class="metric-card">
          <span class="metric-number text-red">{{ flaggedCount }}</span>
          <span class="metric-label">FLAGGED</span>
        </div>
      </section>

      <!-- Specimen Table Card -->
      <section id="specimen-table-container" class="table-section">
        <SpecimenTable 
          :specimens="manifest.specimens" 
          :discrepancies="manifest.discrepancies"
          :manifest-id="manifest.id"
          :manifest-status="manifest.status"
          :received-count="receivedCount"
          @updated="loadManifest" 
        />
      </section>

      <!-- Discrepancy Log Card -->
      <section v-if="manifest.discrepancies && manifest.discrepancies.length > 0" class="table-section mt-6">
        <div class="table-card">
          <div class="table-title-bar mb-4">
            <h3>Discrepancy Log</h3>
          </div>
          <div class="table-container">
            <table class="clinical-table">
              <thead>
                <tr>
                  <th>STATUS</th>
                  <th>TYPE</th>
                  <th>SPECIMEN ID</th>
                  <th>NOTE</th>
                  <th>RESOLUTION NOTE</th>
                  <th>RESOLVED AT</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="d in manifest.discrepancies" :key="d.id">
                  <td>
                    <span class="pill" :class="d.status === 'Open' ? 'pill--discrepancy' : 'pill--received'">
                      {{ d.status }}
                    </span>
                  </td>
                  <td class="font-semibold">{{ d.type }}</td>
                  <td class="font-semibold text-slate">
                    {{ manifest.specimens.find(s => s.id === d.specimenId)?.code || '—' }}
                  </td>
                  <td>{{ d.note }}</td>
                  <td>{{ d.resolutionNote || '—' }}</td>
                  <td>{{ d.resolvedAt ? formatResolvedDate(d.resolvedAt) : '—' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </section>


    </div>
  </div>
</template>

<style scoped>
.detail-container {
  padding: 1.5rem;
  background-color: #ffffff;
  height: 100%;
  overflow-y: auto;
}

.no-selection {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--text-secondary);
}

.empty-graphic {
  font-size: 3rem;
  margin-bottom: 1rem;
  opacity: 0.4;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--text-secondary);
  gap: 1rem;
}

.spinner {
  width: 32px;
  height: 32px;
  border: 3px solid #f3f3f3;
  border-top: 3px solid #0b3c5d;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.error-banner {
  display: flex;
  background-color: #fce8e6;
  border: 1px solid #f5c2c1;
  border-radius: var(--radius-md);
  padding: 1rem;
  gap: 1rem;
}

.error-icon {
  font-size: 1.5rem;
}

.error-content h4 {
  color: #c5221f;
  margin-bottom: 0.25rem;
  font-size: 0.95rem;
}

.error-content p {
  font-size: 0.8rem;
  color: var(--text-secondary);
}

.manifest-content {
  display: flex;
  flex-direction: column;
}

/* Header styling */
.header-section {
  border-bottom: 1px solid var(--border-color);
  padding-bottom: 1rem;
  align-items: flex-start;
}

.title-container h2 {
  font-size: 1.25rem;
  font-weight: 700;
  color: #0a2540;
}

.workflow-badge {
  background-color: #e8f0fe;
  color: #1a73e8;
  border: 1px solid #d2e3fc;
  font-size: 0.65rem;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 3px;
}

.subtitle {
  font-size: 0.75rem;
  color: var(--text-secondary);
  margin-top: 0.25rem;
}

.flag-icon {
  font-size: 0.95rem;
  line-height: 1;
}

.close-error {
  background-color: #fce8e6;
  color: #c5221f;
  border: 1px solid #f5c2c1;
  padding: 0.5rem 1rem;
  font-size: 0.75rem;
  border-radius: var(--radius-md);
}

/* Metrics row */
.metrics-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1rem;
}

.metric-card {
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  padding: 0.75rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background-color: #ffffff;
}

.metric-number {
  font-size: 1.75rem;
  font-weight: 700;
  color: #1e293b;
  line-height: 1.2;
}

.metric-number.text-green {
  color: #137333;
}

.metric-number.text-red {
  color: #c5221f;
}

.metric-label {
  font-size: 0.6rem;
  font-weight: 700;
  color: var(--text-secondary);
  margin-top: 0.25rem;
  letter-spacing: 0.05em;
}

/* Spinner for button */
.spinner-sm {
  width: 12px;
  height: 12px;
  border: 2px solid currentColor;
  border-bottom-color: transparent;
  border-radius: 50%;
  display: inline-block;
  animation: spin 1s linear infinite;
  margin-right: 6px;
}

.mb-6 {
  margin-bottom: 1.5rem;
}
</style>
