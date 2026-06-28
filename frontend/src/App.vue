<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { getManifests } from './services/api';
import ManifestList from './components/ManifestList.vue';
import ManifestDetail from './components/ManifestDetail.vue';

const manifests = ref([]);
const savedId = localStorage.getItem('selectedManifestId');
const selectedManifestId = ref(savedId ? parseInt(savedId, 10) : null);
const listLoading = ref(false);
const listError = ref(null);

// Sidebar states
const workflowMode = ref('FastCount'); // 'FastCount' or 'FullScan'
const searchQuery = ref('');
const countedBottles = ref(0);

// Fetch manifest list
const loadManifests = async () => {
  listLoading.value = true;
  listError.value = null;
  try {
    const data = await getManifests();
    manifests.value = data;
    
    // Auto-select first manifest if saved selection is invalid or none selected
    const exists = data.some(m => m.id === selectedManifestId.value);
    if (!exists || selectedManifestId.value === null) {
      if (data.length > 0) {
        selectedManifestId.value = data[0].id;
        localStorage.setItem('selectedManifestId', data[0].id);
      } else {
        selectedManifestId.value = null;
        localStorage.removeItem('selectedManifestId');
      }
    }
  } catch (err) {
    listError.value = err.message || 'Failed to fetch manifests.';
  } finally {
    listLoading.value = false;
  }
};

const handleSelect = (id) => {
  selectedManifestId.value = id;
  localStorage.setItem('selectedManifestId', id);
};

const handleUpdate = () => {
  loadManifests();
};


onMounted(() => {
  loadManifests();
});

// Active selected manifest
const selectedManifest = computed(() => {
  return manifests.value.find((m) => m.id === selectedManifestId.value) || null;
});

// Calculate expected count based on specimens on the manifest
const expectedCount = computed(() => {
  if (!selectedManifest.value) return 0;
  return selectedManifest.value.pendingCount + selectedManifest.value.receivedCount + selectedManifest.value.flaggedCount + selectedManifest.value.addedCount;
});

// Auto-sync counter to expected count when manifest selection changes
watch(
  selectedManifest,
  (newManifest) => {
    if (newManifest) {
      const totalExpected = newManifest.pendingCount + newManifest.receivedCount + newManifest.flaggedCount + newManifest.addedCount;
      countedBottles.value = totalExpected;
    }
  },
  { immediate: true }
);


// Filter manifests in sidebar
const filteredManifests = computed(() => {
  if (!searchQuery.value.trim()) return manifests.value;
  const q = searchQuery.value.toLowerCase();
  return manifests.value.filter(
    (m) =>
      m.code.toLowerCase().includes(q) ||
      m.clinicName.toLowerCase().includes(q)
  );
});

// Counter handlers
const incrementBottles = () => {
  countedBottles.value++;
};
const decrementBottles = () => {
  if (countedBottles.value > 0) {
    countedBottles.value--;
  }
};

// Check discrepancy counts across all manifests for tab badge
const totalDiscrepancies = computed(() => {
  return manifests.value.reduce((acc, m) => acc + (m.flaggedCount || 0) + (m.addedCount || 0), 0);
});
</script>

