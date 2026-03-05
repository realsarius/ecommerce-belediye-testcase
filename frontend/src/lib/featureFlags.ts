function parseBooleanEnv(value: string | undefined) {
  if (!value) {
    return null;
  }

  const normalized = value.trim().toLowerCase();
  if (['1', 'true', 'yes', 'on'].includes(normalized)) {
    return true;
  }

  if (['0', 'false', 'no', 'off'].includes(normalized)) {
    return false;
  }

  return null;
}

const configuredCheatCodesFlag = parseBooleanEnv(import.meta.env.VITE_ENABLE_CHEAT_CODES as string | undefined);

// Cheat kodlari dev ortaminda varsayilan acik, production'da varsayilan kapali.
export const isCheatCodesEnabled = configuredCheatCodesFlag ?? import.meta.env.DEV;
