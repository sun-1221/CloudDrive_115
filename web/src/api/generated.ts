/**
 * 当前后端公开 `/api/*` 契约的最小类型索引。
 * 该文件只描述浏览器可见的 DTO，不复制任何 115 上游模型或敏感字段。
 * 后续从 OpenAPI 生成完整类型时保持这些公开名称作为兼容入口。
 */
import type {
  CloudDriveAuthCheckDto,
  CloudDriveFileDetailDto,
  CloudDriveFileDto,
  CloudDriveOperationResultDto,
  CloudDriveRecycleBinItemDto,
  CloudDriveVideoPlayDto,
  CloudDriveVideoHistoryDto,
  CloudDriveOfflineTaskListDto,
  CloudDriveOfflineStatsDto,
  CloudDriveOfflineQuotaDto,
  CloudDriveShareListDto,
  CloudDriveVipInfoDto,
  CloudDriveBalanceDto,
  CloudDriveSpaceInfoDto,
  PagedResult,
} from '@/types';

export interface ApiPaths {
  '/auth/check': { get: { response: CloudDriveAuthCheckDto } };
  '/files': { get: { response: PagedResult<CloudDriveFileDto> } };
  '/files/{fileId}': { get: { response: CloudDriveFileDetailDto } };
  '/files/folders': { post: { response: CloudDriveOperationResultDto } };
  '/files/{pickCode}/download': { get: { response: { downloadUrl: string; isTemporary: boolean } } };
  '/recycle': { get: { response: PagedResult<CloudDriveRecycleBinItemDto> } };
  '/videos/{pickCode}/play': { get: { response: CloudDriveVideoPlayDto } };
  '/videos/{pickCode}/history': { get: { response: CloudDriveVideoHistoryDto } };
  '/offline/tasks': { get: { response: CloudDriveOfflineTaskListDto } };
  '/offline/stats': { get: { response: CloudDriveOfflineStatsDto } };
  '/offline/quota': { get: { response: CloudDriveOfflineQuotaDto } };
  '/shares': { get: { response: CloudDriveShareListDto } };
  '/user/vip': { get: { response: CloudDriveVipInfoDto } };
  '/user/balance': { get: { response: CloudDriveBalanceDto } };
  '/user/storage': { get: { response: CloudDriveSpaceInfoDto } };
}

export type ApiPath = keyof ApiPaths;
export type ApiMethod<P extends ApiPath> = keyof ApiPaths[P];
export type ApiResponse<P extends ApiPath, M extends ApiMethod<P>> = ApiPaths[P][M] extends {
  response: infer R;
}
  ? R
  : never;
