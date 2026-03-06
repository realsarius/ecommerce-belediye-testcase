const OPTIMIZABLE_CONTENT_TYPES = new Set(['image/jpeg', 'image/png']);
const WEBP_CONTENT_TYPE = 'image/webp';
const MAX_DIMENSION = 1920;
const WEBP_QUALITY = 0.82;
const MIN_SOURCE_SIZE_BYTES = 300 * 1024;
const MIN_REQUIRED_SAVING_BYTES = 16 * 1024;

function replaceExtension(fileName: string, extension: string): string {
  const trimmed = fileName.trim();
  if (!trimmed) {
    return `upload.${extension}`;
  }

  const lastDot = trimmed.lastIndexOf('.');
  if (lastDot <= 0) {
    return `${trimmed}.${extension}`;
  }

  return `${trimmed.slice(0, lastDot)}.${extension}`;
}

function getTargetSize(width: number, height: number): { width: number; height: number } {
  if (width <= 0 || height <= 0) {
    return { width: 0, height: 0 };
  }

  const scale = Math.min(1, MAX_DIMENSION / width, MAX_DIMENSION / height);
  return {
    width: Math.max(1, Math.round(width * scale)),
    height: Math.max(1, Math.round(height * scale)),
  };
}

function loadImage(file: File): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const objectUrl = URL.createObjectURL(file);
    const image = new Image();

    image.onload = () => {
      URL.revokeObjectURL(objectUrl);
      resolve(image);
    };

    image.onerror = () => {
      URL.revokeObjectURL(objectUrl);
      reject(new Error('Image decode failed'));
    };

    image.src = objectUrl;
  });
}

function canvasToBlob(canvas: HTMLCanvasElement, type: string, quality: number): Promise<Blob | null> {
  return new Promise((resolve) => {
    canvas.toBlob((blob) => resolve(blob), type, quality);
  });
}

export async function optimizeImageForUpload(file: File): Promise<File> {
  if (!OPTIMIZABLE_CONTENT_TYPES.has(file.type)) {
    return file;
  }

  if (file.size < MIN_SOURCE_SIZE_BYTES) {
    return file;
  }

  if (typeof window === 'undefined' || typeof document === 'undefined') {
    return file;
  }

  try {
    const image = await loadImage(file);
    const sourceWidth = image.naturalWidth || image.width;
    const sourceHeight = image.naturalHeight || image.height;
    const target = getTargetSize(sourceWidth, sourceHeight);

    if (target.width <= 0 || target.height <= 0) {
      return file;
    }

    const canvas = document.createElement('canvas');
    canvas.width = target.width;
    canvas.height = target.height;

    const context = canvas.getContext('2d');
    if (!context) {
      return file;
    }

    context.drawImage(image, 0, 0, target.width, target.height);

    const blob = await canvasToBlob(canvas, WEBP_CONTENT_TYPE, WEBP_QUALITY);
    if (!blob || blob.type !== WEBP_CONTENT_TYPE || blob.size <= 0) {
      return file;
    }

    const resized = target.width !== sourceWidth || target.height !== sourceHeight;
    const savedBytes = file.size - blob.size;

    if (!resized && savedBytes < MIN_REQUIRED_SAVING_BYTES) {
      return file;
    }

    return new File([blob], replaceExtension(file.name, 'webp'), {
      type: WEBP_CONTENT_TYPE,
      lastModified: Date.now(),
    });
  } catch {
    return file;
  }
}
