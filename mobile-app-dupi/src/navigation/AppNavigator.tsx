import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Text } from 'react-native';

import LoginScreen from '../screens/auth/LoginScreen';
import RegisterScreen from '../screens/auth/RegisterScreen';
import NutritionListScreen from '../screens/nutrition/NutritionListScreen';
import AnalyzeScreen from '../screens/nutrition/AnalyzeScreen';
import ResultScreen from '../screens/nutrition/ResultScreen';
import ChallengeListScreen from '../screens/challenges/ChallengeListScreen';
import ChallengeCreateScreen from '../screens/challenges/ChallengeCreateScreen';
import ChallengeDashboardScreen from '../screens/challenges/ChallengeDashboardScreen';
import FriendsScreen from '../screens/social/FriendsScreen';
import DiscoverScreen from '../screens/social/DiscoverScreen';
import ConversationsScreen from '../screens/chat/ConversationsScreen';
import ChatScreen from '../screens/chat/ChatScreen';
import ProfileScreen from '../screens/profile/ProfileScreen';
import { useAuthStore } from '../store/authStore';

const Stack = createNativeStackNavigator();
const Tab = createBottomTabNavigator();

function TabIcon({ label, focused }: { label: string; focused: boolean }) {
  const icons: Record<string, string> = { Nutrition: '🍎', Challenges: '🏆', Social: '👥', Chat: '💬', Profile: '👤' };
  return <Text style={{ fontSize: 20, opacity: focused ? 1 : 0.5 }}>{icons[label] || '•'}</Text>;
}

function MainTabs() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        tabBarIcon: ({ focused }) => <TabIcon label={route.name} focused={focused} />,
        tabBarActiveTintColor: '#198754',
        headerStyle: { backgroundColor: '#198754' },
        headerTintColor: '#fff',
      })}
    >
      <Tab.Screen name="Nutrition" component={NutritionStack} options={{ headerShown: false }} />
      <Tab.Screen name="Challenges" component={ChallengeStack} options={{ headerShown: false }} />
      <Tab.Screen name="Social" component={SocialStack} options={{ headerShown: false }} />
      <Tab.Screen name="Chat" component={ChatStack} options={{ headerShown: false }} />
      <Tab.Screen name="Profile" component={ProfileScreen} options={{ headerStyle: { backgroundColor: '#198754' }, headerTintColor: '#fff' }} />
    </Tab.Navigator>
  );
}

function NutritionStack() {
  return (
    <Stack.Navigator screenOptions={{ headerStyle: { backgroundColor: '#198754' }, headerTintColor: '#fff' }}>
      <Stack.Screen name="NutritionList" component={NutritionListScreen} options={{ title: 'Nutrition' }} />
      <Stack.Screen name="NutritionAnalyze" component={AnalyzeScreen} options={{ title: 'Analyze Meal' }} />
      <Stack.Screen name="NutritionResult" component={ResultScreen} options={{ title: 'Result' }} />
    </Stack.Navigator>
  );
}

function ChallengeStack() {
  return (
    <Stack.Navigator screenOptions={{ headerStyle: { backgroundColor: '#198754' }, headerTintColor: '#fff' }}>
      <Stack.Screen name="ChallengeList" component={ChallengeListScreen} options={{ title: 'Challenges' }} />
      <Stack.Screen name="ChallengeCreate" component={ChallengeCreateScreen} options={{ title: 'New Challenge' }} />
      <Stack.Screen name="ChallengeDashboard" component={ChallengeDashboardScreen} options={{ title: 'Dashboard' }} />
    </Stack.Navigator>
  );
}

function SocialStack() {
  return (
    <Stack.Navigator screenOptions={{ headerStyle: { backgroundColor: '#198754' }, headerTintColor: '#fff' }}>
      <Stack.Screen name="Friends" component={FriendsScreen} options={{ title: 'Friends' }} />
      <Stack.Screen name="Discover" component={DiscoverScreen} options={{ title: 'Discover' }} />
    </Stack.Navigator>
  );
}

function ChatStack() {
  return (
    <Stack.Navigator screenOptions={{ headerStyle: { backgroundColor: '#198754' }, headerTintColor: '#fff' }}>
      <Stack.Screen name="Conversations" component={ConversationsScreen} options={{ title: 'Chat' }} />
      <Stack.Screen name="ChatScreen" component={ChatScreen} options={({ route }: any) => ({ title: route.params?.friendName || 'Chat' })} />
    </Stack.Navigator>
  );
}

function AuthStack() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false }}>
      <Stack.Screen name="Login" component={LoginScreen} />
      <Stack.Screen name="Register" component={RegisterScreen} />
    </Stack.Navigator>
  );
}

export default function AppNavigator() {
  const token = useAuthStore((s) => s.token);
  const isLoading = useAuthStore((s) => s.isLoading);

  if (isLoading) return null;

  return (
    <NavigationContainer>
      {token ? <MainTabs /> : <AuthStack />}
    </NavigationContainer>
  );
}
