import { beforeEach, describe, expect, it } from 'vitest';
import {
  cloudDriveCapabilities,
  disableCapability,
  getCapabilityStatus,
  isCapabilityEnabled,
  resetRuntimeCapabilities,
} from '@/capabilities/registry';

describe('capability registry', () => {
  beforeEach(() => resetRuntimeCapabilities());

  it('keeps disabled capabilities fail-closed', () => {
    expect(cloudDriveCapabilities.search).toBe('disabled');
    expect(isCapabilityEnabled('search')).toBe(false);
  });

  it('can downgrade a capability after a not-collected response', () => {
    disableCapability('videoBasic');
    expect(getCapabilityStatus('videoBasic')).toBe('disabled');
    expect(isCapabilityEnabled('videoBasic')).toBe(false);
  });
});
