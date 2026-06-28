<script setup>
import { ref, reactive, computed } from 'vue';
import { receiveSpecimen, flagSpecimen, addOffManifestSpecimen, resolveDiscrepancy } from '../services/api';
import StatusPill from './StatusPill.vue';

const props = defineProps({
  specimens: {
    type: Array,
    required: true,
  },
  discrepancies: {
    type: Array,
    default: () => [],
  },
  manifestId: {
    type: Number,
    required: true,
  },
  manifestStatus: {
    type: String,
    required: true,
  },
  receivedCount: {
    type: Number,
    default: 0,
  }
});

const emit = defineEmits(['updated']);

// Per-row state Map<specimenId, { loading, error }>
const rowStates = reactive(new Map());
const getRowState = (id) => {
  if (!rowStates.has(id)) {
    rowStates.set(id, { loading: false, error: null });
  }
  return rowStates.get(id);
};

const handleReceive = async (specimenId) => {
  const state = getRowState(specimenId);
  state.loading = true;
  state.error = null;
  try {
    await receiveSpecimen(props.manifestId, specimenId);
    emit('updated');
  } catch (err) {
    state.error = err.message || 'Error receiving specimen';
  } finally {
    state.loading = false;
  }
};

const handleFlag = async (specimenId) => {
  const state = getRowState(specimenId);
  state.loading = true;
  state.error = null;
  try {
    await flagSpecimen(props.manifestId, specimenId);
    emit('updated');
  } catch (err) {
    state.error = err.message || 'Error flagging specimen';
  } finally {
    state.loading = false;
  }
};

// Off-manifest form toggle
const showAddForm = ref(false);
const formLoading = ref(false);
const formError = ref(null);
const form = reactive({
  code: '',
  patient: '',
  site: '',
  provider: '',
});

const handleAddOffManifest = async () => {
  if (!form.code.trim()) {
    formError.value = 'Specimen Code is required';
    return;
  }
  formLoading.value = true;
  formError.value = null;
  try {
    await addOffManifestSpecimen(props.manifestId, {
      code: form.code,
      patient: form.patient,
      site: form.site,
      provider: form.provider,
    });
    emit('updated');
    showAddForm.value = false;
    form.code = '';
    form.patient = '';
    form.site = '';
    form.provider = '';
  } catch (err) {
    formError.value = err.message || 'Error adding off-manifest specimen';
  } finally {
    formLoading.value = false;
  }
};

// Discrepancy Resolution State
const resolvingSpecimenId = ref(null);
const resolutionNote = ref('');
const submitLoading = ref(false);

const getOpenDiscrepancy = (specimenId) => {
  return props.discrepancies.find(d => d.specimenId === specimenId && d.status === 'Open');
};

const startResolution = (specimenId) => {
  resolvingSpecimenId.value = specimenId;
  resolutionNote.value = '';
};

const cancelResolution = () => {
  resolvingSpecimenId.value = null;
  resolutionNote.value = '';
};

const submitResolution = async (specimenId) => {
  const discrepancy = getOpenDiscrepancy(specimenId);
  if (!discrepancy) return;
  
  if (!resolutionNote.value.trim()) {
    alert('Resolution note is required');
    return;
  }
  
  submitLoading.value = true;
  try {
    await resolveDiscrepancy(props.manifestId, discrepancy.id, resolutionNote.value.trim());
    emit('updated');
    cancelResolution();
  } catch (err) {
    alert(err.message || 'Failed to resolve discrepancy');
  } finally {
    submitLoading.value = false;
  }
};

// Formatting helpers for Received By and At
const getReceivedBy = (specimen) => {
  return specimen.receivedBy || '—';
};

const getReceivedAt = (specimen) => {
  if (!specimen.receivedAt) return '—';
  try {
    const d = new Date(specimen.receivedAt);
    const hrs = String(d.getHours()).padStart(2, '0');
    const mins = String(d.getMinutes()).padStart(2, '0');
    return `${hrs}:${mins}`;
  } catch (e) {
    return '—';
  }
};

</script>

