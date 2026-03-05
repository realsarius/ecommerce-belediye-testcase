import { useRef, useState } from 'react';
import { ImageIcon, Loader2, Upload } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/common/button';
import { Label } from '@/components/common/label';
import { useConfirmMediaUploadMutation, usePresignMediaUploadMutation } from '@/features/media/mediaApi';
import type { ConfirmMediaUploadResult, MediaUploadContext } from '@/features/media/types';
import { uploadToPresignedUrl } from '@/lib/uploadToPresignedUrl';
import { cn } from '@/lib/utils';

const ALLOWED_CONTENT_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'] as const;
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
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const isDisabled = disabled || !referenceId;

  const handleSelectFile = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    if (!referenceId) {
      toast.error(disabledMessage || 'Yükleme için geçerli bir kayıt bulunamadı');
      return;
    }

    if (!ALLOWED_CONTENT_TYPES.includes(file.type as (typeof ALLOWED_CONTENT_TYPES)[number])) {
      toast.error('Sadece JPEG, PNG, WebP veya GIF yükleyebilirsiniz');
      return;
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      toast.error('Dosya boyutu 10 MB sınırını aşıyor');
      return;
    }

    setIsUploading(true);
    setProgress(0);

    try {
      const presigned = await presignUpload({
        context,
        referenceId,
        contentType: file.type,
        fileSizeBytes: file.size,
      }).unwrap();

      await uploadToPresignedUrl(presigned.uploadUrl, file, setProgress);

      const confirmed = await confirmUpload({
        context,
        referenceId,
        objectKey: presigned.objectKey,
      }).unwrap();

      setProgress(100);
      onUploaded?.(confirmed);
      toast.success('Görsel yüklendi');
    } catch (error) {
      const message = (error as { data?: { message?: string } })?.data?.message;
      toast.error(message || 'Görsel yüklenemedi');
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
      window.setTimeout(() => setProgress(null), 1200);
    }
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
        {isDisabled && disabledMessage ? <p className="text-xs text-muted-foreground">{disabledMessage}</p> : null}
      </div>
    </div>
  );
}
