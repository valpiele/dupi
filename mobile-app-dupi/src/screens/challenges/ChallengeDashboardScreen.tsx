import React, { useEffect, useState } from 'react';
import { View, Text, FlatList, StyleSheet, RefreshControl } from 'react-native';
import { getLeaderboard, LeaderboardEntry } from '../../api/challenges';

export default function ChallengeDashboardScreen({ route }: any) {
  const { id } = route.params;
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);
  const [refreshing, setRefreshing] = useState(false);

  const load = async () => {
    try { setLeaderboard(await getLeaderboard(id)); } catch {}
  };

  useEffect(() => { load(); }, [id]);

  const onRefresh = async () => {
    setRefreshing(true);
    await load();
    setRefreshing(false);
  };

  const renderItem = ({ item }: { item: LeaderboardEntry }) => (
    <View style={styles.card}>
      <View style={styles.rankBadge}>
        <Text style={styles.rankText}>#{item.rank}</Text>
      </View>
      <View style={styles.cardContent}>
        <Text style={styles.name}>{item.displayName}</Text>
        <Text style={styles.stats}>
          {item.daysHit}/7 days · {Math.round(item.averageMetricValue)} avg · {item.totalMeals} meals
        </Text>
        <View style={styles.dayRow}>
          {item.dailyBreakdown.map((d, i) => (
            <View key={i} style={[styles.dayDot, { backgroundColor: d.targetHit ? '#198754' : d.mealCount > 0 ? '#ffc107' : '#e9ecef' }]} />
          ))}
        </View>
      </View>
    </View>
  );

  return (
    <FlatList
      data={leaderboard}
      keyExtractor={(item) => item.userId}
      renderItem={renderItem}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
      contentContainerStyle={styles.list}
      style={styles.container}
    />
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8f9fa' },
  list: { padding: 16 },
  card: { flexDirection: 'row', backgroundColor: '#fff', borderRadius: 10, padding: 14, marginBottom: 10, borderWidth: 1, borderColor: '#e9ecef', alignItems: 'center' },
  rankBadge: { width: 40, height: 40, borderRadius: 20, backgroundColor: '#198754', alignItems: 'center', justifyContent: 'center', marginRight: 12 },
  rankText: { color: '#fff', fontWeight: 'bold', fontSize: 16 },
  cardContent: { flex: 1 },
  name: { fontSize: 16, fontWeight: '600' },
  stats: { fontSize: 13, color: '#6c757d', marginTop: 4 },
  dayRow: { flexDirection: 'row', gap: 4, marginTop: 8 },
  dayDot: { width: 20, height: 6, borderRadius: 3 },
});
