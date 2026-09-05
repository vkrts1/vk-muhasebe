import React from 'react';
import { View, Text, StyleSheet, TouchableOpacity, StyleProp, ViewStyle } from 'react-native';
import { ChevronRight } from 'lucide-react-native';
import * as Haptics from 'expo-haptics';
import { AppleTheme } from '../theme/appleDesign';

interface AppleListRowProps {
  icon?: React.ReactNode;
  iconBgColor?: string;
  title: string;
  subtitle?: string;
  value?: string;
  valueSub?: string;
  valueColor?: string;
  tag?: string;
  tagColor?: string;
  onPress?: () => void;
  isFirst?: boolean;
  isLast?: boolean;
  showChevron?: boolean;
  style?: StyleProp<ViewStyle>;
}

export const AppleListRow: React.FC<AppleListRowProps> = ({
  icon,
  iconBgColor,
  title,
  subtitle,
  value,
  valueSub,
  valueColor,
  tag,
  tagColor,
  onPress,
  isFirst = false,
  isLast = false,
  showChevron = true,
  style,
}) => {
  const handlePress = () => {
    if (onPress) {
      Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Light);
      onPress();
    }
  };

  return (
    <TouchableOpacity
      activeOpacity={onPress ? 0.65 : 1}
      onPress={onPress ? handlePress : undefined}
      style={[
        styles.row,
        isFirst && styles.rowFirst,
        isLast && styles.rowLast,
        style,
      ]}
    >
      {icon && (
        <View style={[styles.iconContainer, iconBgColor ? { backgroundColor: iconBgColor } : {}]}>
          {icon}
        </View>
      )}

      <View style={styles.contentContainer}>
        <View style={styles.leftCol}>
          <View style={styles.titleRow}>
            <Text style={styles.title} numberOfLines={1}>
              {title}
            </Text>
            {tag && (
              <View style={[styles.tag, tagColor ? { backgroundColor: tagColor + '20', borderColor: tagColor + '40' } : {}]}>
                <Text style={[styles.tagText, tagColor ? { color: tagColor } : {}]}>{tag}</Text>
              </View>
            )}
          </View>
          {subtitle && (
            <Text style={styles.subtitle} numberOfLines={1}>
              {subtitle}
            </Text>
          )}
        </View>

        {(value || showChevron) && (
          <View style={styles.rightCol}>
            {value && (
              <Text
                style={[
                  styles.value,
                  valueColor ? { color: valueColor } : { color: AppleTheme.colors.textPrimary },
                ]}
              >
                {value}
              </Text>
            )}
            {valueSub && <Text style={styles.valueSub}>{valueSub}</Text>}
            {showChevron && onPress && (
              <ChevronRight color={AppleTheme.colors.textTertiary} size={16} style={styles.chevron} />
            )}
          </View>
        )}
      </View>

      {!isLast && <View style={[styles.divider, { left: icon ? 60 : 16 }]} />}
    </TouchableOpacity>
  );
};

interface AppleGroupedCardProps {
  children: React.ReactNode;
  headerTitle?: string;
  footerTitle?: string;
  style?: StyleProp<ViewStyle>;
}

export const AppleGroupedCard: React.FC<AppleGroupedCardProps> = ({
  children,
  headerTitle,
  footerTitle,
  style,
}) => {
  return (
    <View style={[styles.groupContainer, style]}>
      {headerTitle && <Text style={styles.groupHeader}>{headerTitle.toUpperCase()}</Text>}
      <View style={styles.card}>{children}</View>
      {footerTitle && <Text style={styles.groupFooter}>{footerTitle}</Text>}
    </View>
  );
};

const styles = StyleSheet.create({
  groupContainer: {
    marginBottom: 18,
  },
  groupHeader: {
    fontSize: 12,
    fontWeight: '600',
    color: AppleTheme.colors.textSecondary,
    marginBottom: 6,
    marginLeft: 16,
    letterSpacing: 0.4,
  },
  groupFooter: {
    fontSize: 12,
    color: AppleTheme.colors.textTertiary,
    marginTop: 6,
    marginLeft: 16,
  },
  card: {
    backgroundColor: '#1C1C1E',
    borderRadius: 14,
    overflow: 'hidden',
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.06)',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
    backgroundColor: '#1C1C1E',
    minHeight: 52,
    position: 'relative',
  },
  rowFirst: {
    borderTopLeftRadius: 14,
    borderTopRightRadius: 14,
  },
  rowLast: {
    borderBottomLeftRadius: 14,
    borderBottomRightRadius: 14,
  },
  iconContainer: {
    width: 38,
    height: 38,
    borderRadius: 10,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 14,
    backgroundColor: '#FF9500',
  },
  contentContainer: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  leftCol: {
    flex: 1,
    marginRight: 10,
  },
  titleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  title: {
    fontSize: 15,
    fontWeight: '600',
    color: '#FFFFFF',
    letterSpacing: -0.2,
  },
  tag: {
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 6,
    backgroundColor: 'rgba(255, 255, 255, 0.08)',
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.08)',
  },
  tagText: {
    fontSize: 10,
    fontWeight: '600',
    color: AppleTheme.colors.textSecondary,
  },
  subtitle: {
    fontSize: 12,
    color: AppleTheme.colors.textSecondary,
    marginTop: 2,
    fontWeight: '400',
  },
  rightCol: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'flex-end',
  },
  value: {
    fontSize: 15,
    fontWeight: '700',
    letterSpacing: -0.3,
  },
  valueSub: {
    fontSize: 11,
    color: AppleTheme.colors.textTertiary,
    marginLeft: 4,
  },
  chevron: {
    marginLeft: 6,
  },
  divider: {
    position: 'absolute',
    bottom: 0,
    right: 0,
    height: StyleSheet.hairlineWidth,
    backgroundColor: 'rgba(255, 255, 255, 0.08)',
  },
});
