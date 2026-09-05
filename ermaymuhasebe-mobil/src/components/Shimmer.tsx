import React, { useEffect, useRef } from 'react';
import { View, StyleSheet, Animated, StyleProp, ViewStyle } from 'react-native';

interface ShimmerItemProps {
  width?: number | string;
  height?: number;
  borderRadius?: number;
  style?: StyleProp<ViewStyle>;
}

export const ShimmerItem: React.FC<ShimmerItemProps> = ({
  width = '100%',
  height = 20,
  borderRadius = 8,
  style,
}) => {
  const opacity = useRef(new Animated.Value(0.3)).current;

  useEffect(() => {
    const pulse = Animated.loop(
      Animated.sequence([
        Animated.timing(opacity, {
          toValue: 0.8,
          duration: 750,
          useNativeDriver: true,
        }),
        Animated.timing(opacity, {
          toValue: 0.3,
          duration: 750,
          useNativeDriver: true,
        }),
      ])
    );
    pulse.start();
    return () => pulse.stop();
  }, [opacity]);

  return (
    <Animated.View
      style={[
        styles.shimmer,
        {
          width: width as any,
          height,
          borderRadius,
          opacity,
        },
        style,
      ]}
    />
  );
};

export const ShimmerCardList: React.FC<{ count?: number }> = ({ count = 5 }) => {
  return (
    <View style={styles.listContainer}>
      {Array.from({ length: count }).map((_, index) => (
        <View key={index} style={styles.cardSkeleton}>
          <View style={styles.cardHeader}>
            <ShimmerItem width={44} height={44} borderRadius={22} />
            <View style={{ flex: 1, marginLeft: 12, gap: 8 }}>
              <ShimmerItem width="65%" height={16} borderRadius={6} />
              <ShimmerItem width="40%" height={12} borderRadius={4} />
            </View>
          </View>
          <View style={styles.cardFooter}>
            <ShimmerItem width="35%" height={18} borderRadius={6} />
            <ShimmerItem width="35%" height={18} borderRadius={6} />
          </View>
        </View>
      ))}
    </View>
  );
};

const styles = StyleSheet.create({
  shimmer: {
    backgroundColor: 'rgba(255, 255, 255, 0.12)',
  },
  listContainer: {
    paddingHorizontal: 16,
    paddingTop: 12,
    gap: 12,
  },
  cardSkeleton: {
    backgroundColor: 'rgba(25, 25, 32, 0.7)',
    borderRadius: 18,
    borderWidth: 1,
    borderColor: 'rgba(255, 255, 255, 0.05)',
    padding: 16,
    gap: 16,
  },
  cardHeader: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  cardFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingTop: 8,
    borderTopWidth: 1,
    borderTopColor: 'rgba(255, 255, 255, 0.04)',
  },
});
