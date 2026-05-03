import type {
  CloudDriveAuthorizationFlowType,
  CloudDriveJobType,
  CloudDriveJobStatus,
} from '@/types';

// ===== 授权流程类型归一化 =====

const flowTypeToString: Record<number, CloudDriveAuthorizationFlowType> = {
  0: 'AuthorizationCode',
  1: 'AuthorizationCodeWithPkce',
};

const flowTypeToNumber: Record<CloudDriveAuthorizationFlowType, number> = {
  AuthorizationCode: 0,
  AuthorizationCodeWithPkce: 1,
};

export function normalizeFlowType(value: number | string): CloudDriveAuthorizationFlowType {
  if (typeof value === 'string') return value as CloudDriveAuthorizationFlowType;
  return flowTypeToString[value] ?? 'AuthorizationCode';
}

export function denormalizeFlowType(value: CloudDriveAuthorizationFlowType): number {
  return flowTypeToNumber[value] ?? 0;
}

// ===== 任务类型归一化 =====

const jobTypeToString: Record<number, CloudDriveJobType> = {
  0: 'Upload',
  1: 'BatchCopy',
  2: 'BatchMove',
  3: 'BatchDelete',
  4: 'ClearRecycleBin',
  5: 'Transcode',
};

const jobTypeToNumber: Record<CloudDriveJobType, number> = {
  Upload: 0,
  BatchCopy: 1,
  BatchMove: 2,
  BatchDelete: 3,
  ClearRecycleBin: 4,
  Transcode: 5,
};

export function normalizeJobType(value: number | string): CloudDriveJobType {
  if (typeof value === 'string') return value as CloudDriveJobType;
  return jobTypeToString[value] ?? 'Upload';
}

export function denormalizeJobType(value: CloudDriveJobType): number {
  return jobTypeToNumber[value] ?? 0;
}

// ===== 任务状态归一化 =====

const jobStatusToString: Record<number, CloudDriveJobStatus> = {
  0: 'Pending',
  1: 'Running',
  2: 'Completed',
  3: 'Failed',
  4: 'Cancelled',
};

const jobStatusToNumber: Record<CloudDriveJobStatus, number> = {
  Pending: 0,
  Running: 1,
  Completed: 2,
  Failed: 3,
  Cancelled: 4,
};

export function normalizeJobStatus(value: number | string): CloudDriveJobStatus {
  if (typeof value === 'string') return value as CloudDriveJobStatus;
  return jobStatusToString[value] ?? 'Pending';
}

export function denormalizeJobStatus(value: CloudDriveJobStatus): number {
  return jobStatusToNumber[value] ?? 0;
}

// ===== 视频状态归一化 =====

type VideoPlayStatus = 'Ready' | 'Transcoding';

const videoStatusToString: Record<number, VideoPlayStatus> = {
  0: 'Ready',
  1: 'Transcoding',
};

export function normalizeVideoStatus(value: number | string): VideoPlayStatus {
  if (typeof value === 'string') return value as VideoPlayStatus;
  return videoStatusToString[value] ?? 'Ready';
}

// ===== 文档渲染模式归一化 =====

type DocRenderMode = 'Direct' | 'ThirdParty' | 'NotAvailable';

const docRenderModeToString: Record<number, DocRenderMode> = {
  0: 'Direct',
  1: 'ThirdParty',
  2: 'NotAvailable',
};

export function normalizeDocRenderMode(value: number | string): DocRenderMode {
  if (typeof value === 'string') return value as DocRenderMode;
  return docRenderModeToString[value] ?? 'NotAvailable';
}
