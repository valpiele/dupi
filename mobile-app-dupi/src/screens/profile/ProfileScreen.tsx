import React, { useEffect, useState } from 'react';
import { View, Text, TextInput, TouchableOpacity, StyleSheet, ScrollView, Alert, Switch } from 'react-native';
import { getProfile, updateProfile, Profile } from '../../api/social';
import { useAuthStore } from '../../store/authStore';

export default function ProfileScreen() {
  const [profile, setProfile] = useState<Profile | null>(null);
  const [editing, setEditing] = useState(false);
  const [username, setUsername] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [bio, setBio] = useState('');
  const [isPublic, setIsPublic] = useState(false);
  const [saving, setSaving] = useState(false);
  const logout = useAuthStore((s) => s.logout);

  useEffect(() => {
    getProfile().then((p) => {
      setProfile(p);
      setUsername(p.username);
      setDisplayName(p.displayName);
      setBio(p.bio);
      setIsPublic(p.isPublic);
    });
  }, []);

  const handleSave = async () => {
    setSaving(true);
    try {
      const updated = await updateProfile({ username, displayName, bio, isPublic });
      setProfile(updated);
      setEditing(false);
    } catch (err: any) {
      Alert.alert('Error', err.message);
    } finally {
      setSaving(false);
    }
  };

  if (!profile) return <View style={styles.container}><Text style={styles.loading}>Loading...</Text></View>;

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <View style={styles.avatar}>
        <Text style={styles.avatarText}>{(profile.displayName || profile.email).charAt(0).toUpperCase()}</Text>
      </View>

      {editing ? (
        <>
          <Text style={styles.label}>Username</Text>
          <TextInput style={styles.input} value={username} onChangeText={setUsername} autoCapitalize="none" placeholderTextColor="#999" />

          <Text style={styles.label}>Display Name</Text>
          <TextInput style={styles.input} value={displayName} onChangeText={setDisplayName} placeholderTextColor="#999" />

          <Text style={styles.label}>Bio</Text>
          <TextInput style={styles.input} value={bio} onChangeText={setBio} multiline placeholderTextColor="#999" />

          <View style={styles.switchRow}>
            <Text style={styles.label}>Public Profile</Text>
            <Switch value={isPublic} onValueChange={setIsPublic} trackColor={{ true: '#198754' }} />
          </View>

          <TouchableOpacity style={styles.saveBtn} onPress={handleSave} disabled={saving}>
            <Text style={styles.saveBtnText}>{saving ? 'Saving...' : 'Save'}</Text>
          </TouchableOpacity>
          <TouchableOpacity onPress={() => setEditing(false)}>
            <Text style={styles.cancelText}>Cancel</Text>
          </TouchableOpacity>
        </>
      ) : (
        <>
          <Text style={styles.displayName}>{profile.displayName}</Text>
          <Text style={styles.username}>@{profile.username}</Text>
          <Text style={styles.email}>{profile.email}</Text>
          {profile.bio ? <Text style={styles.bio}>{profile.bio}</Text> : null}
          <Text style={styles.visibility}>{profile.isPublic ? 'Public profile' : 'Private profile'}</Text>

          <TouchableOpacity style={styles.editBtn} onPress={() => setEditing(true)}>
            <Text style={styles.editBtnText}>Edit Profile</Text>
          </TouchableOpacity>
        </>
      )}

      <TouchableOpacity style={styles.logoutBtn} onPress={logout}>
        <Text style={styles.logoutBtnText}>Log Out</Text>
      </TouchableOpacity>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#f8f9fa' },
  content: { padding: 24, alignItems: 'center' },
  loading: { textAlign: 'center', marginTop: 40 },
  avatar: { width: 80, height: 80, borderRadius: 40, backgroundColor: '#198754', alignItems: 'center', justifyContent: 'center', marginBottom: 16 },
  avatarText: { color: '#fff', fontSize: 32, fontWeight: 'bold' },
  displayName: { fontSize: 22, fontWeight: 'bold' },
  username: { fontSize: 16, color: '#6c757d' },
  email: { fontSize: 14, color: '#adb5bd', marginTop: 4 },
  bio: { fontSize: 14, color: '#495057', textAlign: 'center', marginTop: 12 },
  visibility: { fontSize: 13, color: '#6c757d', marginTop: 8 },
  label: { fontSize: 14, fontWeight: '600', color: '#495057', alignSelf: 'flex-start', marginBottom: 4, marginTop: 12 },
  input: { backgroundColor: '#fff', borderWidth: 1, borderColor: '#dee2e6', borderRadius: 8, padding: 12, fontSize: 16, width: '100%' },
  switchRow: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', width: '100%', marginTop: 12 },
  saveBtn: { backgroundColor: '#198754', padding: 14, borderRadius: 8, alignItems: 'center', width: '100%', marginTop: 20 },
  saveBtnText: { color: '#fff', fontWeight: '600', fontSize: 16 },
  cancelText: { color: '#6c757d', marginTop: 12 },
  editBtn: { backgroundColor: '#198754', padding: 14, borderRadius: 8, alignItems: 'center', width: '100%', marginTop: 24 },
  editBtnText: { color: '#fff', fontWeight: '600', fontSize: 16 },
  logoutBtn: { borderWidth: 1, borderColor: '#dc3545', padding: 14, borderRadius: 8, alignItems: 'center', width: '100%', marginTop: 16 },
  logoutBtnText: { color: '#dc3545', fontWeight: '600', fontSize: 16 },
});
