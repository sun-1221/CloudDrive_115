import httpClient from './http';
import type {
  GetVideoPlayInput,
  SubmitTranscodeInput,
  CloudDriveVideoPlayDto,
  CloudDriveVideoTranscodeDto,
  CloudDriveSubtitleDto,
  ListResult,
} from '@/types';
import { normalizeVideoStatus } from '@/utils/enumNormalizers';

interface RawVideoPlayResponse {
  status: string | number;
  jobId?: string;
  qualityOptions: { quality: string; playUrl: string }[];
  duration: number;
  playbackMetadata?: Record<string, string>;
  isTemporary: boolean;
}

export async function getPlayUrl(input: GetVideoPlayInput): Promise<CloudDriveVideoPlayDto> {
  const response = await httpClient.post<RawVideoPlayResponse>('/video/play', {
    cloudAccountUid: input.cloudAccountUid,
    fileId: input.fileId,
    quality: input.quality,
  });
  const raw = response.data;
  return {
    ...raw,
    status: normalizeVideoStatus(raw.status),
  };
}

export async function submitTranscode(
  input: SubmitTranscodeInput,
): Promise<CloudDriveVideoTranscodeDto> {
  const response = await httpClient.post<CloudDriveVideoTranscodeDto>('/video/transcode', input);
  return response.data;
}

export async function getSubtitles(
  cloudAccountUid: string,
  fileId: string,
): Promise<CloudDriveSubtitleDto[]> {
  const response = await httpClient.get<ListResult<CloudDriveSubtitleDto>>(
    `/video/${fileId}/subtitles`,
    { params: { cloudAccountUid } },
  );
  return response.data.items;
}
