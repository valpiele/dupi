import React, { useState } from 'react';
import { View, Text, TextInput, TouchableOpacity, StyleSheet, ScrollView, Alert } from 'react-native';
import { createChallenge } from '../../api/challenges';

const METRICS = ['Protein', 'Carbohydrates', 'Fats', 'Fiber', 'Calories', 'Score', 'MealCount'];

export default function ChallengeCreateScreen({ navigation }: any) {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [metric, setMetric] = useState('Protein');
  const [targetValue, setTargetValue] = useState('120');
  const [direction, setDirection] = useState('AtLeast');
  const [type, setType] = useState('Community');
  const [loading, setLoading] = useState(false);

  const handleCreate = async () => {
    if (!title.trim()) {
      Alert.alert('Error', 'Title is required');
      return;
    }
    setLoading(true);
    try {
      const challenge = await createChallenge({
        title: title.trim(),
        description: description.trim() || undefined,
        metric,
        targetValue: parseFloat(targetValue) || 120,
        direction,
        type,
      });
      navigation.replace('ChallengeDashboard', { id: challenge.id });
    } catch (err: any) {
      Alert.alert('Error', err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.title}>Create Challenge</Text>

      <Text style={styles.label}>Title</Text>
      <TextInput style={styles.input} value={title} onChangeText={setTitle} placeholder="High Protein Week" placeholderTextColor="#999" />

      <Text style={styles.label}>Description (optional)</Text>
      <TextInput style={styles.input} value={description} onChangeText={setDescription} placeholder="Challenge description" multiline placeholderTextColor="#999" />

      <Text style={styles.label}>Type</Text>
      <View style={styles.row}>
        {['Community', 'FriendChallenge'].map((t) => (
          <TouchableOpacity key={t} style={[styles.chip, type === t && styles.chipActive]} onPress={() => setType(t)}>
            <Text style={[styles.chipText, type === t && styles.chipTextActive]}>{t === 'Community' ? 'Community' : 'Friends'}</Text>
          </TouchableOpacity>
        ))}
      </View>

      <Text style={styles.label}>Metric</Text>
      <View style={styles.row}>
        {METRICS.map((m) => (
          <TouchableOpacity key={m} style={[styles.chip, metric === m && styles.chipActive]} onPress={() => setMetric(m)}>
            <Text style={[styles.chipText, metric === m && styles.chipTextActive]}>{m}</Text>
          </TouchableOpacity>
        ))}
      </View>

      <Text style={styles.label}>Direction</Text>
      <View style={styles.row}>
        <TouchableOpacity style={[styles.chip, direction === 'AtLeast' && styles.chipActive]} onPress={() => setDirection('AtLeast')}>
          <Text style={[styles.chipText, direction === 'AtLeast' && styles.chipTextActive]}>At Least</Text>
        </TouchableOpacity>
        <TouchableOpacity style={[styles.chip, direction === 'AtMost' && styles.chipActive]} onPress={() => setDirection('AtMost')}>
          <Text style={[styles.chipText, direction === 'AtMost' && styles.chipTextActive]}>At Most</Text>
        </TouchableOpacity>
      </View>

      <Text style={styles.label}>Target Value</Text>
      <TextInput style={styles.input} value={targetValue} onChangeText={setTargetValue} keyboardType="numeric" placeholderTextColor="#999" />

      <TouchableOpacity style={styles.createBtn} onPress={handleCreate} disabled={loading}>
        <Text style={styles.createBtnText}>{loading ? 'Creating...' : 'Create Challenge'}</Text>
      </TouchableOpacity>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8f9fa' },
  content: { padding: 20 },
  title: { fontSize: 24, fontWeight: 'bold', marginBottom: 20 },
  label: { fontSize: 14, fontWeight: '600', color: '#495057', marginBottom: 8, marginTop: 12 },
  input: { backgroundColor: '#fff', borderWidth: 1, borderColor: '#dee2e6', borderRadius: 8, padding: 14, fontSize: 16 },
  row: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  chip: { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20, borderWidth: 1, borderColor: '#dee2e6' },
  chipActive: { backgroundColor: '#198754', borderColor: '#198754' },
  chipText: { fontSize: 13, color: '#495057' },
  chipTextActive: { color: '#fff' },
  createBtn: { backgroundColor: '#198754', padding: 16, borderRadius: 8, alignItems: 'center', marginTop: 24 },
  createBtnText: { color: '#fff', fontSize: 16, fontWeight: '600' },
});