<template>
  <div class="app-layout">
    <!-- Top Branding Navbar -->
    <header class="app-navbar flex-row justify-between">
      <div class="brand flex-row gap-4">
        <span class="brand-logo">IPI</span>
        <span class="uat-badge">UAT</span>
        <span class="nav-info">Mode: <strong>Check-In</strong></span>
        <span class="nav-info separator">Location: <strong>Central Lab — Receiving</strong></span>
      </div>
      <div class="user-profile flex-row gap-2">
        <span class="user-name">Lab Tech 1</span>
        <div class="avatar">LT</div>
      </div>
    </header>

    <!-- Navigation Tabs Bar -->
    <div class="tab-bar flex-row">
      <button class="tab-btn active">Check-In</button>
      <button class="tab-btn">Scan History <span class="tab-badge gray">12</span></button>
      <button class="tab-btn">Manifests</button>
      <button class="tab-btn">Discrepancies <span class="tab-badge red">{{ totalDiscrepancies || 5 }}</span></button>
    </div>

    <!-- Main Workspace -->
    <div class="app-workspace">
      <!-- Left Sidebar Action Panel -->
      <aside class="sidebar-panel">
        
        <!-- Workflow Card -->
        <div class="sidebar-card">
          <div class="card-header flex-row justify-between mb-2">
            <span class="card-label">Verification workflow</span>
            <span class="setting-tag">LAB SETTING</span>
          </div>
          <div class="toggle-group">
            <button 
              @click="workflowMode = 'FastCount'"
              class="toggle-btn" 
              :class="{ active: workflowMode === 'FastCount' }"
            >
              Fast Count
            </button>
            <button 
              @click="workflowMode = 'FullScan'"
              class="toggle-btn" 
              :class="{ active: workflowMode === 'FullScan' }"
            >
              Full Scan
            </button>
          </div>
        </div>

        <!-- Search Card -->
        <div class="sidebar-card">
          <div class="card-header mb-2">
            <span class="card-label">FIND MANIFEST</span>
          </div>
          <div class="search-box-container">
            <span class="barcode-icon">║║</span>
            <input 
              v-model="searchQuery" 
              type="text" 
              class="search-input" 
              placeholder="Scan or search manifest..."
            />
          </div>
        </div>

        <!-- Verify Count Card -->
        <div class="sidebar-card">
          <div class="card-header mb-2">
            <span class="card-label">VERIFY & RECEIVE</span>
          </div>
          <p class="counter-instruction">Total bottles counted by lab tech</p>
          <div class="counter-container flex-row justify-between mt-2">
            <button @click="decrementBottles" class="counter-btn">-</button>
            <span class="counter-value">{{ countedBottles }}</span>
            <button @click="incrementBottles" class="counter-btn">+</button>
          </div>
          
          <div class="counter-status mt-4">
            <div v-if="countedBottles === expectedCount && expectedCount > 0" class="status-match">
              Matches {{ expectedCount }} expected — ready to close.
            </div>
            <div v-else-if="expectedCount > 0" class="status-mismatch">
              Mismatch: expected {{ expectedCount }}, counted {{ countedBottles }}
            </div>
            <div v-else class="status-empty">
              No active expected count.
            </div>
          </div>
        </div>

        <!-- Recent Manifests List Card -->
        <div class="sidebar-card fill-card">
          <div class="card-header mb-2">
            <span class="card-label">RECENT MANIFESTS</span>
          </div>
          
          <div class="sidebar-list-container">
            <div v-if="listLoading && manifests.length === 0" class="list-loader">
              <span class="spinner-sm"></span> Loading...
            </div>
            <div v-else-if="listError" class="list-error">
              {{ listError }}
            </div>
            <ManifestList
              v-else
              :manifests="filteredManifests"
              :selected-id="selectedManifestId"
              @select="handleSelect"
            />
          </div>
          
          <button class="btn btn-secondary w-full view-all-btn mt-2">
            View all manifests &rsaquo;
          </button>
        </div>

      </aside>

      <!-- Right Main Panel -->
      <main class="main-panel">
        <ManifestDetail
          :manifest-id="selectedManifestId"
          @updated="handleUpdate"
        />
      </main>
    </div>
  </div>
</template>

<style scoped>
.app-layout {
  display: flex;
  flex-direction: column;
  height: 100vh;
  width: 100vw;
  background-color: var(--bg-app);
}

/* Top Navy Navbar */
.app-navbar {
  height: 50px;
  background-color: var(--bg-navbar);
  color: #ffffff;
  padding: 0 1.25rem;
  font-size: 0.8rem;
  font-weight: 500;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.brand-logo {
  font-size: 1.1rem;
  font-weight: 800;
  letter-spacing: 0.05em;
}

.uat-badge {
  background-color: #2b5c8f;
  font-size: 0.65rem;
  font-weight: 700;
  padding: 2px 6px;
  border-radius: 3px;
  color: #ffffff;
}

.nav-info {
  color: #a0aec0;
  font-size: 0.75rem;
}

.nav-info strong {
  color: #ffffff;
}

.nav-info.separator {
  padding-left: 1rem;
  border-left: 1px solid rgba(255, 255, 255, 0.2);
}

.user-profile {
  font-size: 0.75rem;
}

.user-name {
  color: #e2e8f0;
}

.avatar {
  background-color: #4299e1;
  color: var(--bg-navbar);
  font-weight: 700;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.7rem;
}

/* Horizontal tab bar */
.tab-bar {
  height: 48px;
  background-color: var(--bg-tabbar);
  border-bottom: 1px solid var(--border-color);
  padding: 0 1.25rem;
  gap: 1.5rem;
}

.tab-btn {
  background: none;
  border: none;
  font-family: var(--font-sans);
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--text-secondary);
  cursor: pointer;
  height: 100%;
  display: flex;
  align-items: center;
  padding: 0 0.25rem;
  position: relative;
  transition: var(--transition-smooth);
}

