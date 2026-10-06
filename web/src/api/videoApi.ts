import httpClient from './http';
import type {
  GetVideoPlayInput,
  CloudDriveVideoPlayDto,
  CloudDriveSubtitleDto,
  CloudDriveVideoHistoryDto,
  ListResult,
} from '@/types';
import { normalizeVideoStatus } from '@/utils/enumNormalizers';

export async function getPlayUrl(input: GetVideoPlayInput): Promise<CloudDriveVideoPlayDto> {
  const response = await httpClient.get<CloudDriveVideoPlayDto>(
    `/videos/${input.pickCode}/play`,
    { params: { definition: input.definition } },
  );
  const raw = response.data;
  return { ...raw, status: normalizeVideoStatus(raw.status), qualityOptions: raw.qualityOptions ?? [], audioTracks: raw.audioTracks ?? [] };
}

export async function getHistory(pickCode: string): Promise<CloudDriveVideoHistoryDto> {
  const response = await httpClient.get<CloudDriveVideoHistoryDto>(`/videos/${pickCode}/history`);
  return response.data;
}

export async function getSubtitles(pickCode: string): Promise<{ items: CloudDriveSubtitleDto[]; itemSchemaStatus: 'available' | 'not_collected' }> {
  const response = await httpClient.get<ListResult<CloudDriveSubtitleDto>>(
    `/videos/${pickCode}/subtitles`,
  );
  return { items: response.data.items ?? [], itemSchemaStatus: response.data.itemSchemaStatus ?? 'not_collected' };
}
