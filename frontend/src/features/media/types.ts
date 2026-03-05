export type MediaUploadContext = 'product' | 'category' | 'seller-logo' | 'seller-banner';

export interface PresignMediaUploadRequest {
  context: MediaUploadContext;
  referenceId: number;
  contentType: string;
  fileSizeBytes: number;
}

export interface PresignedMediaUpload {
  uploadUrl: string;
  publicUrl: string;
  objectKey: string;
}

export interface ConfirmMediaUploadRequest {
  context: MediaUploadContext;
  referenceId: number;
  objectKey: string;
  isPrimary?: boolean;
  sortOrder?: number;
}

export interface ConfirmMediaUploadResult {
  imageId?: number;
  imageUrl: string;
  objectKey: string;
  isPrimary?: boolean;
  sortOrder?: number;
}

export interface ReorderProductImageItemRequest {
  imageId: number;
  displayOrder: number;
  isPrimary: boolean;
}

export interface ReorderProductImagesRequest {
  imageOrders: ReorderProductImageItemRequest[];
}
