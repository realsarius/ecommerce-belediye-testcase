import { useMemo, useRef, useState } from 'react';
import {
  closestCenter,
  DndContext,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext,
  arrayMove,
  rectSortingStrategy,
  useSortable,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { GripVertical, Loader2, RotateCcw, Star, Trash2, Upload } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/common/button';
import { Label } from '@/components/common/label';
import { useConfirmMediaUploadMutation, useDeleteMediaImageMutation, usePresignMediaUploadMutation, useReorderProductImagesMutation } from '@/features/media/mediaApi';
import { uploadToPresignedUrl } from '@/lib/uploadToPresignedUrl';
import { cn } from '@/lib/utils';

const ALLOWED_CONTENT_TYPES = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'] as const;
const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

export type ProductImageFormValue = {
  id?: number;
  imageUrl: string;
  objectKey?: string;
  sortOrder: number;
  isPrimary: boolean;
};

type SortableImageItem = ProductImageFormValue & { sortableId: string };

type FailedUpload = {
  id: string;
  file: File;
  message: string;
};

type ProductImageUploaderProps = {
  productId?: number;
  canUpload: boolean;
  images: ProductImageFormValue[];
  onChange: (images: ProductImageFormValue[]) => void;
  maxFiles?: number;
};

function normalizeImages(images: ProductImageFormValue[]): ProductImageFormValue[] {
  const sorted = [...images].sort((a, b) => a.sortOrder - b.sortOrder);
  const primaryIndex = sorted.findIndex((image) => image.isPrimary);
  const normalizedPrimaryIndex = primaryIndex >= 0 ? primaryIndex : 0;

  return sorted.map((image, index) => ({
    ...image,
    sortOrder: index,
    isPrimary: sorted.length > 0 ? index === normalizedPrimaryIndex : false,
  }));
}

function createSortableItems(images: ProductImageFormValue[]): SortableImageItem[] {
  return images.map((image, index) => ({
    ...image,
    sortableId: image.id ? `id:${image.id}` : `tmp:${index}:${image.imageUrl}`,
  }));
}

function buildUploadId(file: File): string {
  return `${file.name}-${file.size}-${file.lastModified}`;
}

function getApiErrorMessage(error: unknown, fallbackMessage: string): string {
  return (error as { data?: { message?: string } })?.data?.message ?? fallbackMessage;
}

function SortableImageCard({
  image,
  onSetPrimary,
  onDelete,
  disabled,
  isDeleting,
  isSettingPrimary,
}: {
  image: SortableImageItem;
  onSetPrimary: () => void;
  onDelete: () => void;
  disabled: boolean;
  isDeleting: boolean;
  isSettingPrimary: boolean;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: image.sortableId, disabled });

  return (
    <div
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      className={cn(
        'relative overflow-hidden rounded-xl border border-border/70 bg-muted/20',
        isDragging && 'opacity-70 ring-2 ring-primary/40',
        disabled && 'pointer-events-none opacity-80',
      )}
    >
      <div className="aspect-square overflow-hidden bg-muted">
        <img src={image.imageUrl} alt="Ürün görseli" className="h-full w-full object-cover" />
      </div>

      <div className="absolute left-2 top-2 flex items-center gap-1">
        <button
          type="button"
          className="rounded-md bg-black/70 p-1 text-white"
          aria-label="Görseli sürükle"
          disabled={disabled}
          {...attributes}
          {...listeners}
        >
          <GripVertical className="h-4 w-4" />
        </button>
        {image.isPrimary ? (
          <span className="rounded-md bg-amber-500/90 px-2 py-1 text-xs font-medium text-black">
            Ana Görsel
          </span>
        ) : null}
      </div>

      <div className="flex items-center gap-2 p-2">
        <Button
          type="button"
          variant={image.isPrimary ? 'secondary' : 'outline'}
          size="sm"
          className="flex-1"
          disabled={disabled}
          onClick={onSetPrimary}
        >
          {isSettingPrimary ? <Loader2 className="h-4 w-4 animate-spin" /> : <Star className="h-4 w-4" />}
          {image.isPrimary ? 'Ana' : 'Ana Yap'}
        </Button>
        <Button type="button" variant="destructive" size="sm" disabled={disabled} onClick={onDelete} aria-label="Görseli sil">
          {isDeleting ? <Loader2 className="h-4 w-4 animate-spin" /> : <Trash2 className="h-4 w-4" />}
        </Button>
      </div>
    </div>
  );
}

