import httpClient from './http';
import type { CloudDriveImagePreviewDto, CloudDriveDocPreviewDto } from '@/types';
import { normalizeDocRenderMode } from '@/utils/enumNormalizers';

export async function getImagePreview(
  cloudAccountUid: string,
  fileId: string,
): Promise<CloudDriveImagePreviewDto> {
  const response = await httpClient.get<CloudDriveImagePreviewDto>(
    `/files/${fileId}/preview/image`,
    { params: { cloudAccountUid } },
  );
  return response.data;
}

interface RawDocPreviewResponse {
  previewUrl: string;
  fileName: string;
  fileType: string;
  renderMode: string | number;
  thirdPartyPreviewUrl?: string;
  isTemporary: boolean;
}

export async function getDocPreview(
  cloudAccountUid: string,
  fileId: string,
): Promise<CloudDriveDocPreviewDto> {
  const response = await httpClient.get<RawDocPreviewResponse>(
    `/files/${fileId}/preview/document`,
    { params: { cloudAccountUid } },
  );
  const raw = response.data;
  return {
    ...raw,
    renderMode: normalizeDocRenderMode(raw.renderMode),
  };
}
