/**
 * Apple 2026-2027 Fluid Glass Design System Tokens
 * Human Interface Guidelines (HIG) + Liquid Glass Aesthetics
 */

export const AppleTheme = {
  colors: {
    // Deep OLED Backgrounds
    background: '#070709',
    backgroundSecondary: '#101014',
    backgroundTertiary: '#18181E',
    
    // Glass Surface Overlays
    glassSurface: 'rgba(28, 28, 34, 0.65)',
    glassSurfaceHigh: 'rgba(40, 40, 50, 0.85)',
    glassBorder: 'rgba(255, 255, 255, 0.08)',
    glassBorderHighlight: 'rgba(255, 255, 255, 0.16)',
    
    // Apple Dynamic Accents
    primary: '#0A84FF', // Apple Electric Blue
    primaryGlow: 'rgba(10, 132, 255, 0.25)',
    success: '#30D158', // Apple Mint Green
    successGlow: 'rgba(48, 209, 88, 0.22)',
    warning: '#FF9F0A', // Apple Bright Amber
    warningGlow: 'rgba(255, 159, 10, 0.22)',
    danger: '#FF453A',  // Apple Coral Red
    dangerGlow: 'rgba(255, 69, 58, 0.22)',
    purple: '#BF5AF2',  // Apple Electric Violet
    teal: '#64D2FF',    // Apple Cyan
    
    // Typography Hierarchy
    textPrimary: '#FFFFFF',
    textSecondary: 'rgba(235, 235, 245, 0.68)',
    textTertiary: 'rgba(235, 235, 245, 0.38)',
    textQuaternary: 'rgba(235, 235, 245, 0.18)',
    
    // Gradients
    gradients: {
      card: ['rgba(35, 35, 45, 0.75)', 'rgba(20, 20, 26, 0.85)'],
      primary: ['#0A84FF', '#0055D4'],
      success: ['#30D158', '#248A3D'],
      gold: ['#FFD60A', '#D97706'],
      purple: ['#BF5AF2', '#7A22C2'],
    }
  },
  
  blur: {
    tint: 'dark' as const,
    intensity: 45,
    highIntensity: 80,
  },
  
  radius: {
    xs: 6,
    sm: 10,
    md: 16,
    lg: 22,
    xl: 28,
    full: 9999,
  },
  
  shadows: {
    soft: {
      shadowColor: '#000',
      shadowOffset: { width: 0, height: 4 },
      shadowOpacity: 0.25,
      shadowRadius: 8,
      elevation: 4,
    },
    glow: (color: string) => ({
      shadowColor: color,
      shadowOffset: { width: 0, height: 6 },
      shadowOpacity: 0.35,
      shadowRadius: 12,
      elevation: 8,
    }),
  }
};
