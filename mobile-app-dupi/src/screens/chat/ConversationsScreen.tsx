import React, { useEffect, useState, useCallback } from 'react';
import { View, Text, FlatList, TouchableOpacity, StyleSheet, RefreshControl } from 'react-native';
import { getConversations, Conversation } from '../../api/social';

export default function ConversationsScreen({ navigation }: any) {
  const [convos, setConvos] = useState<Conversation[]>([]);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    try { setConvos(await getConversations()); } catch {}
  }, []);

  useEffect(() => {
    load();
    const unsub = navigation.addListener('focus', load);
    return unsub;
  }, [load, navigation]);

  const onRefresh = async () => { setRefreshing(true); await load(); setRefreshing(false); };

  const renderItem = ({ item }: { item: Conversation }) => (
    <TouchableOpacity
      style={styles.card}
      onPress={() => navigation.navigate('ChatScreen', { friendId: item.friendId, friendName: item.displayName })}
    >
      <View style={styles.avatar}>
        <Text style={styles.avatarText}>{item.displayName.charAt(0).toUpperCase()}</Text>
      </View>
      <View style={styles.content}>
        <View style={styles.topRow}>
          <Text style={styles.name} numberOfLines={1}>{item.displayName}</Text>
          <Text style={styles.time}>{new Date(item.lastMessageAt).toLocaleDateString()}</Text>
        </View>
        <Text style={styles.lastMsg} numberOfLines={1}>{item.lastMessage}</Text>
      </View>
      {item.unreadCount > 0 && (
        <View style={styles.badge}><Text style={styles.badgeText}>{item.unreadCount}</Text></View>
      )}
    </TouchableOpacity>
  );

  return (
    <FlatList
      data={convos}
      keyExtractor={(item) => item.friendId}
      renderItem={renderItem}
      refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}
      contentContainerStyle={styles.list}
      style={styles.container}
      ListEmptyComponent={<Text style={styles.empty}>No conversations yet. Add friends to start chatting!</Text>}
    />
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8f9fa' },
  list: { padding: 16 },
  card: { flexDirection: 'row', backgroundColor: '#fff', borderRadius: 10, padding: 14, marginBottom: 10, borderWidth: 1, borderColor: '#e9ecef', alignItems: 'center' },
  avatar: { width: 44, height: 44, borderRadius: 22, backgroundColor: '#198754', alignItems: 'center', justifyContent: 'center', marginRight: 12 },
  avatarText: { color: '#fff', fontSize: 18, fontWeight: 'bold' },
  content: { flex: 1 },
  topRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' },
  name: { fontSize: 16, fontWeight: '600', flex: 1 },
  time: { fontSize: 12, color: '#adb5bd' },
  lastMsg: { fontSize: 14, color: '#6c757d', marginTop: 2 },
  badge: { backgroundColor: '#198754', borderRadius: 10, minWidth: 20, height: 20, alignItems: 'center', justifyContent: 'center', paddingHorizontal: 6 },
  badgeText: { color: '#fff', fontSize: 11, fontWeight: 'bold' },
  empty: { textAlign: 'center', color: '#6c757d', marginTop: 40 },
});
