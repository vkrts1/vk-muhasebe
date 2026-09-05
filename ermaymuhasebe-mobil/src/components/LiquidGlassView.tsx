import React from 'react';
import { View, StyleSheet, ViewStyle, StyleProp, Platform } from 'react-native';

// Safe dynamic imports for Expo Blur and Linear Gradient
let BlurView: any = null;
let LinearGradient: any = null;

try {
  BlurView = require('expo-blur').BlurView;
} catch {
  BlurView = null;
}

try {
  LinearGradient = require('expo-linear-gradient').LinearGradient;
} catch {
  LinearGradient = null;
}

interface LiquidGlassContainerProps {
  children: React.ReactNode;
  style?: StyleProp<ViewStyle>;
  intensity?: number;
  borderRadius?: number;
  borderWidth?: number;
  glowColor?: string;
}

export function LiquidGlassCard({
  children,
  style,
  intensity = 65,
  borderRadius = 24,
  borderWidth = 1,
  glowColor = 'rgba(255, 255, 255, 0.12)',
}: LiquidGlassContainerProps) {
  const containerStyle = [
    styles.glassCard,
    { borderRadius, borderWidth, borderColor: glowColor },
    style,
  ];

  if (BlurView) {
    return (
      <View style={[styles.outerWrapper, { borderRadius }]}>
        <BlurView
          intensity={Platform.OS === 'ios' ? intensity : 90}
          tint="dark"
          style={[styles.blurView, containerStyle]}
        >
          {children}
        </BlurView>
      </View>
    );
  }

  if (LinearGradient) {
    return (
      <LinearGradient
        colors={['rgba(255, 255, 255, 0.14)', 'rgba(255, 255, 255, 0.03)']}
        style={[styles.outerWrapper, { borderRadius, padding: borderWidth }]}
        start={{ x: 0, y: 0 }}
        end={{ x: 1, y: 1 }}
      >
        <View style={[styles.fallbackGlass, { borderRadius: borderRadius - 1 }, style]}>
          {children}
        </View>
      </LinearGradient>
    );
  }

  return (
    <View style={containerStyle}>
      {children}
    </View>
  );
}

const styles = StyleSheet.create({
  outerWrapper: {
    overflow: 'hidden',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.45,
    shadowRadius: 14,
    elevation: 10,
  },
  blurView: {
    backgroundColor: 'rgba(20, 20, 24, 0.72)',
  },
  glassCard: {
    backgroundColor: 'rgba(24, 24, 28, 0.75)',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 6 },
    shadowOpacity: 0.4,
    shadowRadius: 12,
    elevation: 8,
  },
  fallbackGlass: {
    backgroundColor: 'rgba(18, 18, 22, 0.85)',
  },
});
