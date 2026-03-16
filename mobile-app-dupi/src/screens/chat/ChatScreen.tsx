import React, { useEffect, useState, useRef, useCallback } from 'react';
import { View, Text, FlatList, TextInput, TouchableOpacity, StyleSheet, KeyboardAvoidingView, Platform } from 'react-native';
import { getMessages, Message } from '../../api/social';
import { useSignalR } from '../../hooks/useSignalR';
import { useAuthStore } from '../../store/authStore';

export default function ChatScreen({ route }: any) {
  const { friendId, friendName } = route.params;
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const flatListRef = useRef<FlatList>(null);
  const userId = useAuthStore((s) => s.userId);

  const onSignalRMessage = useCallback((method: string, ...args: any[]) => {
    if (method === 'ReceiveMessage') {
      const msg = args[0];
      if (msg.senderId === friendId || msg.senderId === userId) {
        setMessages((prev) => [...prev, {
          id: msg.id,
          senderId: msg.senderId,
          content: msg.content,
          sentAt: msg.sentAt,
          isRead: false,
        }]);
      }
    }
  }, [friendId, userId]);

  const { sendMessage, markRead } = useSignalR(onSignalRMessage);

  useEffect(() => {
    getMessages(friendId).then(setMessages);
    markRead(friendId);
  }, [friendId]);

  const handleSend = async () => {
    if (!input.trim()) return;
    await sendMessage(friendId, input.trim());
    setInput('');
  };

  const renderItem = ({ item }: { item: Message }) => {
    const isMine = item.senderId === userId;
    return (
      <View style={[styles.bubble, isMine ? styles.myBubble : styles.theirBubble]}>
        <Text style={[styles.bubbleText, isMine && styles.myBubbleText]}>{item.content}</Text>
        <Text style={[styles.bubbleTime, isMine && styles.myBubbleTime]}>
          {new Date(item.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        </Text>
      </View>
    );
  };

  return (
    <KeyboardAvoidingView style={styles.container} behavior={Platform.OS === 'ios' ? 'padding' : undefined} keyboardVerticalOffset={90}>
      <FlatList
        ref={flatListRef}
        data={messages}
        keyExtractor={(item) => item.id.toString()}
        renderItem={renderItem}
        contentContainerStyle={styles.list}
        onContentSizeChange={() => flatListRef.current?.scrollToEnd()}
      />
      <View style={styles.inputRow}>
        <TextInput
          style={styles.input}
          value={input}
          onChangeText={setInput}
          placeholder="Type a message..."
          placeholderTextColor="#999"
          onSubmitEditing={handleSend}
          returnKeyType="send"
        />
        <TouchableOpacity style={styles.sendBtn} onPress={handleSend}>
          <Text style={styles.sendBtnText}>Send</Text>
        </TouchableOpacity>
      </View>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8f9fa' },
  list: { padding: 16, flexGrow: 1, justifyContent: 'flex-end' },
  bubble: { maxWidth: '80%', padding: 12, borderRadius: 16, marginBottom: 8 },
  myBubble: { backgroundColor: '#198754', alignSelf: 'flex-end', borderBottomRightRadius: 4 },
  theirBubble: { backgroundColor: '#fff', alignSelf: 'flex-start', borderBottomLeftRadius: 4, borderWidth: 1, borderColor: '#e9ecef' },
  bubbleText: { fontSize: 15 },
  myBubbleText: { color: '#fff' },
  bubbleTime: { fontSize: 11, color: '#adb5bd', marginTop: 4 },
  myBubbleTime: { color: 'rgba(255,255,255,0.7)' },
  inputRow: { flexDirection: 'row', padding: 12, backgroundColor: '#fff', borderTopWidth: 1, borderColor: '#dee2e6' },
  input: { flex: 1, backgroundColor: '#f8f9fa', borderRadius: 20, paddingHorizontal: 16, paddingVertical: 10, fontSize: 15, marginRight: 8 },
  sendBtn: { backgroundColor: '#198754', borderRadius: 20, paddingHorizontal: 20, justifyContent: 'center' },
  sendBtnText: { color: '#fff', fontWeight: '600' },
});
