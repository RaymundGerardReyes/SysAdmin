// Rules: R-08 Platform isolation — selects iOS vs Windows shell dynamically without breaking test resolution
import React from 'react';
import { Platform } from 'react-native';
import { PlatformShellProps } from './PlatformShell.types';
import { PlatformShell as WindowsPlatformShell } from './PlatformShell.windows';
import { PlatformShell as IosPlatformShell } from './PlatformShell.ios';

export function PlatformShell(props: PlatformShellProps) {
  if (Platform.OS === 'ios') {
    return <IosPlatformShell {...props} />;
  }
  return <WindowsPlatformShell {...props} />;
}

export * from './PlatformShell.types';