<template>
  <div class="table-card">
    <div class="table-title-bar flex-row justify-between mb-4">
      <div class="title-left flex-row gap-2">
        <h3>Specimens on manifest</h3>
      </div>
      <div class="title-right flex-row gap-2">
        <span class="received-badge">{{ receivedCount }} received</span>
        <button 
          v-if="manifestStatus === 'Open'"
          @click="showAddForm = !showAddForm" 
          class="btn btn-secondary btn-sm"
        >
          {{ showAddForm ? '✕ Close Form' : '＋ Add Off-Manifest' }}
        </button>
      </div>
    </div>

    <!-- Off-Manifest Add Form -->
    <div v-if="showAddForm && manifestStatus === 'Open'" class="add-form mb-4">
      <h4>Add Off-Manifest Specimen</h4>
      <div class="form-grid mt-2">
        <div class="form-group">
          <label>Specimen Code *</label>
          <input v-model="form.code" type="text" placeholder="e.g. SP-2026-A999" required />
        </div>
        <div class="form-group">
          <label>Patient</label>
          <input v-model="form.patient" type="text" placeholder="e.g. Sarah Lin" />
        </div>
        <div class="form-group">
          <label>Site</label>
          <input v-model="form.site" type="text" placeholder="e.g. Left cheek" />
        </div>
        <div class="form-group">
          <label>Provider</label>
          <input v-model="form.provider" type="text" placeholder="e.g. Dr. Patel" />
        </div>
      </div>
      <div v-if="formError" class="form-error mt-2">{{ formError }}</div>
      <div class="form-actions mt-4 flex-row gap-2">
        <button @click="handleAddOffManifest" :disabled="formLoading" class="btn btn-primary btn-sm">
          {{ formLoading ? 'Adding...' : 'Save Specimen' }}
        </button>
        <button @click="showAddForm = false" :disabled="formLoading" class="btn btn-secondary btn-sm">
          Cancel
        </button>
      </div>
    </div>

    <!-- Specimens Table -->
    <div class="table-container">
      <table class="clinical-table">
        <thead>
          <tr>
            <th>STATUS</th>
            <th>SPECIMEN ID</th>
            <th>PATIENT</th>
            <th>SITE</th>
            <th>PROVIDER</th>
            <th>RECEIVED BY</th>
            <th>AT</th>
            <th>ACTIONS</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="specimens.length === 0">
            <td colspan="8" class="empty-cell">
              No specimens on this manifest.
            </td>
          </tr>
          <template v-else v-for="specimen in specimens" :key="specimen.id">
            <tr :class="{ 'row-loading': getRowState(specimen.id).loading }">
              <td>
                <StatusPill :status="specimen.status" />
              </td>
              <td class="font-semibold text-slate">{{ specimen.code }}</td>
              <td>{{ specimen.patient || '—' }}</td>
              <td>{{ specimen.site || '—' }}</td>
              <td>{{ specimen.provider || '—' }}</td>
              <td>{{ getReceivedBy(specimen) }}</td>
              <td>{{ getReceivedAt(specimen) }}</td>
              <td>
                <!-- Action Buttons: edit and flag icons (visible for all or depending on status) -->
                <div class="flex-row gap-1">
                  <!-- If specimen has an open discrepancy: show Resolve button -->
                  <button 
                    v-if="getOpenDiscrepancy(specimen.id) && manifestStatus === 'Open'"
                    @click="startResolution(specimen.id)"
                    :disabled="getRowState(specimen.id).loading"
                    class="btn btn-secondary btn-xs"
                    title="Resolve Discrepancy"
                  >
                    Resolve
                  </button>
                  <template v-else>
                    <!-- For Pending status: show active check mark to receive -->
                    <button 
                      v-if="specimen.status === 'Pending' && manifestStatus === 'Open'"
                      @click="handleReceive(specimen.id)" 
                      :disabled="getRowState(specimen.id).loading"
                      class="btn-icon" 
                      title="Mark Received"
                    >
                      ✓
                    </button>
                    <button 
                      v-else
                      @click="handleReceive(specimen.id)"
                      :disabled="manifestStatus !== 'Open' || getRowState(specimen.id).loading"
                      class="btn-icon" 
                      title="Edit Specimen Details"
                    >
                      ✎
                    </button>

                    <button 
                      @click="handleFlag(specimen.id)" 
                      :disabled="manifestStatus !== 'Open' || specimen.status !== 'Pending' || getRowState(specimen.id).loading"
                      class="btn-icon btn-icon-flag"
                      :class="{ disabled: specimen.status !== 'Pending' || manifestStatus !== 'Open' }"
                      title="Flag as Discrepancy"
                    >
                      ⚑
                    </button>
                  </template>
                </div>
              </td>
            </tr>
            <!-- Inline Resolution Form -->
            <tr v-if="resolvingSpecimenId === specimen.id" class="resolution-row">
              <td colspan="8">
                <div class="resolution-container flex-row gap-2 mt-2 mb-2 p-2">
                  <span class="resolution-label">Resolution Note:</span>
                  <input 
                    v-model="resolutionNote" 
                    type="text" 
                    placeholder="e.g. Specimen found in box B" 
                    class="resolution-input" 
                    @keyup.enter="submitResolution(specimen.id)"
                    :disabled="submitLoading"
                  />
                  <button 
                    @click="submitResolution(specimen.id)" 
                    :disabled="submitLoading" 
                    class="btn btn-primary btn-xs"
                  >
                    {{ submitLoading ? 'Confirming...' : 'Confirm' }}
                  </button>
                  <button 
                    @click="cancelResolution" 
                    :disabled="submitLoading" 
                    class="btn btn-secondary btn-xs"
                  >
                    Cancel
                  </button>
                </div>
              </td>
            </tr>
            <!-- Row Error -->
            <tr v-if="getRowState(specimen.id).error" class="error-row">
              <td colspan="8">
                <span class="error-text">Error: {{ getRowState(specimen.id).error }}</span>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.table-card {
  background-color: #ffffff;
  border-top: 1px solid var(--border-color);
  padding-top: 1rem;
}

