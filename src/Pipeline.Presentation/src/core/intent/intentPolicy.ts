import { RiskTier } from '../../contracts/riskTiers';

export interface TierIntentPolicy {
  readonly requiresConfirm: boolean;
  readonly requiresLockedSummary: boolean;
  readonly stepUpExpected: boolean;
  readonly approvalPossible: boolean;
  readonly optimisticAllowed: boolean;
}

export const INTENT_POLICIES: Record<RiskTier, TierIntentPolicy> = {
  T0: {
    requiresConfirm: false,
    requiresLockedSummary: false,
    stepUpExpected: false,
    approvalPossible: false,
    optimisticAllowed: true,
  },
  T1: {
    requiresConfirm: false,
    requiresLockedSummary: false,
    stepUpExpected: false,
    approvalPossible: false,
    optimisticAllowed: false,
  },
  T2: {
    requiresConfirm: true,
    requiresLockedSummary: false,
    stepUpExpected: true,
    approvalPossible: false,
    optimisticAllowed: false,
  },
  T3: {
    requiresConfirm: true,
    requiresLockedSummary: true,
    stepUpExpected: true,
    approvalPossible: true,
    optimisticAllowed: false,
  },
};

export function getTierPolicy(tier: RiskTier): TierIntentPolicy {
  return INTENT_POLICIES[tier];
}
