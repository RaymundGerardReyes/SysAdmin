// Rules: R-08 Platform isolation, L-01 Layer 5 allowed imports only
import React from 'react';

export interface ShellNavItem {
  readonly id: string;
  readonly label: string;
  readonly symbol: string;
  readonly badgeCount?: number;
  readonly isPrimaryMobileTab?: boolean;
}

export interface PlatformShellProps {
  readonly title?: string;
  readonly subtitle?: string;
  readonly navItems?: readonly ShellNavItem[];
  readonly activeNavId?: string;
  readonly onSelectNav?: (id: string) => void;
  readonly sidebarOpen?: boolean;
  readonly onToggleSidebar?: () => void;
  readonly sessionStatusLabel?: string;
  readonly sessionAuthenticated?: boolean;
  readonly headerActions?: React.ReactNode;
  readonly children: React.ReactNode;
}

