const BASE = '/api';
const LAB_ID = import.meta.env?.VITE_LAB_ID || '1';

async function request(url, options = {}) {
  const headers = {
    'Content-Type': 'application/json',
    'X-Lab-Id': LAB_ID,
    ...options.headers,
  };

  const response = await fetch(url, { ...options, headers });

  if (!response.ok) {
    let errorMessage = response.statusText;
    try {
      const data = await response.json();
      if (data && data.error) {
        errorMessage = data.error;
      }
    } catch (e) {
      // Ignore JSON parse errors
    }
    throw new Error(errorMessage);
  }

  return response.json();
}

export function getManifests(status = null) {
  const url = status ? `${BASE}/manifests?status=${encodeURIComponent(status)}` : `${BASE}/manifests`;
  return request(url);
}

export function getManifest(id) {
  return request(`${BASE}/manifests/${id}`);
}

export function receiveSpecimen(manifestId, specimenId) {
  return request(`${BASE}/manifests/${manifestId}/specimens/${specimenId}/receive`, {
    method: 'POST',
  });
}

export function flagSpecimen(manifestId, specimenId) {
  return request(`${BASE}/manifests/${manifestId}/specimens/${specimenId}/flag`, {
    method: 'POST',
  });
}

export function addOffManifestSpecimen(manifestId, specimenData) {
  return request(`${BASE}/manifests/${manifestId}/specimens`, {
    method: 'POST',
    body: JSON.stringify(specimenData),
  });
}

export function closeManifest(manifestId) {
  return request(`${BASE}/manifests/${manifestId}/close`, {
    method: 'POST',
  });
}

export function resolveDiscrepancy(manifestId, discrepancyId, note) {
  return request(`${BASE}/manifests/${manifestId}/discrepancies/${discrepancyId}/resolve`, {
    method: 'POST',
    body: JSON.stringify({ note }),
  });
}

