import httpClient from './http';
import type { CloudDriveImagePreviewDto, CloudDriveDocPreviewDto } from '@/types';
import { normalizeDocRenderMode } from '@/utils/enumNormalizers';

export async function getImagePreview(pickCode: string): Promise<CloudDriveImagePreviewDto> {
  const response = await httpClient.get<CloudDriveImagePreviewDto>(
    `/files/${pickCode}/preview`,
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

export async function getDocPreview(pickCode: string): Promise<CloudDriveDocPreviewDto> {
  const response = await httpClient.get<RawDocPreviewResponse>(
    `/documents/${pickCode}/preview`,
  );
  const raw = response.data;
  return {
    ...raw,
    renderMode: normalizeDocRenderMode(raw.renderMode),
  };
}
