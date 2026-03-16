import React, { useEffect, useState } from 'react';
import { View, Text, ScrollView, StyleSheet } from 'react-native';
import { getPlan, NutritionPlan } from '../../api/nutrition';

export default function ResultScreen({ route }: any) {
  const [plan, setPlan] = useState<NutritionPlan | null>(route.params?.plan ?? null);

  useEffect(() => {
    if (!plan && route.params?.id) {
      getPlan(route.params.id).then(setPlan);
    }
  }, [route.params?.id]);

  if (!plan) return <View style={styles.container}><Text>Loading...</Text></View>;

  const avgCalories = Math.round((plan.caloriesMin + plan.caloriesMax) / 2);
  const scoreColor = plan.score >= 7 ? '#198754' : plan.score >= 4 ? '#ffc107' : '#dc3545';

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <View style={[styles.scoreCircle, { borderColor: scoreColor }]}>
        <Text style={[styles.scoreValue, { color: scoreColor }]}>{plan.score}</Text>
        <Text style={styles.scoreLabel}>/10</Text>
      </View>
      <Text style={styles.scoreSummary}>{plan.scoreSummary}</Text>

      <Text style={styles.foodDesc}>{plan.foodDescription}</Text>

      <View style={styles.macroRow}>
        <MacroCard label="Calories" value={`${avgCalories}`} unit="kcal" color="#fd7e14" />
        <MacroCard label="Protein" value={`${Math.round(plan.proteins)}`} unit="g" color="#0d6efd" />
        <MacroCard label="Carbs" value={`${Math.round(plan.carbohydrates)}`} unit="g" color="#6f42c1" />
        <MacroCard label="Fat" value={`${Math.round(plan.fats)}`} unit="g" color="#dc3545" />
      </View>

      <View style={styles.macroRow}>
        <MacroCard label="Fiber" value={`${Math.round(plan.fiber)}`} unit="g" color="#198754" />
        <MacroCard label="Sugar" value={`${Math.round(plan.sugar)}`} unit="g" color="#e91e8c" />
        <MacroCard label="Sodium" value={`${Math.round(plan.sodium)}`} unit="mg" color="#6c757d" />
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>What's Good</Text>
        {plan.whatsGood.map((item, i) => (
          <Text key={i} style={styles.bullet}>{'\u2022'} {item}</Text>
        ))}
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>What to Improve</Text>
        {plan.whatToImprove.map((item, i) => (
          <Text key={i} style={styles.bullet}>{'\u2022'} {item}</Text>
        ))}
      </View>
    </ScrollView>
  );
}

function MacroCard({ label, value, unit, color }: { label: string; value: string; unit: string; color: string }) {
  return (
    <View style={styles.macroCard}>
      <Text style={[styles.macroValue, { color }]}>{value}</Text>
      <Text style={styles.macroUnit}>{unit}</Text>
      <Text style={styles.macroLabel}>{label}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8f9fa' },
  content: { padding: 20, alignItems: 'center' },
  scoreCircle: {
    width: 80, height: 80, borderRadius: 40, borderWidth: 3,
    alignItems: 'center', justifyContent: 'center', marginBottom: 8,
  },
  scoreValue: { fontSize: 32, fontWeight: 'bold' },
  scoreLabel: { fontSize: 14, color: '#6c757d', marginTop: -4 },
  scoreSummary: { fontSize: 14, color: '#6c757d', textAlign: 'center', marginBottom: 20 },
  foodDesc: { fontSize: 18, fontWeight: '600', textAlign: 'center', marginBottom: 20 },
  macroRow: { flexDirection: 'row', gap: 8, marginBottom: 8, width: '100%', justifyContent: 'center' },
  macroCard: { backgroundColor: '#fff', borderRadius: 10, padding: 12, alignItems: 'center', minWidth: 75, borderWidth: 1, borderColor: '#e9ecef' },
  macroValue: { fontSize: 20, fontWeight: 'bold' },
  macroUnit: { fontSize: 11, color: '#6c757d' },
  macroLabel: { fontSize: 11, color: '#adb5bd', marginTop: 2 },
  section: { width: '100%', marginTop: 20 },
  sectionTitle: { fontSize: 16, fontWeight: '600', marginBottom: 8 },
  bullet: { fontSize: 14, color: '#495057', marginBottom: 6, lineHeight: 20 },
});
