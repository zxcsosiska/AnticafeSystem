import React, { useState, useEffect } from 'react';
import axios from 'axios';
import {
  AppBar, Toolbar, Typography, Container, Grid, Card, CardContent,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Button, Paper, TextField, Alert, Tabs, Tab, IconButton, Box, Chip
} from '@mui/material';
import {
  Refresh, Logout, Person, TableRestaurant, Receipt, TrendingUp
} from '@mui/icons-material';

const API = 'http://localhost:5154/api';

export default function AdminDashboard({ user, onLogout }) {
  const [activeSessions, setActiveSessions] = useState([]);
  const [bookings, setBookings] = useState([]);
  const [tabValue, setTabValue] = useState(0);
  const [newSession, setNewSession] = useState({ guestName: '', phone: '', tableNumber: 1 });
  const [message, setMessage] = useState('');
  const [messageType, setMessageType] = useState('info');
  const [stats, setStats] = useState({ activeCount: 0, todayRevenue: 0, totalGuests: 0 });
  const [reportData, setReportData] = useState(null);
  const [showReport, setShowReport] = useState(false);

  const fetchActiveSessions = async () => {
    try {
      const res = await axios.get(`${API}/session/active`);
      setActiveSessions(res.data);
      setStats(prev => ({ ...prev, activeCount: res.data.length }));
    } catch (err) {
      console.error('Ошибка загрузки сеансов:', err);
    }
  };

  const fetchBookings = async () => {
    try {
      const res = await axios.get(`${API}/booking/active`);
      setBookings(res.data);
    } catch (err) {
      console.error('Ошибка загрузки броней:', err);
    }
  };

  const fetchTodayStats = async () => {
    try {
      const today = new Date().toISOString().split('T')[0];
      const res = await axios.get(`${API}/report/revenue?from=${today}&to=${today}`);
      setStats(prev => ({
        ...prev,
        todayRevenue: res.data.totalRevenue || 0,
        totalGuests: res.data.sessionsCount || 0
      }));
    } catch (err) {
      console.error('Ошибка загрузки статистики:', err);
    }
  };

  const startSession = async () => {
    if (!newSession.guestName) {
      showMessage('Введите имя гостя', 'error');
      return;
    }
    try {
      await axios.post(`${API}/session/start`, newSession);
      setNewSession({ guestName: '', phone: '', tableNumber: 1 });
      fetchActiveSessions();
      fetchTodayStats();
      showMessage('Сеанс успешно начат', 'success');
    } catch (err) {
      showMessage('Ошибка при начале сеанса', 'error');
    }
  };

  const endSession = async (id, guestName) => {
    if (window.confirm(`Завершить сеанс для "${guestName}"?`)) {
      try {
        const res = await axios.post(`${API}/session/end/${id}`);
        showMessage(res.data.message || 'Сеанс завершён', 'success');
        fetchActiveSessions();
        fetchTodayStats();
      } catch (err) {
        showMessage('Ошибка при завершении сеанса', 'error');
      }
    }
  };

  const cancelBooking = async (id, guestName) => {
    if (window.confirm(`Отменить бронь для "${guestName}"?`)) {
      try {
        await axios.delete(`${API}/booking/cancel/${id}`, { data: { confirm: true } });
        fetchBookings();
        showMessage('Бронь отменена', 'success');
      } catch (err) {
        showMessage('Ошибка при отмене', 'error');
      }
    }
  };

  const fetchReport = async () => {
    try {
      const today = new Date().toISOString().split('T')[0];
      const res = await axios.get(`${API}/report/revenue?from=${today}&to=${today}`);
      setReportData(res.data);
      setShowReport(true);
    } catch (err) {
      showMessage('Ошибка загрузки отчёта', 'error');
    }
  };

  const showMessage = (text, type) => {
    setMessage(text);
    setMessageType(type);
    setTimeout(() => setMessage(''), 3000);
  };

  useEffect(() => {
    fetchActiveSessions();
    fetchBookings();
    fetchTodayStats();
    
    const interval = setInterval(() => {
      fetchActiveSessions();
    }, 30000);
    
    return () => clearInterval(interval);
  }, []);

  const formatTime = (dateStr) => {
    return new Date(dateStr).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
  };

  const formatDate = (dateStr) => {
    return new Date(dateStr).toLocaleDateString('ru-RU');
  };

  return (
    <>
      <AppBar position="static" sx={{ bgcolor: '#2c3e50' }}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1 }}>
            🍵 Анти-кафе | {user?.fullName || user?.username}
          </Typography>
          <Chip label={user?.role === 'admin' ? 'Администратор' : user?.role} size="small" sx={{ mr: 2, bgcolor: '#e74c3c', color: 'white' }} />
          <IconButton color="inherit" onClick={fetchActiveSessions}>
            <Refresh />
          </IconButton>
          <Button color="inherit" onClick={onLogout} startIcon={<Logout />}>
            Выйти
          </Button>
        </Toolbar>
      </AppBar>
      
      <Container sx={{ mt: 3, mb: 4 }}>
        {message && <Alert severity={messageType} sx={{ mb: 2 }}>{message}</Alert>}
        
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={4}>
            <Card sx={{ bgcolor: '#3498db', color: 'white' }}>
              <CardContent>
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Box>
                    <Typography variant="h3">{stats.activeCount}</Typography>
                    <Typography>Активных сеансов</Typography>
                  </Box>
                  <Person sx={{ fontSize: 48, opacity: 0.7 }} />
                </Box>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={4}>
            <Card sx={{ bgcolor: '#27ae60', color: 'white' }}>
              <CardContent>
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Box>
                    <Typography variant="h3">{stats.todayRevenue} ₽</Typography>
                    <Typography>Выручка сегодня</Typography>
                  </Box>
                  <Receipt sx={{ fontSize: 48, opacity: 0.7 }} />
                </Box>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={4}>
            <Card sx={{ bgcolor: '#e67e22', color: 'white' }}>
              <CardContent>
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Box>
                    <Typography variant="h3">{stats.totalGuests}</Typography>
                    <Typography>Гостей сегодня</Typography>
                  </Box>
                  <TableRestaurant sx={{ fontSize: 48, opacity: 0.7 }} />
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
        
        <Paper sx={{ mb: 2 }}>
          <Tabs value={tabValue} onChange={(e, v) => setTabValue(v)} indicatorColor="primary" textColor="primary">
            <Tab label="🟢 Активные сеансы" />
            <Tab label="📅 Бронирования" />
            <Tab label="➕ Новый сеанс" />
            <Tab label="📊 Отчёты" />
          </Tabs>
        </Paper>
        
        {tabValue === 0 && (
          <TableContainer component={Paper}>
            <Table>
              <TableHead sx={{ bgcolor: '#ecf0f1' }}>
                <TableRow>
                  <TableCell><b>Гость</b></TableCell>
                  <TableCell><b>Телефон</b></TableCell>
                  <TableCell><b>Стол</b></TableCell>
                  <TableCell><b>Начало</b></TableCell>
                  <TableCell><b>Длительность</b></TableCell>
                  <TableCell><b>Действие</b></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {activeSessions.map((session) => (
                  <TableRow key={session.id}>
                    <TableCell>{session.guestName}</TableCell>
                    <TableCell>{session.phone || '—'}</TableCell>
                    <TableCell>{session.tableNumber}</TableCell>
                    <TableCell>{formatTime(session.startTime)}</TableCell>
                    <TableCell>
                      {Math.floor((new Date() - new Date(session.startTime)) / 60000)} мин
                    </TableCell>
                    <TableCell>
                      <Button variant="contained" color="error" size="small" onClick={() => endSession(session.id, session.guestName)}>
                        Завершить
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {activeSessions.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                      Нет активных сеансов
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}
        
        {tabValue === 1 && (
          <TableContainer component={Paper}>
            <Table>
              <TableHead sx={{ bgcolor: '#ecf0f1' }}>
                <TableRow>
                  <TableCell><b>Гость</b></TableCell>
                  <TableCell><b>Телефон</b></TableCell>
                  <TableCell><b>Стол</b></TableCell>
                  <TableCell><b>Дата</b></TableCell>
                  <TableCell><b>Время</b></TableCell>
                  <TableCell><b>Действие</b></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {bookings.map((booking) => (
                  <TableRow key={booking.id}>
                    <TableCell>{booking.guestName}</TableCell>
                    <TableCell>{booking.phone}</TableCell>
                    <TableCell>{booking.tableNumber}</TableCell>
                    <TableCell>{formatDate(booking.bookingDate)}</TableCell>
                    <TableCell>{booking.bookingTime}</TableCell>
                    <TableCell>
                      <Button variant="outlined" color="error" size="small" onClick={() => cancelBooking(booking.id, booking.guestName)}>
                        Отменить
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
                {bookings.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                      Нет активных бронирований
                    </TableCell>
                  </TableRow>
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}
        
        {tabValue === 2 && (
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>Начать новый сеанс</Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={5}>
                <TextField
                  fullWidth
                  label="Имя гостя"
                  value={newSession.guestName}
                  onChange={(e) => setNewSession({ ...newSession, guestName: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  fullWidth
                  label="Телефон"
                  value={newSession.phone}
                  onChange={(e) => setNewSession({ ...newSession, phone: e.target.value })}
                />
              </Grid>
              <Grid item xs={12} sm={2}>
                <TextField
                  fullWidth
                  type="number"
                  label="Стол"
                  value={newSession.tableNumber}
                  onChange={(e) => setNewSession({ ...newSession, tableNumber: parseInt(e.target.value) || 1 })}
                />
              </Grid>
              <Grid item xs={12} sm={1}>
                <Button variant="contained" color="primary" onClick={startSession} sx={{ height: 56 }}>
                  Старт
                </Button>
              </Grid>
            </Grid>
          </Paper>
        )}
        
        {tabValue === 3 && (
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>Отчёты</Typography>
            <Button variant="contained" startIcon={<TrendingUp />} onClick={fetchReport} sx={{ mr: 2 }}>
              Отчёт за сегодня
            </Button>
            
            {reportData && showReport && (
              <Box sx={{ mt: 3 }}>
                <Typography variant="h6">📈 Выручка за {reportData.from}</Typography>
                <Grid container spacing={2} sx={{ mt: 1 }}>
                  <Grid item xs={4}>
                    <Card>
                      <CardContent>
                        <Typography variant="h5">{reportData.totalRevenue} ₽</Typography>
                        <Typography>Общая выручка</Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={4}>
                    <Card>
                      <CardContent>
                        <Typography variant="h5">{reportData.totalMinutes}</Typography>
                        <Typography>Всего минут</Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={4}>
                    <Card>
                      <CardContent>
                        <Typography variant="h5">{reportData.averageCheck?.toFixed(2)} ₽</Typography>
                        <Typography>Средний чек</Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                </Grid>
                <Button sx={{ mt: 2 }} onClick={() => setShowReport(false)}>Скрыть</Button>
              </Box>
            )}
          </Paper>
        )}
      </Container>
    </>
  );
}