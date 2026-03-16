import { apiGet, apiPost, apiDelete } from './client';

export interface Challenge {
  id: number;
  creatorId: string;
  title: string;
  description?: string;
  metric: string;
  targetValue: number;
  direction: string;
  type: string;
  status: string;
  startDate: string;
  endDate: string;
  participantCount: number;
}

export interface ChallengeIndex {
  activeChallenges: Challenge[];
  pendingInvites: Challenge[];
  completedChallenges: Challenge[];
  communityChallenges: Challenge[];
}

export interface LeaderboardEntry {
  userId: string;
  displayName: string;
  rank: number;
  daysHit: number;
  totalMetricValue: number;
  averageMetricValue: number;
  averageScore: number;
  totalMeals: number;
  dailyBreakdown: { date: string; metricValue: number; mealCount: number; targetHit: boolean }[];
}

export interface ChallengeCreateRequest {
  title: string;
  description?: string;
  metric?: string;
  targetValue?: number;
  direction?: string;
  type?: string;
  invitedFriendIds?: string[];
}

export function getChallenges(): Promise<ChallengeIndex> {
  return apiGet<ChallengeIndex>('/api/challenges');
}

export function createChallenge(data: ChallengeCreateRequest): Promise<Challenge> {
  return apiPost<Challenge>('/api/challenges', data);
}

export function getLeaderboard(id: number): Promise<LeaderboardEntry[]> {
  return apiGet<LeaderboardEntry[]>(`/api/challenges/${id}/leaderboard`);
}

export function joinChallenge(id: number): Promise<void> {
  return apiPost(`/api/challenges/${id}/join`);
}

export function leaveChallenge(id: number): Promise<void> {
  return apiPost(`/api/challenges/${id}/leave`);
}

export function deleteChallenge(id: number): Promise<void> {
  return apiDelete(`/api/challenges/${id}`);
}
