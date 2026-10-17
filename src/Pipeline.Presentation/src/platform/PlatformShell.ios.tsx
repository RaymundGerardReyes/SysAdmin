import React from 'react';
import { View, Text } from 'react-native';

export function PlatformShell({ children }: { children: React.ReactNode }) {
  return (
    <View style={{ flex: 1 }}>
      <Text style={{ display: 'none' }}>iOS Shell</Text>
      {children}
    </View>
  );
}
