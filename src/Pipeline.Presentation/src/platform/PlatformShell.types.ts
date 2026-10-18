// Rules: R-08 Platform isolation, L-01 Layer 5 allowed imports only
import React from 'react';

export interface ShellNavItem {
  readonly id: string;
  readonly label: string;
  readonly symbol: string;
  readonly badgeCount?: number | undefined;
  readonly isPrimaryMobileTab?: boolean | undefined;
}

export interface PlatformShellProps {
  readonly title?: string | undefined;
  readonly subtitle?: string | undefined;
  readonly navItems?: readonly ShellNavItem[] | undefined;
  readonly activeNavId?: string | undefined;
  readonly onSelectNav?: ((id: string) => void) | undefined;
  readonly sidebarOpen?: boolean | undefined;
  readonly onToggleSidebar?: (() => void) | undefined;
  readonly sessionStatusLabel?: string | undefined;
  readonly sessionAuthenticated?: boolean | undefined;
  readonly headerActions?: React.ReactNode | undefined;
  readonly breadcrumbTrail?: readonly string[] | undefined;
  readonly canNavigateBack?: boolean | undefined;
  readonly onNavigateBack?: (() => void) | undefined;
  readonly children: React.ReactNode;
}
