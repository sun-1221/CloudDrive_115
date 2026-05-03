import httpClient from './http';
import type { GetJobListInput, CloudDriveJobDto, PagedResult } from '@/types';
import { normalizeJobType, normalizeJobStatus } from '@/utils/enumNormalizers';

interface RawJobDto {
  id: string;
  jobType: string | number;
  status: string | number;
  cloudAccountUid: string;
  resourceId?: string;
  progress: number;
  errorMessage?: string;
  startTime?: string;
  endTime?: string;
}

function normalizeJobDto(raw: RawJobDto): CloudDriveJobDto {
  return {
    ...raw,
    jobType: normalizeJobType(raw.jobType),
    status: normalizeJobStatus(raw.status),
  };
}

export async function getJobList(input: GetJobListInput): Promise<PagedResult<CloudDriveJobDto>> {
  const response = await httpClient.get<PagedResult<RawJobDto>>('/jobs', {
    params: {
      cloudAccountUid: input.cloudAccountUid,
      status: input.status,
      offset: input.offset,
      limit: input.limit,
    },
  });

  return {
    ...response.data,
    items: response.data.items.map(normalizeJobDto),
  };
}

export async function getJobDetail(jobId: string): Promise<CloudDriveJobDto> {
  const response = await httpClient.get<RawJobDto>(`/jobs/${jobId}`);
  return normalizeJobDto(response.data);
}
