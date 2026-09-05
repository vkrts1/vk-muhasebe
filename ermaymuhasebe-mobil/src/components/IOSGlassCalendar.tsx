import React, { useState } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
} from 'react-native';
import { ChevronLeft, ChevronRight, Calendar as CalendarIcon } from 'lucide-react-native';
import { LiquidGlassCard } from './LiquidGlassView';

interface IOSGlassCalendarProps {
  selectedDate?: string; // YYYY-MM-DD
  onSelectDate?: (dateStr: string) => void;
  title?: string;
}

const MONTH_NAMES = [
  'Ocak', 'Şubat', 'Mart', 'Nisan', 'Mayıs', 'Haziran',
  'Temmuz', 'Ağustos', 'Eylül', 'Ekim', 'Kasım', 'Aralık'
];

const WEEK_DAYS = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];

export function IOSGlassCalendar({
  selectedDate,
  onSelectDate,
  title = 'Tarih Seçin',
}: IOSGlassCalendarProps) {
  const initialDate = selectedDate ? new Date(selectedDate) : new Date();
  const [currentYear, setCurrentYear] = useState(
    isNaN(initialDate.getTime()) ? new Date().getFullYear() : initialDate.getFullYear()
  );
  const [currentMonth, setCurrentMonth] = useState(
    isNaN(initialDate.getTime()) ? new Date().getMonth() : initialDate.getMonth()
  );
  const [selectedDayStr, setSelectedDayStr] = useState(
    selectedDate || new Date().toISOString().split('T')[0]
  );

  const prevMonth = () => {
    if (currentMonth === 0) {
      setCurrentMonth(11);
      setCurrentYear(currentYear - 1);
    } else {
      setCurrentMonth(currentMonth - 1);
    }
  };

  const nextMonth = () => {
    if (currentMonth === 11) {
      setCurrentMonth(0);
      setCurrentYear(currentYear + 1);
    } else {
      setCurrentMonth(currentMonth + 1);
    }
  };

  // Days calculation
  const firstDayOfMonth = new Date(currentYear, currentMonth, 1).getDay();
  // Adjust JS 0=Sun to Mon=0
  const startingOffset = firstDayOfMonth === 0 ? 6 : firstDayOfMonth - 1;
  const daysInMonth = new Date(currentYear, currentMonth + 1, 0).getDate();

  const daysArray: (number | null)[] = [];
  for (let i = 0; i < startingOffset; i++) {
    daysArray.push(null);
  }
  for (let d = 1; d <= daysInMonth; d++) {
    daysArray.push(d);
  }

  const handleDateClick = (day: number) => {
    const monthStr = String(currentMonth + 1).padStart(2, '0');
    const dayStr = String(day).padStart(2, '0');
    const formatted = `${currentYear}-${monthStr}-${dayStr}`;
    setSelectedDayStr(formatted);
    if (onSelectDate) {
      onSelectDate(formatted);
    }
  };

  const todayStr = new Date().toISOString().split('T')[0];

  return (
    <LiquidGlassCard style={styles.calendarContainer} borderRadius={24} intensity={80}>
      {/* Header */}
      <View style={styles.headerRow}>
        <View style={styles.titleGroup}>
          <CalendarIcon color="#0061FF" size={18} />
          <Text style={styles.headerTitleText}>
            {MONTH_NAMES[currentMonth]} {currentYear}
          </Text>
        </View>
        <View style={styles.navBtnRow}>
          <TouchableOpacity onPress={prevMonth} style={styles.navBtn} activeOpacity={0.7}>
            <ChevronLeft color="#FFF" size={18} />
          </TouchableOpacity>
          <TouchableOpacity onPress={nextMonth} style={styles.navBtn} activeOpacity={0.7}>
            <ChevronRight color="#FFF" size={18} />
          </TouchableOpacity>
        </View>
      </View>

      {/* Week Days Header */}
      <View style={styles.weekRow}>
        {WEEK_DAYS.map((wd, i) => (
          <Text key={i} style={styles.weekDayText}>
            {wd}
          </Text>
        ))}
      </View>

      {/* Days Grid */}
      <View style={styles.daysGrid}>
        {daysArray.map((day, index) => {
          if (day === null) {
            return <View key={index} style={styles.dayCellEmpty} />;
          }

          const monthStr = String(currentMonth + 1).padStart(2, '0');
          const dayPad = String(day).padStart(2, '0');
          const dateKey = `${currentYear}-${monthStr}-${dayPad}`;
          const isSelected = dateKey === selectedDayStr;
          const isToday = dateKey === todayStr;

          return (
            <TouchableOpacity
              key={index}
              style={[
                styles.dayCell,
                isToday && styles.todayCell,
                isSelected && styles.selectedCell,
              ]}
              onPress={() => handleDateClick(day)}
              activeOpacity={0.7}
            >
              <Text
                style={[
                  styles.dayText,
                  isToday && styles.todayText,
                  isSelected && styles.selectedText,
                ]}
              >
                {day}
              </Text>
            </TouchableOpacity>
          );
        })}
      </View>
    </LiquidGlassCard>
  );
}

const styles = StyleSheet.create({
  calendarContainer: {
    padding: 16,
    marginVertical: 10,
  },
  headerRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 16,
  },
  titleGroup: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  headerTitleText: {
    fontSize: 16,
    fontWeight: '800',
    color: '#FFFFFF',
    letterSpacing: -0.3,
  },
  navBtnRow: {
    flexDirection: 'row',
    gap: 6,
  },
  navBtn: {
    width: 32,
    height: 32,
    borderRadius: 10,
    backgroundColor: 'rgba(255, 255, 255, 0.08)',
    alignItems: 'center',
    justifyContent: 'center',
  },
  weekRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginBottom: 10,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(255, 255, 255, 0.06)',
    paddingBottom: 8,
  },
  weekDayText: {
    width: '14%',
    textAlign: 'center',
    color: '#8E8E93',
    fontSize: 11,
    fontWeight: '700',
    textTransform: 'uppercase',
  },
  daysGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
  },
  dayCellEmpty: {
    width: '14%',
    height: 36,
  },
  dayCell: {
    width: '14%',
    height: 36,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: 10,
    marginVertical: 2,
  },
  todayCell: {
    borderWidth: 1,
    borderColor: '#0061FF',
  },
  selectedCell: {
    backgroundColor: '#0061FF',
  },
  dayText: {
    color: '#E2E8F0',
    fontSize: 13,
    fontWeight: '600',
  },
  todayText: {
    color: '#60A5FA',
    fontWeight: '800',
  },
  selectedText: {
    color: '#FFFFFF',
    fontWeight: '900',
  },
});
