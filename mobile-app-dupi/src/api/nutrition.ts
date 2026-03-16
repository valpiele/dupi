import { apiGet, apiDelete, apiFetch, API_URL } from './client';
import * as SecureStore from 'expo-secure-store';

export interface NutritionPlan {
  id: string;
  title: string;
  createdAt: string;
  mealType?: string;
  inputType: string;
  hasFile: boolean;
  foodDescription: string;
  caloriesMin: number;
  caloriesMax: number;
  proteins: number;
  carbohydrates: number;
  fats: number;
  fiber: number;
  sugar: number;
  sodium: number;
  whatsGood: string[];
  whatToImprove: string[];
  score: number;
  scoreSummary: string;
}

export interface ActiveChallenge {
  id: number;
  title: string;
  metric: string;
  targetValue: number;
  direction: string;
  todayMetricValue: number;
}

export interface NutritionIndex {
  plans: NutritionPlan[];
  todayPlans: NutritionPlan[];
  currentStreak: number;
  activeChallenge?: ActiveChallenge;
}

export function getPlans(): Promise<NutritionIndex> {
  return apiGet<NutritionIndex>('/api/nutrition');
}

export function getPlan(id: string): Promise<NutritionPlan> {
  return apiGet<NutritionPlan>(`/api/nutrition/${id}`);
}

export function deletePlan(id: string): Promise<void> {
  return apiDelete(`/api/nutrition/${id}`);
}

export async function analyzeStream(
  formData: FormData,
  onEvent: (event: { type: string; text?: string; plan?: NutritionPlan; message?: string }) => void,
): Promise<void> {
  const token = await SecureStore.getItemAsync('auth_token');
  const res = await fetch(`${API_URL}/api/nutrition/analyze`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
    },
    body: formData,
  });

  const reader = res.body?.getReader();
  if (!reader) throw new Error('Streaming not supported');

  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';

    for (const line of lines) {
      if (line.startsWith('data: ')) {
        try {
          const event = JSON.parse(line.slice(6));
          onEvent(event);
        } catch {}
      }
    }
  }
}
