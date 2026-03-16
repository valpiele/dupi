import React, { useEffect, useState, useCallback } from 'react';
import { View, Text, FlatList, TouchableOpacity, StyleSheet, RefreshControl, Alert } from 'react-native';
import { getFriends, acceptFriend, declineFriend, FriendsList, Friend } from '../../api/social';

export default function FriendsScreen({ navigation }: any) {
  const [data, setData] = useState<FriendsList | null>(null);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try { setData(await getFriends()); } catch {}
  }, []);

  useEffect(() => {
    load();
    const unsub = navigation.addListener('focus', load);
    return unsub;
  }, [load, navigation]);

  const onRefresh = async () => { setRefreshing(true); await load(); setRefreshing(false); };

  const handleAccept = async (userId: string) => { await acceptFriend(userId); load(); };
  const handleDecline = async (userId: string) => { await declineFriend(userId); load(); };

  const renderPending = ({ item }: { item: Friend }) => (
    <View style={styles.card}>
      <View style={{ flex: 1 }}>
        <Text style={styles.name}>{item.displayName}</Text>
        <Text style={styles.username}>@{item.username}</Text>
      </View>
      <TouchableOpacity style={styles.acceptBtn} onPress={() => handleAccept(item.userId)}>
        <Text style={styles.acceptBtnText}>Accept</Text>
      </TouchableOpacity>
      <TouchableOpacity style={styles.declineBtn} onPress={() => handleDecline(item.userId)}>
        <Text style={styles.declineBtnText}>Decline</Text>
      </TouchableOpacity>
    </View>
  );

  const renderFriend = ({ item }: { item: Friend }) => (
    <TouchableOpacity style={styles.card} onPress={() => navigation.navigate('ChatScreen', { friendId: item.userId, friendName: item.displayName })}>
      <View style={{ flex: 1 }}>
        <Text style={styles.name}>{item.displayName}</Text>
        <Text style={styles.username}>@{item.username}</Text>
      </View>
      <Text style={styles.chatIcon}>💬</Text>
    </TouchableOpacity>
  );

  return (
    <View style={styles.container}>
      <FlatList
        data={[...(data?.pendingReceived ?? []), ...(data?.friends ?? [])]}
        keyExtractor={(item) => item.userId}
        renderItem={({ item }) =>
          item.status === 'pending_received' ? renderPending({ item }) : renderFriend({ item })
        }
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
        contentContainerStyle={styles.list}
        ListHeaderComponent={
          data?.pendingReceived?.length ? <Text style={styles.sectionTitle}>Pending Requests</Text> : null
        }
        ListEmptyComponent={<Text style={styles.empty}>No friends yet. Discover people to connect with!</Text>}
      />

      <TouchableOpacity style={styles.discoverBtn} onPress={() => navigation.navigate('Discover')}>
        <Text style={styles.discoverBtnText}>Discover People</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8f9fa' },
  list: { padding: 16 },
  sectionTitle: { fontSize: 16, fontWeight: 'bold', color: '#495057', marginBottom: 8 },
  card: { flexDirection: 'row', backgroundColor: '#fff', borderRadius: 10, padding: 14, marginBottom: 10, borderWidth: 1, borderColor: '#e9ecef', alignItems: 'center' },
  name: { fontSize: 16, fontWeight: '600' },
  username: { fontSize: 13, color: '#6c757d' },
  chatIcon: { fontSize: 20 },
  acceptBtn: { backgroundColor: '#198754', paddingHorizontal: 12, paddingVertical: 6, borderRadius: 6, marginRight: 8 },
  acceptBtnText: { color: '#fff', fontWeight: '600', fontSize: 13 },
  declineBtn: { borderWidth: 1, borderColor: '#dc3545', paddingHorizontal: 12, paddingVertical: 6, borderRadius: 6 },
  declineBtnText: { color: '#dc3545', fontWeight: '600', fontSize: 13 },
  empty: { textAlign: 'center', color: '#6c757d', marginTop: 40 },
  discoverBtn: { backgroundColor: '#198754', margin: 16, padding: 14, borderRadius: 8, alignItems: 'center' },
  discoverBtnText: { color: '#fff', fontWeight: '600', fontSize: 16 },
});
