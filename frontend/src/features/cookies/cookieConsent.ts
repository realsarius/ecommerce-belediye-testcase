export type CookieConsentStatus = 'accepted' | 'rejected';

export type CookiePreferences = {
  necessary: true;
  analytics: boolean;
  marketing: boolean;
};

export const COOKIE_CONSENT_KEY = 'cookie_consent';
export const COOKIE_PREFERENCES_KEY = 'cookie_preferences';
export const COOKIE_SETTINGS_EVENT = 'cookie-settings:open';

export const defaultCookiePreferences: CookiePreferences = {
  necessary: true,
  analytics: true,
  marketing: false,
};

export function readCookieConsent(): CookieConsentStatus | null {
  const value = window.localStorage.getItem(COOKIE_CONSENT_KEY);
  return value === 'accepted' || value === 'rejected' ? value : null;
}

export function readCookiePreferences(): CookiePreferences {
  const raw = window.localStorage.getItem(COOKIE_PREFERENCES_KEY);

  if (!raw) {
    return defaultCookiePreferences;
  }

  try {
    const parsed = JSON.parse(raw) as Partial<CookiePreferences>;
    return {
      necessary: true,
      analytics: Boolean(parsed.analytics),
      marketing: Boolean(parsed.marketing),
    };
  } catch {
    return defaultCookiePreferences;
  }
}

export function persistCookieDecision(status: CookieConsentStatus, preferences: CookiePreferences) {
  window.localStorage.setItem(COOKIE_CONSENT_KEY, status);
  window.localStorage.setItem(COOKIE_PREFERENCES_KEY, JSON.stringify(preferences));
}

export function openCookieSettings() {
  window.dispatchEvent(new CustomEvent(COOKIE_SETTINGS_EVENT));
}