h3 {
  font-size: 0.95rem;
  font-weight: 700;
  color: #1e293b;
}

.received-badge {
  background-color: #e6f4ea;
  color: #137333;
  font-size: 0.7rem;
  font-weight: 700;
  padding: 3px 10px;
  border-radius: var(--radius-full);
}

.btn-sm {
  padding: 0.35rem 0.75rem;
  font-size: 0.75rem;
}

/* Off-manifest form */
.add-form {
  background-color: var(--bg-app);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  padding: 1rem;
}

.add-form h4 {
  font-size: 0.85rem;
  font-weight: 700;
  color: #0b3c5d;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 0.75rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.form-group label {
  font-size: 0.65rem;
  font-weight: 600;
  color: var(--text-secondary);
}

.form-group input {
  height: 28px;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-sm);
  padding: 0 0.5rem;
  font-size: 0.75rem;
  font-family: var(--font-sans);
  background-color: #ffffff;
}

.form-group input:focus {
  outline: none;
  border-color: var(--border-focus);
}

.form-error {
  color: #c5221f;
  font-size: 0.75rem;
  font-weight: 600;
}

/* Table styles are globally loaded from index.css */


.text-slate {
  color: #475569;
}

.empty-cell {
  text-align: center;
  padding: 2rem !important;
  color: var(--text-secondary);
  font-style: italic;
}

/* Square Icon Buttons matching screenshot */
.btn-icon {
  width: 24px;
  height: 24px;
  border: 1px solid var(--border-color);
  background-color: #ffffff;
  border-radius: var(--radius-sm);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 0.8rem;
  color: #64748b;
  cursor: pointer;
  transition: var(--transition-smooth);
}

.btn-icon:hover:not(:disabled) {
  background-color: #f1f5f9;
  border-color: #cbd5e1;
  color: #1e293b;
}

.btn-icon-flag {
  color: #c5221f;
}

.btn-icon-flag:hover:not(:disabled) {
  background-color: #fce8e6;
  border-color: #f5c2c1;
  color: #c5221f;
}

.btn-icon.disabled {
  opacity: 0.4;
  cursor: not-allowed;
  background-color: #f1f3f4 !important;
  border-color: var(--border-color) !important;
}

.row-loading {
  opacity: 0.6;
}

.error-row {
  background-color: #fce8e6;
}

.error-text {
  color: #c5221f;
  font-size: 0.75rem;
  font-weight: 600;
}

.resolution-row {
  background-color: #f8fafc;
}
.resolution-container {
  background-color: #f1f5f9;
  border-radius: var(--radius-sm);
  border: 1px solid var(--border-color);
  padding: 0.5rem;
  width: 100%;
}
.resolution-label {
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--text-primary);
}
.resolution-input {
  flex: 1;
  height: 26px;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-sm);
  padding: 0 0.5rem;
  font-size: 0.75rem;
}
.resolution-input:focus {
  outline: none;
  border-color: var(--border-focus);
}
</style>

