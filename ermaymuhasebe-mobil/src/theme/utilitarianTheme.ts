// Twitter/X & Telegram Pro Utilitarian Design System (2026/2027)

export const UtilitarianTheme = {
  colors: {
    // Pure Deep Canvas (Zero Fake Gradients)
    background: '#000000',
    surface: '#16181C',
    surfaceHover: '#1D1F24',
    surfaceElevated: '#202327',
    
    // Hairline Borders
    border: 'rgba(255, 255, 255, 0.08)',
    borderSubtle: 'rgba(255, 255, 255, 0.04)',
    borderFocus: '#1D9BF0', // Twitter Brand Electric Blue
    
    // Crisp Typography
    textPrimary: '#F7F9F9',
    textSecondary: '#71767B',
    textTertiary: '#4E5358',
    
    // High-Contrast Utilitarian Indicators
    positive: '#00BA7C',      // Crisp Green (Alacak / Gelir)
    positiveBg: 'rgba(0, 186, 124, 0.12)',
    negative: '#F4212E',      // Sharp Red (Borç / Gider)
    negativeBg: 'rgba(244, 33, 46, 0.12)',
    warning: '#FF7A00',       // Amber (Vadesi Geçen)
    warningBg: 'rgba(255, 122, 0, 0.12)',
    brand: '#1D9BF0',         // Action Blue
    brandBg: 'rgba(29, 155, 240, 0.12)',
  },
  typography: {
    title: {
      fontSize: 20,
      fontWeight: '800' as const,
      letterSpacing: -0.4,
      color: '#F7F9F9',
    },
    subtitle: {
      fontSize: 13,
      fontWeight: '500' as const,
      color: '#71767B',
    },
    bodyBold: {
      fontSize: 15,
      fontWeight: '700' as const,
      letterSpacing: -0.2,
      color: '#F7F9F9',
    },
    body: {
      fontSize: 14,
      fontWeight: '400' as const,
      color: '#E7E9EA',
    },
    monoNumber: {
      fontSize: 15,
      fontWeight: '700' as const,
      letterSpacing: -0.3,
    },
    badge: {
      fontSize: 11,
      fontWeight: '700' as const,
      letterSpacing: 0.2,
    },
  },
  radius: {
    sm: 8,
    md: 12,
    lg: 16,
    full: 9999,
  },
  spacing: {
    xs: 4,
    sm: 8,
    md: 12,
    lg: 16,
    xl: 20,
  }
};
