import React, { useState } from 'react';
import axios from 'axios';
import { TextField, Button, Paper, Typography, Alert, Box } from '@mui/material';

const API = 'http://localhost:5154/api';

export default function Login({ onLogin }) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    
    try {
      const res = await axios.post(`${API}/auth/login`, { username, password });
      if (res.data.success) {
        localStorage.setItem('user', JSON.stringify(res.data.user));
        onLogin(res.data.user);
      }
    } catch (err) {
      if (err.response?.status === 403) {
        setError(err.response.data.message);
      } else {
        setError('Неверный логин или пароль');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ 
      minHeight: '100vh', 
      display: 'flex', 
      alignItems: 'center', 
      justifyContent: 'center',
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)'
    }}>
      <Paper sx={{ padding: 4, maxWidth: 400, width: '100%', borderRadius: 3 }}>
        <Typography variant="h4" align="center" gutterBottom sx={{ color: '#2c3e50', fontWeight: 'bold' }}>
          🍵 Анти-кафе
        </Typography>
        <Typography variant="body2" align="center" color="textSecondary" sx={{ mb: 3 }}>
          Система управления
        </Typography>
        
        <form onSubmit={handleSubmit}>
          <TextField
            fullWidth
            label="Логин"
            margin="normal"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            disabled={loading}
          />
          <TextField
            fullWidth
            type="password"
            label="Пароль"
            margin="normal"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            disabled={loading}
          />
          {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
          <Button 
            fullWidth 
            variant="contained" 
            type="submit" 
            disabled={loading}
            sx={{ mt: 3, mb: 2, py: 1.5, bgcolor: '#2c3e50', '&:hover': { bgcolor: '#1a252f' } }}
          >
            {loading ? 'Вход...' : 'Войти'}
          </Button>
        </form>
      </Paper>
    </Box>
  );
}