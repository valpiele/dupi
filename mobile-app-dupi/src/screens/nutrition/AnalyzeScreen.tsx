import React, { useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet, ScrollView,
  ActivityIndicator, Alert, Image,
} from 'react-native';
import * as ImagePicker from 'expo-image-picker';
import { analyzeStream } from '../../api/nutrition';

export default function AnalyzeScreen({ navigation }: any) {
  const [description, setDescription] = useState('');
  const [mealType, setMealType] = useState('');
  const [image, setImage] = useState<ImagePicker.ImagePickerAsset | null>(null);
  const [thinking, setThinking] = useState('');
  const [loading, setLoading] = useState(false);

  const pickImage = async () => {
    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ['images'],
      quality: 0.8,
    });
    if (!result.canceled) {
      setImage(result.assets[0]);
    }
  };

  const takePhoto = async () => {
    const { status } = await ImagePicker.requestCameraPermissionsAsync();
    if (status !== 'granted') {
      Alert.alert('Permission needed', 'Camera access is required to take photos');
      return;
    }
    const result = await ImagePicker.launchCameraAsync({ quality: 0.8 });
    if (!result.canceled) {
      setImage(result.assets[0]);
    }
  };

  const handleAnalyze = async () => {
    if (!image && !description.trim()) {
      Alert.alert('Error', 'Please add a photo or description');
      return;
    }

    setLoading(true);
    setThinking('');

    const formData = new FormData();
    if (description) formData.append('description', description);
    if (mealType) formData.append('mealType', mealType);

    if (image) {
      const ext = image.uri.split('.').pop() || 'jpg';
      formData.append('file', {
        uri: image.uri,
        name: `photo.${ext}`,
        type: `image/${ext === 'png' ? 'png' : 'jpeg'}`,
      } as any);
    }

    try {
      await analyzeStream(formData, (event) => {
        if (event.type === 'thinking') {
          setThinking((prev) => prev + (event.text || ''));
        } else if (event.type === 'done' && event.plan) {
          navigation.replace('NutritionResult', { id: event.plan.id, plan: event.plan });
        } else if (event.type === 'error') {
          Alert.alert('Error', event.message || 'Analysis failed');
          setLoading(false);
        }
      });
    } catch (err: any) {
      Alert.alert('Error', err.message);
      setLoading(false);
    }
  };

  const mealTypes = ['breakfast', 'lunch', 'dinner', 'snack'];

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.title}>Analyze Meal</Text>

      <View style={styles.imageButtons}>
        <TouchableOpacity style={styles.imageBtn} onPress={takePhoto}>
          <Text style={styles.imageBtnText}>Take Photo</Text>
        </TouchableOpacity>
        <TouchableOpacity style={styles.imageBtn} onPress={pickImage}>
          <Text style={styles.imageBtnText}>Gallery</Text>
        </TouchableOpacity>
      </View>

      {image && <Image source={{ uri: image.uri }} style={styles.preview} />}

      <Text style={styles.label}>Meal Type</Text>
      <View style={styles.mealTypeRow}>
        {mealTypes.map((type) => (
          <TouchableOpacity
            key={type}
            style={[styles.mealTypeBtn, mealType === type && styles.mealTypeActive]}
            onPress={() => setMealType(mealType === type ? '' : type)}
          >
            <Text style={[styles.mealTypeText, mealType === type && styles.mealTypeTextActive]}>
              {type.charAt(0).toUpperCase() + type.slice(1)}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      <Text style={styles.label}>Description (optional)</Text>
      <TextInput
        style={styles.textArea}
        placeholder="e.g., Grilled chicken with rice and salad"
        value={description}
        onChangeText={setDescription}
        multiline
        numberOfLines={3}
        placeholderTextColor="#999"
      />

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#198754" />
          <Text style={styles.loadingText}>Analyzing your meal...</Text>
          {thinking ? (
            <Text style={styles.thinkingText} numberOfLines={4}>{thinking.slice(-200)}</Text>
          ) : null}
        </View>
      ) : (
        <TouchableOpacity style={styles.analyzeBtn} onPress={handleAnalyze}>
          <Text style={styles.analyzeBtnText}>Analyze</Text>
        </TouchableOpacity>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8f9fa' },
  content: { padding: 20 },
  title: { fontSize: 24, fontWeight: 'bold', marginBottom: 20 },
  imageButtons: { flexDirection: 'row', gap: 12, marginBottom: 16 },
  imageBtn: { flex: 1, backgroundColor: '#e9ecef', padding: 14, borderRadius: 8, alignItems: 'center' },
  imageBtnText: { fontWeight: '600', color: '#495057' },
  preview: { width: '100%', height: 200, borderRadius: 10, marginBottom: 16 },
  label: { fontSize: 14, fontWeight: '600', color: '#495057', marginBottom: 8 },
  mealTypeRow: { flexDirection: 'row', gap: 8, marginBottom: 16, flexWrap: 'wrap' },
  mealTypeBtn: { paddingHorizontal: 16, paddingVertical: 8, borderRadius: 20, borderWidth: 1, borderColor: '#dee2e6' },
  mealTypeActive: { backgroundColor: '#198754', borderColor: '#198754' },
  mealTypeText: { color: '#495057', fontSize: 14 },
  mealTypeTextActive: { color: '#fff' },
  textArea: {
    backgroundColor: '#fff', borderWidth: 1, borderColor: '#dee2e6', borderRadius: 8,
    padding: 14, fontSize: 16, minHeight: 80, textAlignVertical: 'top', marginBottom: 20,
  },
  analyzeBtn: { backgroundColor: '#198754', padding: 16, borderRadius: 8, alignItems: 'center' },
  analyzeBtnText: { color: '#fff', fontSize: 16, fontWeight: '600' },
  loadingContainer: { alignItems: 'center', marginTop: 20 },
  loadingText: { marginTop: 12, fontSize: 16, color: '#495057' },
  thinkingText: { marginTop: 8, fontSize: 12, color: '#6c757d', fontStyle: 'italic', textAlign: 'center' },
});
