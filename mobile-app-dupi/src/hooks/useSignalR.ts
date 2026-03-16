import { useEffect, useRef, useCallback } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import * as SecureStore from 'expo-secure-store';
import { API_URL } from '../api/client';

export function useSignalR(onMessage?: (method: string, ...args: any[]) => void) {
  const connectionRef = useRef<HubConnection | null>(null);

  const connect = useCallback(async () => {
    const token = await SecureStore.getItemAsync('auth_token');
    if (!token) return;

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/chatHub`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    if (onMessage) {
      connection.on('ReceiveMessage', (...args) => onMessage('ReceiveMessage', ...args));
      connection.on('MessagesRead', (...args) => onMessage('MessagesRead', ...args));
      connection.on('ChallengeUpdate', (...args) => onMessage('ChallengeUpdate', ...args));
    }

    await connection.start();
    connectionRef.current = connection;
  }, [onMessage]);

  useEffect(() => {
    connect();
    return () => {
      connectionRef.current?.stop();
    };
  }, [connect]);

  const sendMessage = useCallback(async (receiverId: string, content: string) => {
    await connectionRef.current?.invoke('SendMessage', receiverId, content);
  }, []);

  const markRead = useCallback(async (friendId: string) => {
    await connectionRef.current?.invoke('MarkRead', friendId);
  }, []);

  return { sendMessage, markRead, connection: connectionRef };
}
