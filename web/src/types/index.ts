// ===== 通用类型 =====

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  offset: number;
  limit: number;
}

export interface ListResult<T> {
  items: T[];
}

export interface CloudDriveOperationResultDto {
  accepted: boolean;
  jobId?: string;
  message?: string;
}

export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}

// ===== 认证相关 =====

export type CloudDriveAuthorizationFlowType = 'AuthorizationCode' | 'AuthorizationCodeWithPkce';

export interface ExchangeCodeInput {
  code: string;
  flowType?: CloudDriveAuthorizationFlowType;
  pkceSessionId?: string;
  codeVerifier?: string;
}

export interface CloudDrivePkceAuthorizationDto {
  sessionId: string;
  qrCodeContent: string;
  expiresAt: string;
}

export interface CloudDriveTokenDto {
  cloudAccountUid: string;
  accessTokenExpiresAt: string;
  requiresReauthorization: boolean;
}

export interface CloudDriveAccountSummaryDto {
  cloudAccountUid: string;
  nickname?: string;
  isActive: boolean;
  creationTime: string;
}

export interface CloudDriveUserInfoDto {
  cloudAccountUid: string;
  nickname: string;
  avatarUrl?: string;
}

// ===== 文件相关 =====

export interface CloudDriveFileDto {
  fileId: string;
  name: string;
  size: number;
  fileType: string;
  isDirectory: boolean;
  modifiedTime: string;
  thumbnailUrl?: string;
}

export interface CloudDriveFileDetailDto extends CloudDriveFileDto {
  createdTime: string;
  sha1?: string;
  parentId?: string;
  path?: string;
}

export interface CloudDriveSearchResultDto extends CloudDriveFileDto {
  path?: string;
  parentPath?: string;
}

export interface CloudDriveFolderDto {
  folderId: string;
  name: string;
  parentId?: string;
  path?: string;
}

export interface CloudDriveRecycleBinItemDto extends CloudDriveFileDto {
  deletedTime: string;
  originalPath?: string;
}

export interface CloudDriveDownloadDto {
  downloadUrl: string;
  fileName: string;
  fileSize: number;
  isTemporary: boolean;
}

export interface FileQueryCapabilities {
  serverSort: boolean;
  serverFilter: boolean;
}

export type FileListResult = PagedResult<CloudDriveFileDto>;

export interface UploadProgressEvent {
  loaded: number;
  total?: number;
  percentage?: number;
}

export interface UploadQueueItem {
  localId: string;
  cloudAccountUid: string;
  fileName: string;
  size: number;
  directoryId: string;
  status: 'queued' | 'uploading' | 'accepted' | 'completed' | 'failed' | 'cancelled';
  progress: number;
  jobId?: string;
  errorMessage?: string;
}

// ===== 文件操作输入 =====

export interface GetFileListInput {
  cloudAccountUid: string;
  directoryId?: string;
  offset: number;
  limit: number;
  sortField?: 'name' | 'size' | 'modifiedTime';
  sortOrder?: 'asc' | 'desc';
  fileTypeFilter?: string;
}

export interface CreateFolderInput {
  cloudAccountUid: string;
  parentDirectoryId: string;
  name: string;
}

export interface UploadFileInput {
  cloudAccountUid: string;
  directoryId: string;
  file: File;
  forceAsync?: boolean;
}

export interface CloudDriveUploadExecutionDto extends CloudDriveOperationResultDto {
  file?: {
    fileId: string;
    name: string;
    path?: string;
  };
}

export interface MoveFilesInput {
  cloudAccountUid: string;
  fileIds: string[];
  targetDirectoryId: string;
  sourceDirectoryId?: string;
}

export interface CopyFilesInput {
  cloudAccountUid: string;
  fileIds: string[];
  targetDirectoryId: string;
}

export interface DeleteFilesInput {
  cloudAccountUid: string;
  fileIds: string[];
  sourceDirectoryId?: string;
}

export interface RenameFileInput {
  cloudAccountUid: string;
  fileId: string;
  newName: string;
  parentDirectoryId?: string;
}

export interface SearchFileInput {
  cloudAccountUid: string;
  keyword: string;
  offset: number;
  limit: number;
}

export interface GetRecycleBinListInput {
  cloudAccountUid: string;
  offset: number;
  limit: number;
}

export interface RecycleBinOperationInput {
  cloudAccountUid: string;
  fileIds: string[];
}

export interface GetVideoPlayInput {
  cloudAccountUid: string;
  fileId: string;
  quality?: string;
}

export interface SubmitTranscodeInput {
  cloudAccountUid: string;
  fileId: string;
}

export interface GetJobListInput {
  cloudAccountUid?: string;
  status?: CloudDriveJobStatus;
  offset: number;
  limit: number;
}

// ===== 预览相关 =====

export interface CloudDriveImagePreviewDto {
  previewUrl: string;
  fileName: string;
  fileType: string;
  isTemporary: boolean;
}

export interface CloudDriveDocPreviewDto {
  previewUrl: string;
  fileName: string;
  fileType: string;
  renderMode: 'Direct' | 'ThirdParty' | 'NotAvailable';
  thirdPartyPreviewUrl?: string;
  isTemporary: boolean;
}

// ===== 视频相关 =====

export interface CloudDriveVideoPlayDto {
  status: 'Ready' | 'Transcoding';
  jobId?: string;
  qualityOptions: VideoQualityOption[];
  duration: number;
  playbackMetadata?: Record<string, string>;
  isTemporary: boolean;
}

export interface VideoQualityOption {
  quality: string;
  playUrl: string;
}

export interface CloudDriveSubtitleDto {
  subtitleId: string;
  language: string;
  subtitleUrl: string;
}

export interface CloudDriveVideoTranscodeDto {
  fileId: string;
  status: string;
  jobId?: string;
}

// ===== 任务相关 =====

export type CloudDriveJobType = 'Upload' | 'BatchCopy' | 'BatchMove' | 'BatchDelete' | 'ClearRecycleBin' | 'Transcode';
export type CloudDriveJobStatus = 'Pending' | 'Running' | 'Completed' | 'Failed' | 'Cancelled';

export type JobLocalOperationKind =
  | 'upload'
  | 'copy'
  | 'move'
  | 'delete'
  | 'recycleRestore'
  | 'recyclePermanentDelete'
  | 'clearRecycleBin'
  | 'transcode';

export interface JobLocalContext {
  kind: JobLocalOperationKind;
  fileName?: string;
  fileCount?: number;
  sourceDirectoryId?: string;
  targetDirectoryId?: string;
}

export interface CloudDriveJobDto {
  id: string;
  jobType: CloudDriveJobType;
  status: CloudDriveJobStatus;
  cloudAccountUid: string;
  resourceId?: string;
  progress: number;
  errorMessage?: string;
  startTime?: string;
  endTime?: string;
}

export interface JobListItem {
  job: CloudDriveJobDto;
  description: string;
  localContext?: JobLocalContext;
  accountBadge?: string;
}

// ===== UI 相关 =====

export type RefreshTarget =
  | { type: 'directory'; directoryId: string }
  | { type: 'recycleBin' };

export interface BreadcrumbItem {
  id: string;
  name: string;
}

export interface ClipboardItem {
  fileIds: string[];
  sourceDirectoryId: string;
  operation: 'copy' | 'cut';
}

export type KeyboardScope = 'fileBrowser' | 'imageViewer' | 'videoPlayer' | 'docViewer' | 'global';

export type FileCategory = 'folder' | 'image' | 'video' | 'document' | 'audio' | 'archive' | 'other';
