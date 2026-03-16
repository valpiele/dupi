import React, { useEffect, useState, useCallback } from 'react';
import { View, Text, FlatList, TouchableOpacity, StyleSheet, RefreshControl, SectionList } from 'react-native';
import { getChallenges, ChallengeIndex, Challenge, joinChallenge } from '../../api/challenges';

export default function ChallengeListScreen({ navigation }: any) {
  const [data, setData] = useState<ChallengeIndex | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try { setData(await getChallenges()); } catch {}
  }, []);

  useEffect(() => {
    load();
    const unsub = navigation.addListener('focus', load);
    return unsub;
  }, [load, navigation]);

  const onRefresh = async () => {
    setRefreshing(true);
    await load();
    setRefreshing(false);
  };

  const handleJoin = async (id: number) => {
    await joinChallenge(id);
    load();
  };

  const sections = [
    ...(data?.pendingInvites?.length ? [{ title: 'Pending Invites', data: data.pendingInvites, isPending: true }] : []),
    ...(data?.activeChallenges?.length ? [{ title: 'Active Challenges', data: data.activeChallenges, isPending: false }] : []),
    ...(data?.communityChallenges?.length ? [{ title: 'Community Challenges', data: data.communityChallenges, isPending: false }] : []),
    ...(data?.completedChallenges?.length ? [{ title: 'Completed', data: data.completedChallenges, isPending: false }] : []),
  ];

  return (
    <View style={styles.container}>
      <SectionList
        sections={sections}
        keyExtractor={(item) => item.id.toString()}
        renderSectionHeader={({ section }) => (
          <Text style={styles.sectionHeader}>{section.title}</Text>
        )}
        renderItem={({ item, section }) => (
          <TouchableOpacity
            style={styles.card}
            onPress={() => navigation.navigate('ChallengeDashboard', { id: item.id })}
          >
            <Text style={styles.cardTitle}>{item.title}</Text>
            <Text style={styles.cardMeta}>
              {item.metric} · {item.direction === 'AtLeast' ? '>=' : '<='} {item.targetValue} · {item.participantCount} participants
            </Text>
            <Text style={styles.cardDate}>
              {new Date(item.startDate).toLocaleDateString()} - {new Date(item.endDate).toLocaleDateString()}
            </Text>
            {(section as any).isPending && (
              <TouchableOpacity style={styles.joinBtn} onPress={() => handleJoin(item.id)}>
                <Text style={styles.joinBtnText}>Accept</Text>
              </TouchableOpacity>
            )}
          </TouchableOpacity>
        )}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
        ListEmptyComponent={<Text style={styles.empty}>No challenges yet. Create one!</Text>}
        contentContainerStyle={styles.list}
      />

      <TouchableOpacity
        style={styles.fab}
        onPress={() => navigation.navigate('ChallengeCreate')}
      >
        <Text style={styles.fabText}>+</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8f9fa' },
  list: { padding: 16 },
  sectionHeader: { fontSize: 18, fontWeight: 'bold', color: '#495057', marginTop: 12, marginBottom: 8 },
  card: { backgroundColor: '#fff', borderRadius: 10, padding: 16, marginBottom: 10, borderWidth: 1, borderColor: '#e9ecef' },
  cardTitle: { fontSize: 16, fontWeight: '600' },
  cardMeta: { fontSize: 13, color: '#6c757d', marginTop: 4 },
  cardDate: { fontSize: 12, color: '#adb5bd', marginTop: 4 },
  joinBtn: { marginTop: 10, backgroundColor: '#198754', padding: 10, borderRadius: 6, alignItems: 'center' },
  joinBtnText: { color: '#fff', fontWeight: '600' },
  empty: { textAlign: 'center', color: '#6c757d', marginTop: 40 },
  fab: {
    position: 'absolute', bottom: 24, right: 24, width: 56, height: 56,
    borderRadius: 28, backgroundColor: '#198754', alignItems: 'center', justifyContent: 'center',
    elevation: 4, shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.25, shadowRadius: 4,
  },
  fabText: { color: '#fff', fontSize: 28, lineHeight: 30 },
});
