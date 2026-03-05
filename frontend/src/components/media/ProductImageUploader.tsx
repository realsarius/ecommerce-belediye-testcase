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
import { GripVertical, ImagePlus, Loader2, Star, Trash2, Upload } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/common/button';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { useConfirmMediaUploadMutation, useDeleteMediaImageMutation, usePresignMediaUploadMutation, useReorderProductImagesMutation } from '@/features/media/mediaApi';
import { uploadToPresignedUrl } from '@/lib/uploadToPresignedUrl';
import { cn } from '@/lib/utils';

export type ProductImageFormValue = {
  id?: number;
  imageUrl: string;
  objectKey?: string;
  sortOrder: number;
  isPrimary: boolean;
};

type SortableImageItem = ProductImageFormValue & { sortableId: string };

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

function SortableImageCard({
  image,
  onSetPrimary,
  onDelete,
}: {
  image: SortableImageItem;
  onSetPrimary: () => void;
  onDelete: () => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: image.sortableId });

  return (
    <div
      ref={setNodeRef}
      style={{ transform: CSS.Transform.toString(transform), transition }}
      className={cn(
        'relative overflow-hidden rounded-xl border border-border/70 bg-muted/20',
        isDragging && 'opacity-70 ring-2 ring-primary/40',
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
        <Button type="button" variant={image.isPrimary ? 'secondary' : 'outline'} size="sm" className="flex-1" onClick={onSetPrimary}>
          <Star className="h-4 w-4" />
          {image.isPrimary ? 'Ana' : 'Ana Yap'}
        </Button>
        <Button type="button" variant="destructive" size="sm" onClick={onDelete}>
          <Trash2 className="h-4 w-4" />
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
  const [uploadStatus, setUploadStatus] = useState<Record<string, number>>({});
  const [manualUrl, setManualUrl] = useState('');
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 8 } }));
  const sortableItems = useMemo(() => createSortableItems(images), [images]);
  const remainingCount = Math.max(0, maxFiles - images.length);

  const syncReorderIfPossible = async (nextImages: ProductImageFormValue[]) => {
    if (!productId) {
      return;
    }

    const allImageIds = nextImages.every((image) => typeof image.id === 'number' && image.id > 0);
    if (!allImageIds) {
      return;
    }

    try
    {
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
    }
  };

  const handleAddManualUrl = () => {
    const trimmedUrl = manualUrl.trim();
    if (!trimmedUrl) {
      return;
    }

    try {
      // URL formatını doğrula
      new URL(trimmedUrl);
    } catch {
      toast.error('Geçerli bir görsel URL girin');
      return;
    }

    const next = normalizeImages([
      ...images,
      {
        imageUrl: trimmedUrl,
        sortOrder: images.length,
        isPrimary: images.length === 0,
      },
    ]);

    onChange(next);
    setManualUrl('');
  };

  const handleSetPrimary = async (sortableId: string) => {
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
  };

  const handleDelete = async (sortableId: string) => {
    const target = sortableItems.find((item) => item.sortableId === sortableId);
    if (!target) {
      return;
    }

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
  };

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over || active.id === over.id) {
      return;
    }

    const oldIndex = sortableItems.findIndex((item) => item.sortableId === active.id);
    const newIndex = sortableItems.findIndex((item) => item.sortableId === over.id);

    if (oldIndex < 0 || newIndex < 0) {
      return;
    }

    const reordered = arrayMove(sortableItems, oldIndex, newIndex).map((item) => ({
      id: item.id,
      imageUrl: item.imageUrl,
      objectKey: item.objectKey,
      sortOrder: item.sortOrder,
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

      for (const file of files) {
        setUploadStatus((current) => ({ ...current, [file.name]: 0 }));

        const presigned = await presignUpload({
          context: 'product',
          referenceId: productId,
          contentType: file.type || 'application/octet-stream',
          fileSizeBytes: file.size,
        }).unwrap();

        await uploadToPresignedUrl(presigned.uploadUrl, file, (progress) => {
          setUploadStatus((current) => ({ ...current, [file.name]: progress }));
        });

        const confirmed = await confirmUpload({
          context: 'product',
          referenceId: productId,
          objectKey: presigned.objectKey,
          isPrimary: nextImages.length === 0,
          sortOrder: nextImages.length,
        }).unwrap();

        nextImages = normalizeImages([
          ...nextImages,
          {
            id: confirmed.imageId,
            imageUrl: confirmed.imageUrl,
            objectKey: confirmed.objectKey,
            isPrimary: !!confirmed.isPrimary,
            sortOrder: confirmed.sortOrder ?? nextImages.length,
          },
        ]);

        setUploadStatus((current) => ({ ...current, [file.name]: 100 }));
      }

      onChange(nextImages);
      toast.success(`${files.length} görsel yüklendi`);
    } catch (error) {
      const message = (error as { data?: { message?: string } })?.data?.message;
      toast.error(message || 'Dosya yükleme sırasında hata oluştu');
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
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
              : 'Yeni ürün için önce kaydetme yapın veya geçici olarak URL ekleyin'}
          </p>
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
            disabled={!canUpload || isUploading || remainingCount === 0}
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
          <div className="space-y-2">
            <Label htmlFor="manual-image-url">Geçici Görsel URL</Label>
            <div className="flex gap-2">
              <Input
                id="manual-image-url"
                placeholder="https://..."
                value={manualUrl}
                onChange={(event) => setManualUrl(event.target.value)}
              />
              <Button type="button" variant="outline" onClick={handleAddManualUrl}>
                <ImagePlus className="h-4 w-4" />
                Ekle
              </Button>
            </div>
          </div>
        </div>
      ) : null}

      {Object.keys(uploadStatus).length > 0 ? (
        <div className="space-y-2 rounded-xl border border-border/70 bg-muted/20 p-3">
          {Object.entries(uploadStatus).map(([name, progress]) => (
            <div key={name} className="space-y-1">
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span className="truncate">{name}</span>
                <span>%{progress}</span>
              </div>
              <div className="h-2 rounded bg-muted">
                <div className="h-2 rounded bg-primary transition-all" style={{ width: `${progress}%` }} />
              </div>
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
