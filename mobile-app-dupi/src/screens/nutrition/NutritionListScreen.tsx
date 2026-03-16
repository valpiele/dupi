import React, { useEffect, useState, useCallback } from 'react';
import { View, Text, FlatList, TouchableOpacity, StyleSheet, RefreshControl, Alert } from 'react-native';
import { getPlans, deletePlan, NutritionIndex, NutritionPlan } from '../../api/nutrition';

export default function NutritionListScreen({ navigation }: any) {
  const [data, setData] = useState<NutritionIndex | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try {
      setData(await getPlans());
    } catch (err: any) {
      Alert.alert('Error', err.message);
    }
  }, []);

  useEffect(() => {
    load();
    const unsubscribe = navigation.addListener('focus', load);
    return unsubscribe;
  }, [load, navigation]);

  const onRefresh = async () => {
    setRefreshing(true);
    await load();
    setRefreshing(false);
  };

  const handleDelete = (id: string) => {
    Alert.alert('Delete', 'Delete this meal?', [
      { text: 'Cancel' },
      {
        text: 'Delete', style: 'destructive', onPress: async () => {
          await deletePlan(id);
          load();
        },
      },
    ]);
  };

  const renderItem = ({ item }: { item: NutritionPlan }) => (
    <TouchableOpacity
      style={styles.card}
      onPress={() => navigation.navigate('NutritionResult', { id: item.id })}
      onLongPress={() => handleDelete(item.id)}
    >
      <View style={styles.cardHeader}>
        <Text style={styles.cardTitle} numberOfLines={1}>{item.foodDescription}</Text>
        <View style={[styles.scoreBadge, { backgroundColor: item.score >= 7 ? '#198754' : item.score >= 4 ? '#ffc107' : '#dc3545' }]}>
          <Text style={styles.scoreText}>{item.score}/10</Text>
        </View>
      </View>
      <Text style={styles.cardMeta}>
        {item.mealType ? `${item.mealType} · ` : ''}{Math.round((item.caloriesMin + item.caloriesMax) / 2)} kcal · P:{Math.round(item.proteins)}g C:{Math.round(item.carbohydrates)}g F:{Math.round(item.fats)}g
      </Text>
      <Text style={styles.cardDate}>{new Date(item.createdAt).toLocaleDateString()}</Text>
    </TouchableOpacity>
  );

  return (
    <View style={styles.container}>
      {data && (
        <View style={styles.statsRow}>
          <View style={styles.stat}>
            <Text style={styles.statValue}>{data.todayPlans.length}</Text>
            <Text style={styles.statLabel}>Today</Text>
          </View>
          <View style={styles.stat}>
            <Text style={styles.statValue}>{data.currentStreak}</Text>
            <Text style={styles.statLabel}>Streak</Text>
          </View>
          <View style={styles.stat}>
            <Text style={styles.statValue}>{data.plans.length}</Text>
            <Text style={styles.statLabel}>Total</Text>
          </View>
        </View>
      )}

      {data?.activeChallenge && (
        <TouchableOpacity
          style={styles.challengeBanner}
          onPress={() => navigation.navigate('Challenges')}
        >
          <Text style={styles.challengeText}>
            {data.activeChallenge.title} — {Math.round(data.activeChallenge.todayMetricValue)}/{data.activeChallenge.targetValue}
          </Text>
        </TouchableOpacity>
      )}

      <FlatList
        data={data?.plans ?? []}
        keyExtractor={(item) => item.id}
        renderItem={renderItem}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
        contentContainerStyle={styles.list}
        ListEmptyComponent={<Text style={styles.empty}>No meals logged yet. Tap + to analyze your first meal!</Text>}
      />

      <TouchableOpacity
        style={styles.fab}
        onPress={() => navigation.navigate('NutritionAnalyze')}
      >
        <Text style={styles.fabText}>+</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8f9fa' },
  statsRow: { flexDirection: 'row', justifyContent: 'space-around', padding: 16, backgroundColor: '#fff', borderBottomWidth: 1, borderColor: '#dee2e6' },
  stat: { alignItems: 'center' },
  statValue: { fontSize: 24, fontWeight: 'bold', color: '#198754' },
  statLabel: { fontSize: 12, color: '#6c757d' },
  challengeBanner: { backgroundColor: '#198754', padding: 12, marginHorizontal: 16, marginTop: 12, borderRadius: 8 },
  challengeText: { color: '#fff', fontWeight: '600', textAlign: 'center' },
  list: { padding: 16 },
  card: { backgroundColor: '#fff', borderRadius: 10, padding: 16, marginBottom: 12, borderWidth: 1, borderColor: '#e9ecef' },
  cardHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  cardTitle: { fontSize: 16, fontWeight: '600', flex: 1, marginRight: 8 },
  scoreBadge: { borderRadius: 12, paddingHorizontal: 8, paddingVertical: 2 },
  scoreText: { color: '#fff', fontSize: 12, fontWeight: 'bold' },
  cardMeta: { fontSize: 13, color: '#6c757d', marginTop: 6 },
  cardDate: { fontSize: 12, color: '#adb5bd', marginTop: 4 },
  empty: { textAlign: 'center', color: '#6c757d', marginTop: 40, fontSize: 15 },
  fab: {
    position: 'absolute', bottom: 24, right: 24, width: 56, height: 56,
    borderRadius: 28, backgroundColor: '#198754', alignItems: 'center', justifyContent: 'center',
    elevation: 4, shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.25, shadowRadius: 4,
  },
  fabText: { color: '#fff', fontSize: 28, lineHeight: 30 },
});
