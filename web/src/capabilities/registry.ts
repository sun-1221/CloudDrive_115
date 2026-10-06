import type { CapabilityStatus } from '@/types';

/**
 * 由当前验收证据导出的能力闭集。
 * disabled 能力不会被 API/Store/UI 层重新启用；后端返回 not_collected 时
 * 可通过 disableCapability 收敛到 fail-closed 状态。
 */
export const cloudDriveCapabilities = {
  fileList: 'available',
  download: 'available',
  createFolder: 'available',
  copySingle: 'available',
  rename: 'available',
  star: 'available',
  recycleList: 'available',
  videoBasic: 'partial',
  subtitleItems: 'disabled',
  historyRead: 'available',
  offlineRead: 'partial',
  myShares: 'available',
  vipBalanceSpace: 'available',
  userInfo: 'disabled',
  upload: 'disabled',
  search: 'disabled',
  move: 'disabled',
  delete: 'disabled',
  recycleWrite: 'disabled',
  historyWrite: 'disabled',
  transcode: 'disabled',
  offlineWrite: 'disabled',
  shareSendReceive: 'disabled',
} as const satisfies Record<string, CapabilityStatus>;

export type CloudDriveCapability = keyof typeof cloudDriveCapabilities;

const runtimeCapabilities: Record<CloudDriveCapability, CapabilityStatus> = {
  ...cloudDriveCapabilities,
};

export function getCapabilityStatus(capability: CloudDriveCapability): CapabilityStatus {
  return runtimeCapabilities[capability];
}

export function isCapabilityAvailable(capability: CloudDriveCapability): boolean {
  return runtimeCapabilities[capability] === 'available';
}

export function isCapabilityEnabled(capability: CloudDriveCapability): boolean {
  return runtimeCapabilities[capability] !== 'disabled';
}

export function disableCapability(capability: CloudDriveCapability): void {
  runtimeCapabilities[capability] = 'disabled';
}

export function resetRuntimeCapabilities(): void {
  for (const capability of Object.keys(cloudDriveCapabilities) as CloudDriveCapability[]) {
    runtimeCapabilities[capability] = cloudDriveCapabilities[capability];
  }
}

export function capabilityEntries(): ReadonlyArray<readonly [CloudDriveCapability, CapabilityStatus]> {
  return Object.entries(runtimeCapabilities) as ReadonlyArray<readonly [CloudDriveCapability, CapabilityStatus]>;
}