export function ProductImageUploader({
  productId,
  canUpload,
  images,
  onChange,
  maxFiles = 8,
}: ProductImageUploaderProps) {
  const [presignUpload] = usePresignMediaUploadMutation();
  const [confirmUpload] = useConfirmMediaUploadMutation();
  const [deleteImageMutation] = useDeleteMediaImageMutation();
  const [reorderImagesMutation] = useReorderProductImagesMutation();

  const [isUploading, setIsUploading] = useState(false);
  const [uploadStatus, setUploadStatus] = useState<Record<string, { fileName: string; progress: number }>>({});
  const [failedUploads, setFailedUploads] = useState<FailedUpload[]>([]);
  const [isReordering, setIsReordering] = useState(false);
  const [activeDeleteSortableId, setActiveDeleteSortableId] = useState<string | null>(null);
  const [activePrimarySortableId, setActivePrimarySortableId] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const isMutating = isUploading || isReordering || activeDeleteSortableId !== null || activePrimarySortableId !== null;

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 8 } }));
  const sortableItems = useMemo(() => createSortableItems(images), [images]);
  const remainingCount = Math.max(0, maxFiles - images.length);

  const removeUploadStatus = (uploadId: string) => {
    setUploadStatus((current) => {
      if (!current[uploadId]) {
        return current;
      }

      const next = { ...current };
      delete next[uploadId];
      return next;
    });
  };

  const syncReorderIfPossible = async (nextImages: ProductImageFormValue[]) => {
    if (!productId) {
      return;
    }

    const allImageIds = nextImages.every((image) => typeof image.id === 'number' && image.id > 0);
    if (!allImageIds) {
      return;
    }

    setIsReordering(true);
    try {
      await reorderImagesMutation({
        productId,
        data: {
          imageOrders: nextImages.map((image, index) => ({
            imageId: image.id as number,
            displayOrder: index,
            isPrimary: image.isPrimary,
          })),
        },
      }).unwrap();
    } catch {
      toast.error('Görsel sıralaması kaydedilemedi');
    } finally {
      setIsReordering(false);
    }
  };

  const validateFile = (file: File): string | null => {
    if (!ALLOWED_CONTENT_TYPES.includes(file.type as (typeof ALLOWED_CONTENT_TYPES)[number])) {
      return 'Sadece JPEG, PNG, WebP veya GIF yükleyebilirsiniz';
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      return 'Dosya boyutu 10 MB sınırını aşıyor';
    }

    return null;
  };

  const uploadSingleFile = async (
    file: File,
    currentImages: ProductImageFormValue[],
    uploadId: string,
  ): Promise<{ success: true; images: ProductImageFormValue[] } | { success: false; images: ProductImageFormValue[]; message: string }> => {
    const validationError = validateFile(file);
    if (validationError) {
      removeUploadStatus(uploadId);
      return { success: false, images: currentImages, message: validationError };
    }

    if (!productId) {
      removeUploadStatus(uploadId);
      return { success: false, images: currentImages, message: 'Dosya yüklemek için ürünü önce kaydedin' };
    }

    setUploadStatus((current) => ({ ...current, [uploadId]: { fileName: file.name, progress: 0 } }));

    try {
      const presigned = await presignUpload({
        context: 'product',
        referenceId: productId,
        contentType: file.type,
        fileSizeBytes: file.size,
      }).unwrap();

      await uploadToPresignedUrl(presigned.uploadUrl, file, (progress) => {
        setUploadStatus((current) => ({ ...current, [uploadId]: { fileName: file.name, progress } }));
      });

      const confirmed = await confirmUpload({
        context: 'product',
        referenceId: productId,
        objectKey: presigned.objectKey,
        isPrimary: currentImages.length === 0,
        sortOrder: currentImages.length,
      }).unwrap();

      setUploadStatus((current) => ({ ...current, [uploadId]: { fileName: file.name, progress: 100 } }));

      const nextImages = normalizeImages([
        ...currentImages,
        {
          id: confirmed.imageId,
          imageUrl: confirmed.imageUrl,
          objectKey: confirmed.objectKey,
          isPrimary: !!confirmed.isPrimary,
          sortOrder: confirmed.sortOrder ?? currentImages.length,
        },
      ]);

      return { success: true, images: nextImages };
    } catch (error) {
      removeUploadStatus(uploadId);
      return {
        success: false,
        images: currentImages,
        message: getApiErrorMessage(error, 'Dosya yükleme sırasında hata oluştu'),
      };
    }
  };

  const handleSetPrimary = async (sortableId: string) => {
    if (isMutating) {
      return;
    }

    setActivePrimarySortableId(sortableId);
    try {
      const next = normalizeImages(
        sortableItems.map((item) => ({
          id: item.id,
          imageUrl: item.imageUrl,
          objectKey: item.objectKey,
          sortOrder: item.sortOrder,
          isPrimary: item.sortableId === sortableId,
        })),
      );

      onChange(next);
      await syncReorderIfPossible(next);
    } finally {
      setActivePrimarySortableId(null);
    }
  };

  const handleDelete = async (sortableId: string) => {
    if (isMutating) {
      return;
    }

    const target = sortableItems.find((item) => item.sortableId === sortableId);
    if (!target) {
      return;
    }

    setActiveDeleteSortableId(sortableId);
    try {
      if (target.id && canUpload) {
        try {
          await deleteImageMutation(target.id).unwrap();
        } catch {
          toast.error('Görsel silinemedi');
          return;
        }
      }

      const next = normalizeImages(
        sortableItems
          .filter((item) => item.sortableId !== sortableId)
          .map((item) => ({
            id: item.id,
            imageUrl: item.imageUrl,
            objectKey: item.objectKey,
            sortOrder: item.sortOrder,
            isPrimary: item.isPrimary,
          })),
      );

      onChange(next);
      await syncReorderIfPossible(next);
    } finally {
      setActiveDeleteSortableId(null);
    }
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    if (isMutating) {
      return;
    }

    const { active, over } = event;
    if (!over || active.id === over.id) {
      return;
    }

    const oldIndex = sortableItems.findIndex((item) => item.sortableId === active.id);
    const newIndex = sortableItems.findIndex((item) => item.sortableId === over.id);

    if (oldIndex < 0 || newIndex < 0) {
      return;
    }

    // Drag sonrası yeni sırayı index bazlı yeniden numaralandırıyoruz.
    // Eski sortOrder değerlerini korursak normalize adımı öğeleri tekrar eski yerine dizer.
    const reordered = arrayMove(sortableItems, oldIndex, newIndex).map((item, index) => ({
      id: item.id,
      imageUrl: item.imageUrl,
      objectKey: item.objectKey,
      sortOrder: index,
      isPrimary: item.isPrimary,
    }));

    const normalized = normalizeImages(reordered);
    onChange(normalized);
    await syncReorderIfPossible(normalized);
  };

  const handleSelectFiles = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(event.target.files ?? []);
    if (files.length === 0) {
      return;
    }

    if (!canUpload || !productId) {
      toast.info('Dosya yüklemek için ürünü önce kaydedin');
      return;
    }

    if (files.length > remainingCount) {
      toast.error(`En fazla ${remainingCount} dosya daha yükleyebilirsiniz`);
      return;
    }

    setIsUploading(true);

    try {
      let nextImages = [...images];
      let uploadedCount = 0;
      const nextFailedUploads: FailedUpload[] = [];
      const attemptedUploadIds = new Set<string>();

      for (const file of files) {
        const uploadId = buildUploadId(file);
        attemptedUploadIds.add(uploadId);

        const result = await uploadSingleFile(file, nextImages, uploadId);
        if (result.success) {
          nextImages = result.images;
          uploadedCount += 1;
          continue;
        }

        nextFailedUploads.push({ id: uploadId, file, message: result.message });
      }

      onChange(nextImages);

      setFailedUploads((current) => {
        const untouched = current.filter((item) => !attemptedUploadIds.has(item.id));
        return [...untouched, ...nextFailedUploads];
      });

      if (uploadedCount > 0) {
        toast.success(`${uploadedCount} görsel yüklendi`);
      }

      if (nextFailedUploads.length > 0) {
        toast.error(`${nextFailedUploads.length} görsel yüklenemedi, tekrar deneyebilirsiniz`);
      }
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
      window.setTimeout(() => setUploadStatus({}), 1200);
    }
  };

  const handleRetryUpload = async (uploadId: string) => {
    const failedUpload = failedUploads.find((item) => item.id === uploadId);
    if (!failedUpload) {
      return;
    }

    if (!canUpload || !productId) {
      toast.info('Dosya yüklemek için ürünü önce kaydedin');
      return;
    }

    setIsUploading(true);
    try {
      const result = await uploadSingleFile(failedUpload.file, [...images], uploadId);
      if (!result.success) {
        setFailedUploads((current) =>
          current.map((item) => (item.id === uploadId ? { ...item, message: result.message } : item)),
        );
        toast.error(result.message);
        return;
      }

      onChange(result.images);
      setFailedUploads((current) => current.filter((item) => item.id !== uploadId));
      toast.success('Görsel tekrar yükleme ile eklendi');
    } finally {
      setIsUploading(false);
      window.setTimeout(() => setUploadStatus({}), 1200);
    }
  };

  const handleRetryAll = async () => {
    if (failedUploads.length === 0) {
      return;
    }

    if (!canUpload || !productId) {
      toast.info('Dosya yüklemek için ürünü önce kaydedin');
      return;
    }

    setIsUploading(true);
    try {
      let nextImages = [...images];
      let uploadedCount = 0;
      const remainingFailures: FailedUpload[] = [];

      for (const failedUpload of failedUploads) {
        const result = await uploadSingleFile(failedUpload.file, nextImages, failedUpload.id);
        if (!result.success) {
          remainingFailures.push({ ...failedUpload, message: result.message });
          continue;
        }

        nextImages = result.images;
        uploadedCount += 1;
      }

      onChange(nextImages);
      setFailedUploads(remainingFailures);

      if (uploadedCount > 0) {
        toast.success(`${uploadedCount} görsel tekrar denemede yüklendi`);
      }

      if (remainingFailures.length > 0) {
        toast.error(`${remainingFailures.length} görsel hala yüklenemedi`);
      }
    } finally {
      setIsUploading(false);
      window.setTimeout(() => setUploadStatus({}), 1200);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <Label className="text-base font-semibold">Ürün Görselleri</Label>
          <p className="text-sm text-muted-foreground">
            {canUpload
              ? 'Dosya seçip R2 storage alanına yükleyebilir, sürükleyip sıralayabilir ve ana görsel belirleyebilirsiniz'
              : 'Yeni ürün için önce kaydetme yapın, ardından düzenleme ekranında dosya yükleyin'}
          </p>
          {isReordering ? <p className="text-xs text-muted-foreground">Sıralama kaydediliyor</p> : null}
        </div>
        <div className="flex items-center gap-2">
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp,image/gif"
            multiple
            className="hidden"
            onChange={(event) => void handleSelectFiles(event)}
          />
          <Button
            type="button"
            variant="outline"
            disabled={!canUpload || isMutating || remainingCount === 0}
            onClick={() => fileInputRef.current?.click()}
          >
            {isUploading ? <Loader2 className="h-4 w-4 animate-spin" /> : <Upload className="h-4 w-4" />}
            Dosya Yükle
          </Button>
          <span className="text-xs text-muted-foreground">{images.length}/{maxFiles}</span>
        </div>
      </div>

      {!canUpload ? (
        <div className="rounded-xl border border-dashed border-border/70 bg-muted/20 p-4">
          <p className="text-sm text-muted-foreground">
            Görsel yüklemek için ürünü önce oluşturmanız gerekiyor
          </p>
        </div>
      ) : null}

      {Object.keys(uploadStatus).length > 0 ? (
        <div className="space-y-2 rounded-xl border border-border/70 bg-muted/20 p-3">
          {Object.entries(uploadStatus).map(([uploadId, status]) => (
            <div key={uploadId} className="space-y-1">
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span className="truncate">{status.fileName}</span>
                <span>%{status.progress}</span>
              </div>
              <div className="h-2 rounded bg-muted">
                <div className="h-2 rounded bg-primary transition-all" style={{ width: `${status.progress}%` }} />
              </div>
            </div>
          ))}
        </div>
      ) : null}

      {failedUploads.length > 0 ? (
        <div className="space-y-2 rounded-xl border border-destructive/50 bg-destructive/5 p-3">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <p className="text-sm font-medium text-destructive">Yüklenemeyen dosyalar</p>
            <Button type="button" variant="outline" size="sm" disabled={isMutating} onClick={() => void handleRetryAll()}>
              <RotateCcw className="h-4 w-4" />
              Tümünü Tekrar Dene
            </Button>
          </div>
          {failedUploads.map((failedUpload) => (
            <div key={failedUpload.id} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-border/60 bg-background/70 px-3 py-2 text-sm">
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium">{failedUpload.file.name}</p>
                <p className="text-xs text-muted-foreground">{failedUpload.message}</p>
              </div>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                disabled={isMutating}
                onClick={() => void handleRetryUpload(failedUpload.id)}
              >
                <RotateCcw className="h-4 w-4" />
                Tekrar Dene
              </Button>
            </div>
          ))}
        </div>
      ) : null}

      {sortableItems.length === 0 ? (
        <div className="rounded-xl border border-dashed border-border/70 p-6 text-sm text-muted-foreground">
          Henüz görsel eklenmedi
        </div>
      ) : (
        <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={(event) => void handleDragEnd(event)}>
          <SortableContext items={sortableItems.map((item) => item.sortableId)} strategy={rectSortingStrategy}>
            <div className="grid grid-cols-2 gap-3 md:grid-cols-3 xl:grid-cols-4">
              {sortableItems.map((item) => (
                <SortableImageCard
                  key={item.sortableId}
                  image={item}
                  disabled={isMutating}
                  isDeleting={activeDeleteSortableId === item.sortableId}
                  isSettingPrimary={activePrimarySortableId === item.sortableId}
                  onSetPrimary={() => void handleSetPrimary(item.sortableId)}
                  onDelete={() => void handleDelete(item.sortableId)}
                />
              ))}
            </div>
          </SortableContext>
        </DndContext>
      )}
    </div>
  );
}
