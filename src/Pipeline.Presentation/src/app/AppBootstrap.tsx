import React from 'react';
import { View, Text } from 'react-native';
import { FEATURE_REGISTRY } from './featureRegistry';

export function AppBootstrap() {
  return (
    <View style={{ flex: 1, padding: 24 }}>
      <Text style={{ fontSize: 24, fontWeight: 'bold' }}>Pipeline Presentation</Text>
      <Text style={{ marginTop: 8, color: '#64748b' }}>
        Registered Features: {FEATURE_REGISTRY.map((f) => f.id).join(', ')}
      </Text>
    </View>
  );
}
