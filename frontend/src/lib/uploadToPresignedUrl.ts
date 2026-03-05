export function uploadToPresignedUrl(
  uploadUrl: string,
  file: File,
  onProgress?: (progress: number) => void,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();

    xhr.upload.onprogress = (event) => {
      if (!event.lengthComputable || !onProgress) {
        return;
      }

      const progress = Math.round((event.loaded / event.total) * 100);
      onProgress(progress);
    };

    xhr.onerror = () => {
      reject(new Error('Dosya yüklenirken ağ hatası oluştu'));
    };

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve();
        return;
      }

      reject(new Error(`Dosya yükleme başarısız oldu. HTTP ${xhr.status}`));
    };

    xhr.open('PUT', uploadUrl);
    xhr.setRequestHeader('Content-Type', file.type);
    xhr.send(file);
  });
}
