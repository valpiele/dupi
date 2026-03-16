import React, { useEffect, useState } from 'react';
import { View, Text, FlatList, TouchableOpacity, StyleSheet, RefreshControl } from 'react-native';
import { getDiscoverProfiles, sendFriendRequest, DiscoverProfile } from '../../api/social';

export default function DiscoverScreen() {
  const [profiles, setProfiles] = useState<DiscoverProfile[]>([]);
  const [refreshing, setRefreshing] = useState(false);

  const load = async () => {
    try { setProfiles(await getDiscoverProfiles()); } catch {}
  };

  useEffect(() => { load(); }, []);

  const onRefresh = async () => { setRefreshing(true); await load(); setRefreshing(false); };

  const handleAdd = async (userId: string) => {
    await sendFriendRequest(userId);
    setProfiles((prev) => prev.map((p) => p.userId === userId ? { ...p, friendshipStatus: 'pending_sent' } : p));
  };

  const renderItem = ({ item }: { item: DiscoverProfile }) => (
    <View style={styles.card}>
      <View style={{ flex: 1 }}>
        <Text style={styles.name}>{item.displayName}</Text>
        <Text style={styles.username}>@{item.username}</Text>
        {item.bio ? <Text style={styles.bio} numberOfLines={2}>{item.bio}</Text> : null}
      </View>
      {item.friendshipStatus === 'none' && (
        <TouchableOpacity style={styles.addBtn} onPress={() => handleAdd(item.userId)}>
          <Text style={styles.addBtnText}>Add</Text>
        </TouchableOpacity>
      )}
      {item.friendshipStatus === 'pending_sent' && (
        <Text style={styles.pendingText}>Pending</Text>
      )}
      {item.friendshipStatus === 'friends' && (
        <Text style={styles.friendsText}>Friends</Text>
      )}
    </View>
  );

  return (
    <FlatList
      data={profiles}
      keyExtractor={(item) => item.userId}
      renderItem={renderItem}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
      contentContainerStyle={styles.list}
      style={styles.container}
      ListEmptyComponent={<Text style={styles.empty}>No public profiles found.</Text>}
    />
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8f9fa' },
  list: { padding: 16 },
  card: { flexDirection: 'row', backgroundColor: '#fff', borderRadius: 10, padding: 14, marginBottom: 10, borderWidth: 1, borderColor: '#e9ecef', alignItems: 'center' },
  name: { fontSize: 16, fontWeight: '600' },
  username: { fontSize: 13, color: '#6c757d' },
  bio: { fontSize: 13, color: '#495057', marginTop: 4 },
  addBtn: { backgroundColor: '#198754', paddingHorizontal: 16, paddingVertical: 8, borderRadius: 6 },
  addBtnText: { color: '#fff', fontWeight: '600' },
  pendingText: { color: '#6c757d', fontStyle: 'italic' },
  friendsText: { color: '#198754', fontWeight: '600' },
  empty: { textAlign: 'center', color: '#6c757d', marginTop: 40 },
});