.tab-btn:hover {
  color: var(--text-primary);
}

.tab-btn.active {
  color: #0b3c5d;
  border-bottom: 3px solid #0b3c5d;
  margin-bottom: -1px;
}

.tab-badge {
  font-size: 0.7rem;
  padding: 1px 6px;
  border-radius: var(--radius-full);
  margin-left: 4px;
  font-weight: 700;
}

.tab-badge.gray {
  background-color: #f1f3f4;
  color: #5f6368;
}

.tab-badge.red {
  background-color: #fce8e6;
  color: #c5221f;
}

/* Main Workspace Grid */
.app-workspace {
  display: grid;
  grid-template-columns: 290px 1fr;
  flex: 1;
  overflow: hidden;
}

/* Left Sidebar styling */
.sidebar-panel {
  background-color: var(--bg-sidebar);
  border-right: 1px solid var(--border-color);
  padding: 1rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  overflow-y: auto;
  min-height: 0;
}

/* Right Main Panel styling */
.main-panel {
  height: 100%;
  min-height: 0;
  overflow: hidden;
}


.sidebar-card {
  background: var(--bg-card);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  padding: 0.75rem 1rem;
  box-shadow: var(--shadow-sm);
  display: flex;
  flex-direction: column;
}

.fill-card {
  flex: 1;
  min-height: 250px;
  overflow: hidden;
}

.card-header {
  font-size: 0.65rem;
  font-weight: 700;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.setting-tag {
  background-color: #f1f3f4;
  color: #5f6368;
  font-size: 0.55rem;
  padding: 2px 4px;
  border-radius: 2px;
}

/* Toggle Switch Group */
.toggle-group {
  display: grid;
  grid-template-columns: 1fr 1fr;
  background-color: #f1f3f4;
  padding: 2px;
  border-radius: var(--radius-md);
}

.toggle-btn {
  background: none;
  border: none;
  font-family: var(--font-sans);
  font-size: 0.75rem;
  font-weight: 600;
  padding: 6px;
  color: var(--text-secondary);
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: var(--transition-smooth);
}

.toggle-btn.active {
  background-color: #0b3c5d;
  color: #ffffff;
  box-shadow: var(--shadow-sm);
}

/* Search Box styling */
.search-box-container {
  display: flex;
  align-items: center;
  background-color: #f1f5f9;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-md);
  padding: 0 0.5rem;
}

.barcode-icon {
  font-size: 0.85rem;
  color: #64748b;
  margin-right: 0.35rem;
  letter-spacing: -1px;
}

.search-input {
  background: none;
  border: none;
  outline: none;
  font-family: var(--font-sans);
  font-size: 0.75rem;
  width: 100%;
  height: 32px;
  color: var(--text-primary);
}

/* Counter styles */
.counter-instruction {
  font-size: 0.7rem;
  color: var(--text-secondary);
}

.counter-container {
  background-color: #ffffff;
}

.counter-btn {
  width: 32px;
  height: 32px;
  border: 1px solid var(--border-color);
  background: #ffffff;
  border-radius: var(--radius-md);
  font-size: 1.1rem;
  font-weight: 500;
  color: #64748b;
  cursor: pointer;
  transition: var(--transition-smooth);
  display: flex;
  align-items: center;
  justify-content: center;
}

.counter-btn:hover {
  background-color: #f8fafc;
  border-color: #cbd5e1;
}

.counter-value {
  font-size: 1.5rem;
  font-weight: 700;
  color: var(--text-primary);
}

.counter-status {
  font-size: 0.7rem;
  font-weight: 600;
}

.status-match {
  color: #137333;
}

.status-mismatch {
  color: #c5221f;
}

.status-empty {
  color: var(--text-muted);
}

/* Sidebar List styling */
.sidebar-list-container {
  flex: 1;
  overflow-y: auto;
  margin-top: 0.5rem;
}

.view-all-btn {
  width: 100%;
  font-size: 0.75rem;
  padding: 6px;
  border: 1px solid var(--border-color);
  background-color: #ffffff;
  color: var(--text-primary);
  border-radius: var(--radius-md);
  font-weight: 600;
}

.list-loader, .list-error {
  text-align: center;
  padding: 1rem 0;
  font-size: 0.75rem;
  color: var(--text-secondary);
}

.w-full {
  width: 100%;
}
</style>
