import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity } from 'react-native';
import { ArrowUpRight, ArrowDownRight, Phone, FileText, ChevronRight } from 'lucide-react-native';
import * as Haptics from 'expo-haptics';
import { UtilitarianTheme } from '../theme/utilitarianTheme';

interface UtilitarianCariRowProps {
  item: {
    id: number;
    unvan: string;
    cariKod?: string;
    grup?: string;
    borc?: number;
    alacak?: number;
    telefon?: string;
  };
  onPress: () => void;
  onQuickTahsilat?: () => void;
  onQuickOdeme?: () => void;
  onQuickCall?: () => void;
}

const formatMoney = (val: number) => {
  return new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(val);
};

export const UtilitarianCariRow: React.FC<UtilitarianCariRowProps> = ({
  item,
  onPress,
  onQuickTahsilat,
  onQuickOdeme,
  onQuickCall,
}) => {
  const borc = item.borc || 0;
  const alacak = item.alacak || 0;
  const netBakiye = borc - alacak; // Pozitif: Müşteri bize borçlu (Alacağımız var)

  const renderRightActions = () => (
    <View style={styles.swipeActionsContainer}>
      {onQuickTahsilat && (
        <TouchableOpacity
          style={[styles.swipeBtn, { backgroundColor: UtilitarianTheme.colors.positive }]}
          onPress={() => {
            Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium);
            onQuickTahsilat();
          }}
        >
          <ArrowDownRight color="#FFF" size={18} />
          <Text style={styles.swipeBtnText}>Tahsilat</Text>
        </TouchableOpacity>
      )}
      {onQuickOdeme && (
        <TouchableOpacity
          style={[styles.swipeBtn, { backgroundColor: UtilitarianTheme.colors.negative }]}
          onPress={() => {
            Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium);
            onQuickOdeme();
          }}
        >
          <ArrowUpRight color="#FFF" size={18} />
          <Text style={styles.swipeBtnText}>Ödeme</Text>
        </TouchableOpacity>
      )}
    </View>
  );

  return (
    <TouchableOpacity
      activeOpacity={0.7}
      onPress={() => {
        Haptics.selectionAsync();
        onPress();
      }}
      style={styles.rowContainer}
    >
        {/* Avatar / Monogram */}
        <View style={styles.avatarBox}>
          <Text style={styles.avatarText}>
            {(item.unvan || 'C').trim().substring(0, 2).toUpperCase()}
          </Text>
        </View>

        {/* Info Column */}
        <View style={styles.infoCol}>
          <View style={styles.titleRow}>
            <Text style={styles.unvanText} numberOfLines={1}>
              {item.unvan}
            </Text>
            {item.grup ? (
              <View style={styles.grupPill}>
                <Text style={styles.grupPillText}>{item.grup}</Text>
              </View>
            ) : null}
          </View>
          <Text style={styles.codeText}>{item.cariKod || `CARI-${item.id}`}</Text>
        </View>

        {/* Balance Column */}
        <View style={styles.balanceCol}>
          <Text
            style={[
              styles.balanceAmount,
              netBakiye > 0
                ? { color: UtilitarianTheme.colors.positive }
                : netBakiye < 0
                ? { color: UtilitarianTheme.colors.negative }
                : { color: UtilitarianTheme.colors.textSecondary },
            ]}
          >
            {formatMoney(Math.abs(netBakiye))} ₺
          </Text>
          <Text style={styles.balanceLabel}>
            {netBakiye > 0 ? 'Alacak' : netBakiye < 0 ? 'Borç' : 'Sıfır Bakiye'}
          </Text>
        </View>
      </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  rowContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 14,
    paddingHorizontal: 16,
    backgroundColor: UtilitarianTheme.colors.background,
    borderBottomWidth: 1,
    borderBottomColor: UtilitarianTheme.colors.border,
  },
  avatarBox: {
    width: 42,
    height: 42,
    borderRadius: UtilitarianTheme.radius.full,
    backgroundColor: UtilitarianTheme.colors.surfaceElevated,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
    borderWidth: 1,
    borderColor: UtilitarianTheme.colors.border,
  },
  avatarText: {
    color: UtilitarianTheme.colors.textPrimary,
    fontWeight: '800',
    fontSize: 14,
    letterSpacing: 0.5,
  },
  infoCol: {
    flex: 1,
    marginRight: 12,
  },
  titleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  unvanText: {
    ...UtilitarianTheme.typography.bodyBold,
    flexShrink: 1,
  },
  grupPill: {
    backgroundColor: UtilitarianTheme.colors.surfaceElevated,
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: UtilitarianTheme.radius.sm,
    borderWidth: 1,
    borderColor: UtilitarianTheme.colors.borderSubtle,
  },
  grupPillText: {
    color: UtilitarianTheme.colors.textSecondary,
    fontSize: 10,
    fontWeight: '600',
  },
  codeText: {
    ...UtilitarianTheme.typography.subtitle,
    marginTop: 2,
  },
  balanceCol: {
    alignItems: 'flex-end',
  },
  balanceAmount: {
    ...UtilitarianTheme.typography.monoNumber,
  },
  balanceLabel: {
    color: UtilitarianTheme.colors.textSecondary,
    fontSize: 11,
    fontWeight: '500',
    marginTop: 2,
  },
  swipeActionsContainer: {
    flexDirection: 'row',
    width: 150,
  },
  swipeBtn: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 8,
  },
  swipeBtnText: {
    color: '#FFF',
    fontSize: 11,
    fontWeight: '700',
    marginTop: 4,
  },
});
