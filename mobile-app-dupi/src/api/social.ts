import { apiGet, apiPost } from './client';

export interface Friend {
  userId: string;
  username: string;
  displayName: string;
  bio: string;
  status: string;
}

export interface FriendsList {
  friends: Friend[];
  pendingReceived: Friend[];
}

export interface Conversation {
  friendId: string;
  displayName: string;
  username: string;
  lastMessage: string;
  lastMessageAt: string;
  unreadCount: number;
}

export interface Message {
  id: number;
  senderId: string;
  content: string;
  sentAt: string;
  isRead: boolean;
}

export interface DiscoverProfile {
  userId: string;
  username: string;
  displayName: string;
  bio: string;
  friendshipStatus: string;
}

export interface Profile {
  userId: string;
  username: string;
  email: string;
  displayName: string;
  bio: string;
  isPublic: boolean;
}

export function getFriends(): Promise<FriendsList> {
  return apiGet<FriendsList>('/api/friends');
}

export function sendFriendRequest(userId: string): Promise<void> {
  return apiPost('/api/friends/request', { userId });
}

export function acceptFriend(userId: string): Promise<void> {
  return apiPost('/api/friends/accept', { userId });
}

export function declineFriend(userId: string): Promise<void> {
  return apiPost('/api/friends/decline', { userId });
}

export function unfriend(userId: string): Promise<void> {
  return apiPost('/api/friends/unfriend', { userId });
}

export function getConversations(): Promise<Conversation[]> {
  return apiGet<Conversation[]>('/api/chat/conversations');
}

export function getMessages(friendId: string, skip = 0): Promise<Message[]> {
  return apiGet<Message[]>(`/api/chat/${friendId}?skip=${skip}`);
}

export function getDiscoverProfiles(): Promise<DiscoverProfile[]> {
  return apiGet<DiscoverProfile[]>('/api/discover');
}

export function getProfile(): Promise<Profile> {
  return apiGet<Profile>('/api/profile');
}

export function updateProfile(data: Partial<Profile>): Promise<Profile> {
  return apiPost<Profile>('/api/profile', data);
}
