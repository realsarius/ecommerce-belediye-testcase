const STORAGE_KEY = 'campaign_session_id';

function createSessionId() {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }

  return `campaign-session-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

export function getCampaignSessionId() {
  if (typeof window === 'undefined') {
    return null;
  }

  const existing = window.localStorage.getItem(STORAGE_KEY);
  if (existing) {
    return existing;
  }

  const sessionId = createSessionId();
  window.localStorage.setItem(STORAGE_KEY, sessionId);
  return sessionId;
}
