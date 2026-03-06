import { useRef, useState } from 'react';
import { ImageIcon, Loader2, RotateCcw, Upload } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/common/button';
import { Label } from '@/components/common/label';
import { useConfirmMediaUploadMutation, usePresignMediaUploadMutation } from '@/features/media/mediaApi';
import type { ConfirmMediaUploadResult, MediaUploadContext } from '@/features/media/types';
import { optimizeImageForUpload } from '@/lib/optimizeImageForUpload';
import { uploadToPresignedUrl } from '@/lib/uploadToPresignedUrl';
import { cn } from '@/lib/utils';

const ALLOWED_CONTENT_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'] as const;
const OPTIMIZABLE_CONTENT_TYPES = ['image/jpeg', 'image/png'] as const;
const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

type SingleMediaUploaderProps = {
  title: string;
  description?: string;
  context: MediaUploadContext;
  referenceId?: number;
  imageUrl?: string | null;
  onUploaded?: (result: ConfirmMediaUploadResult) => void;
  disabled?: boolean;
  disabledMessage?: string;
  className?: string;
};

type FailedSingleUpload = {
  file: File;
  message: string;
};

function getApiErrorMessage(error: unknown, fallbackMessage: string): string {
  return (error as { data?: { message?: string } })?.data?.message ?? fallbackMessage;
}

export function SingleMediaUploader({
  title,
  description,
  context,
  referenceId,
  imageUrl,
  onUploaded,
  disabled = false,
  disabledMessage,
  className,
}: SingleMediaUploaderProps) {
  const [presignUpload] = usePresignMediaUploadMutation();
  const [confirmUpload] = useConfirmMediaUploadMutation();
  const [isUploading, setIsUploading] = useState(false);
  const [progress, setProgress] = useState<number | null>(null);
  const [failedUpload, setFailedUpload] = useState<FailedSingleUpload | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const isDisabled = disabled || !referenceId;

  const uploadFile = async (file: File): Promise<boolean> => {
    if (!referenceId) {
      toast.error(disabledMessage || 'Yükleme için geçerli bir kayıt bulunamadı');
      return false;
    }

    if (!ALLOWED_CONTENT_TYPES.includes(file.type as (typeof ALLOWED_CONTENT_TYPES)[number])) {
      const message = 'Sadece JPEG, PNG, WebP veya GIF yükleyebilirsiniz';
      toast.error(message);
      setFailedUpload({ file, message });
      return false;
    }

    if (
      file.size > MAX_FILE_SIZE_BYTES &&
      !OPTIMIZABLE_CONTENT_TYPES.includes(file.type as (typeof OPTIMIZABLE_CONTENT_TYPES)[number])
    ) {
      const message = 'Dosya boyutu 10 MB sınırını aşıyor';
      toast.error(message);
      setFailedUpload({ file, message });
      return false;
    }

    const optimizedFile = await optimizeImageForUpload(file);
    if (optimizedFile.size > MAX_FILE_SIZE_BYTES) {
      const message = 'Dosya boyutu 10 MB sınırını aşıyor';
      toast.error(message);
      setFailedUpload({ file, message });
      return false;
    }

    setIsUploading(true);
    setProgress(0);

    try {
      const presigned = await presignUpload({
        context,
        referenceId,
        contentType: optimizedFile.type,
        fileSizeBytes: optimizedFile.size,
      }).unwrap();

      await uploadToPresignedUrl(presigned.uploadUrl, optimizedFile, setProgress);

      const confirmed = await confirmUpload({
        context,
        referenceId,
        objectKey: presigned.objectKey,
      }).unwrap();

      setProgress(100);
      setFailedUpload(null);
      onUploaded?.(confirmed);
      toast.success('Görsel yüklendi');
      return true;
    } catch (error) {
      const message = getApiErrorMessage(error, 'Görsel yüklenemedi');
      setFailedUpload({ file, message });
      toast.error(message);
      return false;
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
      window.setTimeout(() => setProgress(null), 1200);
    }
  };

  const handleSelectFile = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    await uploadFile(file);
  };

  const handleRetry = async () => {
    if (!failedUpload) {
      return;
    }

    await uploadFile(failedUpload.file);
  };

  return (
    <div className={cn('space-y-3 rounded-xl border border-border/70 bg-muted/20 p-4', className)}>
      <div className="space-y-1">
        <Label className="text-sm font-semibold">{title}</Label>
        {description ? <p className="text-xs text-muted-foreground">{description}</p> : null}
      </div>

      <div className="overflow-hidden rounded-lg border border-border/60 bg-background">
        {imageUrl ? (
          <img src={imageUrl} alt={title} className="h-36 w-full object-cover" />
        ) : (
          <div className="flex h-36 w-full items-center justify-center text-muted-foreground">
            <ImageIcon className="h-5 w-5" />
          </div>
        )}
      </div>

      {progress !== null ? (
        <div className="space-y-1">
          <div className="flex items-center justify-between text-xs text-muted-foreground">
            <span>Yükleniyor</span>
            <span>%{progress}</span>
          </div>
          <div className="h-2 rounded bg-muted">
            <div className="h-2 rounded bg-primary transition-all" style={{ width: `${progress}%` }} />
          </div>
        </div>
      ) : null}

      {failedUpload ? (
        <div className="space-y-2 rounded-lg border border-destructive/50 bg-destructive/5 px-3 py-2">
          <p className="text-xs font-medium text-destructive">{failedUpload.message}</p>
          <p className="truncate text-xs text-muted-foreground">{failedUpload.file.name}</p>
        </div>
      ) : null}

      <div className="flex flex-wrap items-center gap-2">
        <input
          ref={fileInputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp,image/gif"
          className="hidden"
          onChange={(event) => void handleSelectFile(event)}
        />
        <Button
          type="button"
          variant="outline"
          disabled={isDisabled || isUploading}
          onClick={() => fileInputRef.current?.click()}
        >
          {isUploading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />}
          {imageUrl ? 'Görseli Değiştir' : 'Görsel Yükle'}
        </Button>
        <Button type="button" variant="secondary" disabled={isDisabled || isUploading || !failedUpload} onClick={() => void handleRetry()}>
          {isUploading ? <Loader2 className="h-4 w-4 animate-spin" /> : <RotateCcw className="h-4 w-4" />}
          Tekrar Dene
        </Button>
        {isDisabled && disabledMessage ? <p className="text-xs text-muted-foreground">{disabledMessage}</p> : null}
      </div>
    </div>
  );
}
