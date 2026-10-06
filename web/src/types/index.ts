// ===== 通用类型 =====

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  offset: number;
  limit: number;
  /** 后端归一化的路径节点（文件列表/详情可能返回）。 */
  paths?: CloudDrivePathDto[];
  itemSchemaStatus?: 'available' | 'not_collected';
}

export interface ListResult<T> {
  items: T[];
  itemSchemaStatus?: 'available' | 'not_collected';
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

export interface SafeUiError {
  status?: number;
  code?: string;
  message: string;
}

export type CapabilityStatus = 'available' | 'partial' | 'disabled';
export type CloudDriveAuthorizationFlowType = 'AuthorizationCode' | 'AuthorizationCodeWithPkce';

// ===== 认证相关 =====

export interface CloudDriveAuthCheckDto {
  isAuthenticated: boolean;
  isLoggedIn: boolean | null;
  lastCheckedAt: string;
  cooldownUntil?: string;
  failureReason?: string;
}

export interface AuthResponseDto {
  accessToken: string;
  tokenType: 'Bearer';
  expiresAt: string;
  userId: string;
}

export interface CurrentUserDto {
  userId: string;
  userName: string;
  currentCloudAccountUid?: string;
  accounts: CloudDriveAccountSummaryDto[];
}

export interface CloudDriveAccountSummaryDto {
  cloudAccountUid: string;
  status: string | number;
  creationTime: string;
  lastModificationTime?: string;
  cooldownUntil?: string;
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
  parentId?: string;
  pickCode?: string;
  isStar?: boolean;
  isVideo?: boolean;
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

export interface CloudDriveRecycleBinItemDto {
  cid: string;
  dtime: string;
  fileName: string;
  fileSize: string;
  id: string;
  parentName: string;
  status: string;
  type: string;
  schemaStatus?: 'available' | 'not_collected';
}

export interface CloudDrivePathDto {
  aid?: string;
  fileId: string;
  fileName: string;
  parentId?: string;
  isSelectedPath?: boolean;
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
  directoryId?: string;
  offset: number;
  limit: number;
  type?: number;
  suffix?: string;
  asc?: number;
  orderBy?: string;
  star?: number;
  showDir?: number;
  natSort?: number;
  snap?: number;
  countFolders?: number;
  recordOpenTime?: number;
  code?: string;
  scid?: string;
  source?: string;
  customOrder?: string;
  stdir?: number;
}

export interface CreateFolderInput {
  parentId: string;
  folderName: string;
}

export interface UploadFileInput {
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
  fileIds: string[];
  targetDirectoryId: string;
  sourceDirectoryId?: string;
}

export interface CopyFilesInput {
  fileIds: string[];
  targetDirectoryId: string;
}

export interface DeleteFilesInput {
  fileIds: string[];
  sourceDirectoryId?: string;
}

export interface RenameFileInput {
  fileId: string;
  newFileName: string;
}

export interface SearchFileInput {
  searchValue: string;
  offset: number;
  limit: number;
  directoryId?: string;
  date?: string;
  type?: number;
  pickCode?: string;
  countFolders?: number;
  source?: string;
}

export interface GetRecycleBinListInput {
  offset: number;
  limit: number;
}

export interface RecycleBinOperationInput {
  recycleBinItemIds: string[];
}

export interface GetVideoPlayInput {
  pickCode: string;
  definition?: number;
}

export interface SubmitTranscodeInput {
  pickCode: string;
}

export interface GetJobListInput {
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
  playbackSchemaStatus?: 'available' | 'not_collected';
  fileId?: string;
  fileName?: string;
  fileSize?: number | string;
  pickCode?: string;
  downloadUrl?: string;
  dvProfile?: number | string;
  hdrType?: number | string;
  isHdr?: boolean;
  isDolbyVision?: boolean;
  status: 'Ready' | 'Transcoding' | 'Idle' | 'SchemaNotCollected';
  jobId?: string;
  qualityOptions: VideoQualityOption[];
  audioTracks: AudioTrackOption[];
  duration?: number;
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

export interface AudioTrackOption { id?: string; language?: string; name?: string; }

export interface CloudDriveVideoHistoryDto {
  playedSeconds?: number;
  time?: number;
  watchEnd?: boolean;
  schemaStatus?: 'available' | 'not_collected';
}

export interface CloudDriveOfflineTaskDto {
  infoHash?: string;
  name?: string;
  status?: string;
}
export interface CloudDriveOfflineTaskListDto {
  items: CloudDriveOfflineTaskDto[];
  page?: number; pageCount?: number; pageSize?: number; count?: number; total?: number;
  itemSchemaStatus: 'available' | 'not_collected';
}
export interface CloudDriveOfflineStatsDto { total: number; active: number; completed: number; failed: number; }
export interface CloudDriveOfflineQuotaDto { package?: unknown; count: number; surplus: number; maxSize: number; used: number; }

export interface CloudDriveShareItemDto {
  shareCode: string; shareTitle: string; fileId: string; fileName: string;
  pickCode?: string; fileSize?: number; createTime?: string; expireTime?: string; url?: string;
  schemaStatus: 'available' | 'not_collected';
}
export interface CloudDriveShareListDto {
  items: CloudDriveShareItemDto[]; count?: number; itemSchemaStatus: 'available' | 'not_collected';
}

export interface CloudDriveVipInfoDto { levelName?: string; expire?: string; userLimit?: unknown; limitList?: unknown[]; }
export interface CloudDriveBalanceDto { balance?: number; displayText?: string; canWithdraw?: string; cantWithdraw?: string; detail?: number; feedbackActMoney?: number; money?: string; }
export interface SpaceUsageDto { size: number; sizeFormat?: string; }
export interface TypeSummaryDto { count: number; pecent: number; size: number; sizeFormat?: string; }
export interface CloudDriveSpaceInfoDto {
  schemaStatus: 'available' | 'not_collected'; allTotal?: SpaceUsageDto; allUse?: SpaceUsageDto; allRemain?: SpaceUsageDto;
  typeCounts: Record<string, number>; typeSummaries: Record<string, TypeSummaryDto>;
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
