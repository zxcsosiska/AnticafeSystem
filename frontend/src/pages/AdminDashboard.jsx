import React, { useState, useEffect } from 'react';
import axios from 'axios';
import {
  AppBar, Toolbar, Typography, Container, Grid, Card, CardContent,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Button, Paper, TextField, Alert, Tabs, Tab, IconButton, Box, Chip,
  CircularProgress, Dialog, DialogTitle, DialogContent, DialogActions,
  Snackbar
} from '@mui/material';
import {
  Refresh, Logout, Person, TableRestaurant, Receipt, TrendingUp,
  AttachMoney, Schedule, Cancel, CheckCircle, Add
} from '@mui/icons-material';

const API = 'http://localhost:5154/api';

export default function AdminDashboard({ user, onLogout }) {
  // Состояния
  const [activeSessions, setActiveSessions] = useState([]);
  const [bookings, setBookings] = useState([]);
  const [tabValue, setTabValue] = useState(0);
  const [newSession, setNewSession] = useState({ guestName: '', phone: '', tableNumber: 1 });
  const [message, setMessage] = useState({ text: '', type: 'info', open: false });
  const [stats, setStats] = useState({ activeCount: 0, todayRevenue: 0, totalGuests: 0, currentTariff: 3.5 });
  const [reportData, setReportData] = useState(null);
  const [loading, setLoading] = useState({ sessions: false, bookings: false, report: false });
  const [endDialogOpen, setEndDialogOpen] = useState(false);
  const [selectedSession, setSelectedSession] = useState(null);
  const [endResult, setEndResult] = useState(null);

  // Загрузка активных сеансов
  const fetchActiveSessions = async () => {
    setLoading(prev => ({ ...prev, sessions: true }));
    try {
      const res = await axios.get(`${API}/session/active`);
      setActiveSessions(res.data);
      setStats(prev => ({ ...prev, activeCount: res.data.length }));
    } catch (err) {
      showMessage('Ошибка загрузки сеансов: ' + (err.response?.data?.error || err.message), 'error');
    } finally {
      setLoading(prev => ({ ...prev, sessions: false }));
    }
  };

  // Загрузка бронирований
  const fetchBookings = async () => {
    setLoading(prev => ({ ...prev, bookings: true }));
    try {
      const res = await axios.get(`${API}/booking/active`);
      setBookings(res.data);
    } catch (err) {
      showMessage('Ошибка загрузки броней: ' + (err.response?.data?.error || err.message), 'error');
    } finally {
      setLoading(prev => ({ ...prev, bookings: false }));
    }
  };

  // Загрузка статистики за сегодня
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

  // Загрузка текущего тарифа
  const fetchCurrentTariff = async () => {
    try {
      const res = await axios.get(`${API}/tariff/current`);
      setStats(prev => ({ ...prev, currentTariff: res.data.pricePerMinute }));
    } catch (err) {
      console.error('Ошибка загрузки тарифа:', err);
    }
  };

  // Начало сеанса
  const startSession = async () => {
    if (!newSession.guestName.trim()) {
      showMessage('Введите имя гостя', 'error');
      return;
    }
    try {
      await axios.post(`${API}/session/start`, newSession);
      setNewSession({ guestName: '', phone: '', tableNumber: 1 });
      await Promise.all([fetchActiveSessions(), fetchTodayStats()]);
      showMessage(`Сеанс для "${newSession.guestName}" успешно начат`, 'success');
    } catch (err) {
      showMessage('Ошибка при начале сеанса: ' + (err.response?.data?.error || err.message), 'error');
    }
  };

  // Открытие диалога завершения сеанса
  const openEndSessionDialog = (session) => {
    setSelectedSession(session);
    setEndResult(null);
    setEndDialogOpen(true);
  };

  // Завершение сеанса
  const confirmEndSession = async () => {
    if (!selectedSession) return;
    
    try {
      const res = await axios.post(`${API}/session/end/${selectedSession.id}`);
      setEndResult({
        guestName: selectedSession.guestName,
        minutes: res.data.totalMinutes,
        cost: res.data.totalCost
      });
      await Promise.all([fetchActiveSessions(), fetchTodayStats()]);
      showMessage(res.data.message || 'Сеанс завершён', 'success');
      
      // Закрываем диалог через 2 секунды после успеха
      setTimeout(() => {
        setEndDialogOpen(false);
        setSelectedSession(null);
        setEndResult(null);
      }, 2000);
    } catch (err) {
      showMessage('Ошибка при завершении сеанса: ' + (err.response?.data?.error || err.message), 'error');
      setEndDialogOpen(false);
      setSelectedSession(null);
    }
  };

  // Отмена брони
  const cancelBooking = async (id, guestName) => {
    if (!window.confirm(`Отменить бронь для "${guestName}"?`)) return;
    
    try {
      await axios.delete(`${API}/booking/cancel/${id}`);
      await fetchBookings();
      showMessage(`Бронь для "${guestName}" отменена`, 'success');
    } catch (err) {
      showMessage('Ошибка при отмене: ' + (err.response?.data?.error || err.message), 'error');
    }
  };

  // Загрузка отчёта
  const fetchReport = async () => {
    setLoading(prev => ({ ...prev, report: true }));
    try {
      const today = new Date().toISOString().split('T')[0];
      const res = await axios.get(`${API}/report/revenue?from=${today}&to=${today}`);
      setReportData(res.data);
    } catch (err) {
      showMessage('Ошибка загрузки отчёта: ' + (err.response?.data?.error || err.message), 'error');
    } finally {
      setLoading(prev => ({ ...prev, report: false }));
    }
  };

  // Показать уведомление
  const showMessage = (text, type) => {
    setMessage({ text, type, open: true });
    setTimeout(() => setMessage(prev => ({ ...prev, open: false })), 4000);
  };

  // Форматирование времени
  const formatTime = (dateStr) => {
    return new Date(dateStr).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
  };

  // Форматирование даты
  const formatDate = (dateStr) => {
    return new Date(dateStr).toLocaleDateString('ru-RU');
  };

  // Подсчёт длительности сеанса
  const getDuration = (startTime) => {
    const minutes = Math.floor((new Date() - new Date(startTime)) / 60000);
    if (minutes < 60) return `${minutes} мин`;
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return `${hours} ч ${mins} мин`;
  };

  // Первоначальная загрузка
  useEffect(() => {
    Promise.all([
      fetchActiveSessions(),
      fetchBookings(),
      fetchTodayStats(),
      fetchCurrentTariff()
    ]);
    
    const interval = setInterval(() => {
      fetchActiveSessions();
      fetchCurrentTariff();
    }, 30000);
    
    return () => clearInterval(interval);
  }, []);

  return (
    <>
      <AppBar position="static" sx={{ bgcolor: '#2c3e50' }}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1 }}>
            🍵 Анти-кафе | {user?.fullName || user?.username}
          </Typography>
          <Chip 
            label={user?.role === 'admin' ? 'Администратор' : user?.role} 
            size="small" 
            sx={{ mr: 2, bgcolor: '#e74c3c', color: 'white' }} 
          />
          <IconButton color="inherit" onClick={fetchActiveSessions}>
            <Refresh />
          </IconButton>
          <Button color="inherit" onClick={onLogout} startIcon={<Logout />}>
            Выйти
          </Button>
        </Toolbar>
      </AppBar>
      
      <Container sx={{ mt: 3, mb: 4 }}>
        <Snackbar
          open={message.open}
          autoHideDuration={4000}
          anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
        >
          <Alert severity={message.type} sx={{ width: '100%' }}>
            {message.text}
          </Alert>
        </Snackbar>
        
        {/* Карточки статистики */}
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={3}>
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
          <Grid item xs={12} sm={3}>
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
          <Grid item xs={12} sm={3}>
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
          <Grid item xs={12} sm={3}>
            <Card sx={{ bgcolor: '#9b59b6', color: 'white' }}>
              <CardContent>
                <Box display="flex" justifyContent="space-between" alignItems="center">
                  <Box>
                    <Typography variant="h3">{stats.currentTariff} ₽</Typography>
                    <Typography>Цена за минуту</Typography>
                  </Box>
                  <AttachMoney sx={{ fontSize: 48, opacity: 0.7 }} />
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
        
        {/* Табы */}
        <Paper sx={{ mb: 2 }}>
          <Tabs 
            value={tabValue} 
            onChange={(e, v) => setTabValue(v)} 
            indicatorColor="primary" 
            textColor="primary"
            variant="scrollable"
            scrollButtons="auto"
          >
            <Tab icon={<Schedule />} label="Активные сеансы" />
            <Tab icon={<TableRestaurant />} label="Бронирования" />
            <Tab icon={<Add />} label="Новый сеанс" />
            <Tab icon={<TrendingUp />} label="Отчёты" />
          </Tabs>
        </Paper>
        
        {/* Активные сеансы */}
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
                  <TableCell><b>Сумма (приблиз.)</b></TableCell>
                  <TableCell><b>Действие</b></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {loading.sessions ? (
                  <TableRow>
                    <TableCell colSpan={7} align="center" sx={{ py: 4 }}>
                      <CircularProgress />
                    </TableCell>
                  </TableRow>
                ) : activeSessions.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} align="center" sx={{ py: 4 }}>
                      🟢 Нет активных сеансов
                    </TableCell>
                  </TableRow>
                ) : (
                  activeSessions.map((session) => {
                    const duration = getDuration(session.startTime);
                    const approxCost = Math.floor(duration.split(' ')[0]) * stats.currentTariff;
                    return (
                      <TableRow key={session.id}>
                        <TableCell>{session.guestName}</TableCell>
                        <TableCell>{session.phone || '—'}</TableCell>
                        <TableCell>{session.tableNumber}</TableCell>
                        <TableCell>{formatTime(session.startTime)}</TableCell>
                        <TableCell>{duration}</TableCell>
                        <TableCell>~{approxCost} ₽</TableCell>
                        <TableCell>
                          <Button 
                            variant="contained" 
                            color="error" 
                            size="small"
                            startIcon={<CheckCircle />}
                            onClick={() => openEndSessionDialog(session)}
                          >
                            Завершить
                          </Button>
                        </TableCell>
                      </TableRow>
                    );
                  })
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}
        
        {/* Бронирования */}
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
                {loading.bookings ? (
                  <TableRow>
                    <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                      <CircularProgress />
                    </TableCell>
                  </TableRow>
                ) : bookings.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} align="center" sx={{ py: 4 }}>
                      📅 Нет активных бронирований
                    </TableCell>
                  </TableRow>
                ) : (
                  bookings.map((booking) => (
                    <TableRow key={booking.id}>
                      <TableCell>{booking.guestName}</TableCell>
                      <TableCell>{booking.phone}</TableCell>
                      <TableCell>{booking.tableNumber}</TableCell>
                      <TableCell>{formatDate(booking.bookingDate)}</TableCell>
                      <TableCell>{booking.bookingTime}</TableCell>
                      <TableCell>
                        <Button 
                          variant="outlined" 
                          color="error" 
                          size="small"
                          startIcon={<Cancel />}
                          onClick={() => cancelBooking(booking.id, booking.guestName)}
                        >
                          Отменить
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}
        
        {/* Новый сеанс */}
        {tabValue === 2 && (
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              ➕ Начать новый сеанс
            </Typography>
            <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
              Текущий тариф: {stats.currentTariff} ₽/минута
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={5}>
                <TextField
                  fullWidth
                  label="Имя гостя *"
                  value={newSession.guestName}
                  onChange={(e) => setNewSession({ ...newSession, guestName: e.target.value })}
                  onKeyPress={(e) => e.key === 'Enter' && startSession()}
                />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField
                  fullWidth
                  label="Телефон"
                  value={newSession.phone}
                  onChange={(e) => setNewSession({ ...newSession, phone: e.target.value })}
                  onKeyPress={(e) => e.key === 'Enter' && startSession()}
                />
              </Grid>
              <Grid item xs={12} sm={2}>
                <TextField
                  fullWidth
                  type="number"
                  label="Стол"
                  inputProps={{ min: 1, max: 20 }}
                  value={newSession.tableNumber}
                  onChange={(e) => setNewSession({ ...newSession, tableNumber: parseInt(e.target.value) || 1 })}
                />
              </Grid>
              <Grid item xs={12} sm={1}>
                <Button 
                  variant="contained" 
                  color="primary" 
                  onClick={startSession} 
                  sx={{ height: 56 }}
                  fullWidth
                  startIcon={<Add />}
                >
                  Старт
                </Button>
              </Grid>
            </Grid>
          </Paper>
        )}
        
        {/* Отчёты */}
        {tabValue === 3 && (
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>📊 Отчёты</Typography>
            <Button 
              variant="contained" 
              startIcon={<TrendingUp />} 
              onClick={fetchReport}
              disabled={loading.report}
            >
              {loading.report ? <CircularProgress size={24} /> : 'Отчёт за сегодня'}
            </Button>
            
            {reportData && (
              <Box sx={{ mt: 3 }}>
                <Typography variant="h6">📈 Выручка за {reportData.from}</Typography>
                <Grid container spacing={2} sx={{ mt: 1 }}>
                  <Grid item xs={12} sm={4}>
                    <Card sx={{ bgcolor: '#27ae60', color: 'white' }}>
                      <CardContent>
                        <Typography variant="h4">{reportData.totalRevenue} ₽</Typography>
                        <Typography>Общая выручка</Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={12} sm={4}>
                    <Card sx={{ bgcolor: '#3498db', color: 'white' }}>
                      <CardContent>
                        <Typography variant="h4">{reportData.totalMinutes}</Typography>
                        <Typography>Всего минут</Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={12} sm={4}>
                    <Card sx={{ bgcolor: '#e67e22', color: 'white' }}>
                      <CardContent>
                        <Typography variant="h4">{reportData.averageCheck?.toFixed(2)} ₽</Typography>
                        <Typography>Средний чек</Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                </Grid>
                <Typography variant="body2" color="textSecondary" sx={{ mt: 2 }}>
                  Всего сеансов: {reportData.sessionsCount}
                </Typography>
              </Box>
            )}
          </Paper>
        )}
      </Container>

      {/* Диалог завершения сеанса */}
      <Dialog open={endDialogOpen} onClose={() => !endResult && setEndDialogOpen(false)}>
        <DialogTitle>
          {endResult ? '✅ Сеанс завершён' : 'Завершить сеанс?'}
        </DialogTitle>
        <DialogContent>
          {selectedSession && !endResult && (
            <Box>
              <Typography><strong>Гость:</strong> {selectedSession.guestName}</Typography>
              <Typography><strong>Стол:</strong> {selectedSession.tableNumber}</Typography>
              <Typography><strong>Начало:</strong> {formatTime(selectedSession.startTime)}</Typography>
              <Typography><strong>Длительность:</strong> {getDuration(selectedSession.startTime)}</Typography>
              <Typography sx={{ mt: 2, color: 'warning.main' }}>
                ⚠️ После завершения вернуть сеанс будет нельзя
              </Typography>
            </Box>
          )}
          {endResult && (
            <Box>
              <Typography variant="h6" color="success.main" gutterBottom>
                Сеанс "{endResult.guestName}" завершён!
              </Typography>
              <Typography>⏱️ Длительность: {endResult.minutes} минут</Typography>
              <Typography variant="h5" color="primary" sx={{ mt: 2 }}>
                💰 К оплате: {endResult.cost} ₽
              </Typography>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          {!endResult && (
            <>
              <Button onClick={() => setEndDialogOpen(false)}>Отмена</Button>
              <Button onClick={confirmEndSession} variant="contained" color="error">
                Завершить
              </Button>
            </>
          )}
          {endResult && (
            <Button onClick={() => setEndDialogOpen(false)} variant="contained">
              Закрыть
            </Button>
          )}
        </DialogActions>
      </Dialog>
    </>
  );
}