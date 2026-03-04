export function buildTrackingUrl(cargoCompany?: string, trackingCode?: string) {
  if (!cargoCompany || !trackingCode) {
    return null;
  }

  const normalizedCompany = cargoCompany.toLocaleLowerCase('tr-TR');

  if (normalizedCompany.includes('yurti')) {
    return `https://www.yurticikargo.com/tr/online-servisler/gonderi-sorgula?code=${encodeURIComponent(trackingCode)}`;
  }

  if (normalizedCompany.includes('aras')) {
    return `https://www.araskargo.com.tr/kargo-takip/${encodeURIComponent(trackingCode)}`;
  }

  if (normalizedCompany.includes('mng')) {
    return `https://www.mngkargo.com.tr/gonderitakip/${encodeURIComponent(trackingCode)}`;
  }

  if (normalizedCompany.includes('ptt')) {
    return `https://gonderitakip.ptt.gov.tr/Track/Verify?q=${encodeURIComponent(trackingCode)}`;
  }

  if (normalizedCompany.includes('sürat') || normalizedCompany.includes('surat')) {
    return `https://www.suratkargo.com.tr/KargoTakip/?query=${encodeURIComponent(trackingCode)}`;
  }

  if (normalizedCompany.includes('ups')) {
    return `https://www.ups.com/track?tracknum=${encodeURIComponent(trackingCode)}`;
  }

  return null;
}
