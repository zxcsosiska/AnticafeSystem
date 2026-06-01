import React, { useState, useEffect } from 'react';
import axios from 'axios';
import {
  AppBar, Toolbar, Typography, Container, Grid, Card, CardContent,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Button, Paper, TextField, Alert, Tabs, Tab, IconButton, Box, Chip,
  Dialog, DialogTitle, DialogContent, DialogActions,
  FormControl, InputLabel, Select, MenuItem, FormHelperText,
  CircularProgress, Divider, InputAdornment, Avatar,
  Stepper, Step, StepLabel, StepContent
} from '@mui/material';
import {
  Refresh, Logout, Person, TableRestaurant, Receipt, TrendingUp,
  AccessTime, Phone, Event, Print, CheckCircle, Cancel,
  People, MeetingRoom, Chair, Timeline, AttachMoney,
  Star, EmojiEmotions, SportsEsports, Timer
} from '@mui/icons-material';

const API = 'http://localhost:5154/api';

export default function AdminDashboard({ user, onLogout }) {
  // ============================================
  // СОСТОЯНИЯ
  // ============================================
  const [activeSessions, setActiveSessions] = useState([]);
  const [bookings, setBookings] = useState([]);
  const [tables, setTables] = useState([]);
  const [rooms, setRooms] = useState([]);
  const [availableTables, setAvailableTables] = useState([]);
  const [tabValue, setTabValue] = useState(0);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState({ text: '', type: 'info', open: false });
  const [activeStep, setActiveStep] = useState(0);
  const [remainingTimes, setRemainingTimes] = useState({});
  
  const [stats, setStats] = useState({
    activeCount: 0,
    todayRevenue: 0,
    totalGuests: 0,
    currentTariff: 3.5
  });
  
  const [newSession, setNewSession] = useState({
    guestName: '',
    phone: '',
    tableNumber: '',
    roomId: 1,
    startDate: new Date().toISOString().split('T')[0],
    startTime: new Date().toTimeString().slice(0, 5),
    durationMinutes: 60
  });
  
  const [receiptDialogOpen, setReceiptDialogOpen] = useState(false);
  const [currentReceipt, setCurrentReceipt] = useState(null);
  const [reportData, setReportData] = useState(null);
  const [showReport, setShowReport] = useState(false);
  const [errors, setErrors] = useState({});

  // ============================================
  // ЗАГРУЗКА ДАННЫХ
  // ============================================

  const showMessage = (text, type) => {
    setMessage({ text, type, open: true });
    setTimeout(() => setMessage(prev => ({ ...prev, open: false })), 4000);
  };

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

  const fetchTables = async () => {
    try {
      const res = await axios.get(`${API}/session/tables`);
      setTables(res.data);
    } catch (err) {
      console.error('Ошибка загрузки столов:', err);
    }
  };

  const fetchRooms = async () => {
    try {
      const res = await axios.get(`${API}/session/rooms`);
      setRooms(res.data);
    } catch (err) {
      console.error('Ошибка загрузки залов:', err);
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

  const fetchCurrentTariff = async () => {
    try {
      const res = await axios.get(`${API}/tariff/current`);
      setStats(prev => ({ ...prev, currentTariff: res.data.pricePerMinute }));
    } catch (err) {
      console.error('Ошибка загрузки тарифа:', err);
    }
  };

  const fetchAvailableTables = async () => {
    if (!newSession.startDate || !newSession.startTime || !newSession.durationMinutes) return;
    
    const startDateTime = new Date(`${newSession.startDate}T${newSession.startTime}`);
    
    try {
      const res = await axios.get(`${API}/session/available-tables`, {
        params: {
          startTime: startDateTime.toISOString(),
          durationMinutes: newSession.durationMinutes
        }
      });
      setAvailableTables(res.data || []);
    } catch (err) {
      console.error('Ошибка загрузки свободных столов:', err);
    }
  };

  // ============================================
  // ОБРАТНЫЙ ОТСЧЕТ
  // ============================================

  useEffect(() => {
    const interval = setInterval(() => {
      const newRemaining = {};
      activeSessions.forEach(session => {
        if (session.isActive && session.plannedDurationMinutes) {
          const endTime = new Date(session.startTime);
          endTime.setMinutes(endTime.getMinutes() + session.plannedDurationMinutes);
          const remaining = Math.max(0, Math.floor((endTime - new Date()) / 60000));
          newRemaining[session.id] = remaining;
          
          if (remaining === 0 && session.isActive) {
            fetchActiveSessions();
            fetchTodayStats();
          }
        }
      });
      setRemainingTimes(newRemaining);
    }, 1000);
    
    return () => clearInterval(interval);
  }, [activeSessions]);

  // ============================================
  // ВАЛИДАЦИЯ
  // ============================================

  const validateForm = () => {
    const newErrors = {};
    if (!newSession.guestName.trim()) {
      newErrors.guestName = 'Введите имя гостя';
    }
    if (!newSession.tableNumber) {
      newErrors.tableNumber = 'Выберите стол';
    }
    if (newSession.durationMinutes < 30) {
      newErrors.durationMinutes = 'Минимальная длительность - 30 минут';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // ============================================
  // ДЕЙСТВИЯ
  // ============================================

  const setCurrentTime = () => {
    const now = new Date();
    const formattedDate = now.toISOString().split('T')[0];
    const formattedTime = now.toTimeString().slice(0, 5);
    setNewSession({
      ...newSession,
      startDate: formattedDate,
      startTime: formattedTime
    });
  };

  const startSession = async () => {
    if (!validateForm()) return;
    
    setLoading(true);
    
    try {
      const startDateTime = new Date(`${newSession.startDate}T${newSession.startTime}`);
      
      const response = await axios.post(`${API}/session/start`, {
        guestName: newSession.guestName,
        phone: newSession.phone,
        tableNumber: parseInt(newSession.tableNumber),
        roomId: newSession.roomId,
        startTime: startDateTime.toISOString(),
        durationMinutes: newSession.durationMinutes
      });
      
      showMessage(`✅ Сеанс начат! Стол ${newSession.tableNumber} на ${newSession.durationMinutes} минут`, 'success');
      
      setNewSession({
        guestName: '',
        phone: '',
        tableNumber: '',
        roomId: 1,
        startDate: new Date().toISOString().split('T')[0],
        startTime: new Date().toTimeString().slice(0, 5),
        durationMinutes: 60
      });
      setActiveStep(0);
      
      fetchActiveSessions();
      fetchTodayStats();
      fetchAvailableTables();
      
    } catch (err) {
      const errorMsg = err.response?.data?.error || 'Ошибка при начале сеанса';
      showMessage(errorMsg, 'error');
    } finally {
      setLoading(false);
    }
  };

  const endSession = async (id, guestName) => {
    if (!window.confirm(`Завершить сеанс для "${guestName}"?`)) return;
    
    setLoading(true);
    
    try {
      const res = await axios.post(`${API}/session/end/${id}`);
      setCurrentReceipt(res.data);
      setReceiptDialogOpen(true);
      fetchActiveSessions();
      fetchTodayStats();
      showMessage('✅ Сеанс завершён', 'success');
    } catch (err) {
      const errorMsg = err.response?.data?.error || 'Ошибка при завершении сеанса';
      showMessage(errorMsg, 'error');
    } finally {
      setLoading(false);
    }
  };

  const cancelBooking = async (id, guestName) => {
    if (!window.confirm(`Отменить бронь для "${guestName}"?`)) return;
    
    try {
      await axios.delete(`${API}/booking/cancel/${id}`);
      fetchBookings();
      showMessage('✅ Бронь отменена', 'success');
    } catch (err) {
      const errorMsg = err.response?.data?.error || 'Ошибка при отмене';
      showMessage(errorMsg, 'error');
    }
  };

  const fetchReport = async () => {
    setLoading(true);
    try {
      const today = new Date().toISOString().split('T')[0];
      const res = await axios.get(`${API}/report/revenue?from=${today}&to=${today}`);
      setReportData(res.data);
      setShowReport(true);
    } catch (err) {
      showMessage('Ошибка загрузки отчёта', 'error');
    } finally {
      setLoading(false);
    }
  };

  const printReceipt = () => {
    const printContent = document.getElementById('receipt-content').innerHTML;
    const originalContents = document.body.innerHTML;
    document.body.innerHTML = printContent;
    window.print();
    document.body.innerHTML = originalContents;
    window.location.reload();
  };

  // ============================================
  // ФОРМАТИРОВАНИЕ
  // ============================================

  const formatTime = (dateStr) => {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
  };

  const formatDate = (dateStr) => {
    if (!dateStr) return '—';
    return new Date(dateStr).toLocaleDateString('ru-RU');
  };

  const formatDateTime = (dateStr) => {
    if (!dateStr) return '—';
    const date = new Date(dateStr);
    return `${date.toLocaleDateString('ru-RU')} ${date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}`;
  };

  const formatNumber = (num) => {
    if (num === undefined || num === null) return '0';
    return num.toLocaleString('ru-RU').replace(/,/g, ' ');
  };

  const formatRemainingTime = (minutes) => {
    if (minutes <= 0) return 'Время вышло';
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours > 0) {
      return `${hours}ч ${mins}мин`;
    }
    return `${mins} мин`;
  };

  // ============================================
  // ЭФФЕКТЫ
  // ============================================

  useEffect(() => {
    fetchActiveSessions();
    fetchBookings();
    fetchTables();
    fetchRooms();
    fetchTodayStats();
    fetchCurrentTariff();
    
    const interval = setInterval(() => {
      fetchActiveSessions();
      fetchCurrentTariff();
    }, 30000);
    
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    if (newSession.startDate && newSession.startTime && newSession.durationMinutes) {
      const timer = setTimeout(() => {
        fetchAvailableTables();
      }, 500);
      return () => clearTimeout(timer);
    }
  }, [newSession.startDate, newSession.startTime, newSession.durationMinutes, newSession.roomId]);

  // ============================================
  // ВСПОМОГАТЕЛЬНЫЕ КОМПОНЕНТЫ
  // ============================================

  const getRoomIcon = (type) => {
    switch(type) {
      case 'vip': return <Star sx={{ color: '#ffd700' }} />;
      case 'game': return <SportsEsports />;
      default: return <MeetingRoom />;
    }
  };

  const StatCard = ({ title, value, icon, color }) => (
    <Card sx={{ 
      background: color, 
      borderRadius: 3, 
      boxShadow: 3,
      color: 'white',
      transition: 'transform 0.2s',
      '&:hover': { transform: 'translateY(-4px)' }
    }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Box>
            <Typography variant="h3" sx={{ 
              fontWeight: 'bold', 
              color: 'white',
              fontSize: { xs: '1.5rem', sm: '2rem', md: '2.5rem' }
            }}>
              {typeof value === 'number' ? formatNumber(value) : value}
            </Typography>
            <Typography variant="body2" sx={{ opacity: 0.9, color: 'white' }}>{title}</Typography>
          </Box>
          <Avatar sx={{ bgcolor: 'rgba(255,255,255,0.2)', width: 56, height: 56 }}>
            {icon}
          </Avatar>
        </Box>
      </CardContent>
    </Card>
  );

  const steps = [
    { label: 'Информация о госте', icon: <Person />, description: 'Введите имя и телефон' },
    { label: 'Выбор места', icon: <MeetingRoom />, description: 'Выберите зал и стол' },
    { label: 'Время и длительность', icon: <AccessTime />, description: 'Укажите дату, время и длительность' },
    { label: 'Подтверждение', icon: <CheckCircle />, description: 'Проверьте данные' }
  ];

  // ============================================
  // РЕНДЕР
  // ============================================

  return (
    <>
      <AppBar position="static" sx={{ bgcolor: '#1a1a2e' }}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 'bold' }}>
            🍵 Анти-кафе | {user?.fullName || user?.username}
          </Typography>
          <Chip 
            label="Администратор" 
            size="small" 
            sx={{ mr: 2, bgcolor: '#e94560', color: 'white', fontWeight: 'bold' }} 
          />
          <IconButton color="inherit" onClick={fetchActiveSessions}>
            <Refresh />
          </IconButton>
          <Button color="inherit" onClick={onLogout} startIcon={<Logout />}>
            Выйти
          </Button>
        </Toolbar>
      </AppBar>
      
      <Container sx={{ mt: 3, mb: 4, maxWidth: '1400px' }}>
        {/* Уведомления */}
        {message.open && (
          <Alert severity={message.type} sx={{ mb: 2, borderRadius: 2 }} onClose={() => setMessage(prev => ({ ...prev, open: false }))}>
            {message.text}
          </Alert>
        )}
        
        {/* Карточки статистики */}
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard 
              title="Активных сеансов" 
              value={stats.activeCount} 
              icon={<Person sx={{ fontSize: 32 }} />}
              color="linear-gradient(135deg, #667eea 0%, #764ba2 100%)"
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard 
              title="Выручка сегодня" 
              value={`${formatNumber(stats.todayRevenue)} ₽`}
              icon={<AttachMoney sx={{ fontSize: 32 }} />}
              color="linear-gradient(135deg, #f093fb 0%, #f5576c 100%)"
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard 
              title="Гостей сегодня" 
              value={stats.totalGuests} 
              icon={<People sx={{ fontSize: 32 }} />}
              color="linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)"
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard 
              title="Цена за минуту" 
              value={`${stats.currentTariff} ₽`} 
              icon={<AccessTime sx={{ fontSize: 32 }} />}
              color="linear-gradient(135deg, #fa709a 0%, #fee140 100%)"
            />
          </Grid>
        </Grid>
        
        {/* Табы */}
        <Paper sx={{ mb: 2, borderRadius: 2 }}>
          <Tabs 
            value={tabValue} 
            onChange={(e, v) => setTabValue(v)} 
            indicatorColor="primary" 
            textColor="primary"
            variant="scrollable"
            scrollButtons="auto"
            sx={{ '& .MuiTab-root': { py: 2 } }}
          >
            <Tab icon={<Person />} label="Активные сеансы" />
            <Tab icon={<TableRestaurant />} label="Бронирования" />
            <Tab icon={<Event />} label="Новый сеанс" />
            <Tab icon={<TrendingUp />} label="Отчёты" />
          </Tabs>
        </Paper>
        
        {/* ============================================ */}
        {/* ВКЛАДКА 1: АКТИВНЫЕ СЕАНСЫ С ОБРАТНЫМ ОТСЧЕТОМ */}
        {/* ============================================ */}
        {tabValue === 0 && (
          <TableContainer component={Paper} sx={{ borderRadius: 2, overflowX: 'auto' }}>
            <Table sx={{ minWidth: 800 }}>
              <TableHead sx={{ bgcolor: '#f5f5f5' }}>
                <TableRow>
                  <TableCell><b>👤 Гость</b></TableCell>
                  <TableCell><b>📞 Телефон</b></TableCell>
                  <TableCell><b>🏠 Зал</b></TableCell>
                  <TableCell><b>🪑 Стол</b></TableCell>
                  <TableCell><b>🕐 Начало</b></TableCell>
                  <TableCell><b>⏱️ Осталось времени</b></TableCell>
                  <TableCell><b>💰 Сумма</b></TableCell>
                  <TableCell><b>⚡ Действие</b></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {activeSessions.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={8} align="center" sx={{ py: 8 }}>
                      <EmojiEmotions sx={{ fontSize: 48, color: '#ccc', mb: 1 }} />
                      <Typography variant="h6" color="textSecondary">Нет активных сеансов</Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  activeSessions.map((session) => {
                    const room = rooms.find(r => r.id === session.roomId);
                    const remaining = remainingTimes[session.id] || session.plannedDurationMinutes;
                    const isExpiring = remaining < 5;
                    
                    return (
                      <TableRow key={session.id} sx={{ '&:hover': { bgcolor: '#f9f9f9' } }}>
                        <TableCell>
                          <Box display="flex" alignItems="center">
                            <Avatar sx={{ width: 32, height: 32, mr: 1, bgcolor: '#667eea' }}>
                              <Person sx={{ fontSize: 18 }} />
                            </Avatar>
                            <Typography fontWeight="500">{session.guestName}</Typography>
                          </Box>
                        </TableCell>
                        <TableCell>{session.phone || '—'}</TableCell>
                        <TableCell>
                          <Chip 
                            size="small" 
                            label={room?.name || 'Основной зал'} 
                            icon={getRoomIcon(room?.type)} 
                            variant="outlined"
                          />
                        </TableCell>
                        <TableCell>
                          <Chip size="small" label={`Стол ${session.tableNumber}`} color="primary" variant="outlined" />
                        </TableCell>
                        <TableCell>{formatTime(session.startTime)}</TableCell>
                        <TableCell>
                          <Box display="flex" alignItems="center">
                            <Timer sx={{ fontSize: 14, mr: 0.5, color: isExpiring ? 'error' : '#666' }} />
                            <Typography 
                              fontWeight="bold" 
                              color={isExpiring ? 'error' : 'primary'}
                              sx={{ fontSize: '1.1rem' }}
                            >
                              {formatRemainingTime(remaining)}
                            </Typography>
                            <Typography variant="caption" color="textSecondary" sx={{ ml: 1 }}>
                              (из {session.plannedDurationMinutes} мин)
                            </Typography>
                          </Box>
                        </TableCell>
                        <TableCell>
                          <Typography fontWeight="bold" color="success.main">
                            ~{formatNumber(session.totalCost)} ₽
                          </Typography>
                        </TableCell>
                        <TableCell>
                          <Button 
                            variant="contained" 
                            color="error" 
                            size="small"
                            onClick={() => endSession(session.id, session.guestName)}
                            disabled={loading}
                            sx={{ borderRadius: 2, textTransform: 'none' }}
                            startIcon={<CheckCircle />}
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
        
        {/* ============================================ */}
        {/* ВКЛАДКА 2: БРОНИРОВАНИЯ */}
        {/* ============================================ */}
        {tabValue === 1 && (
          <TableContainer component={Paper} sx={{ borderRadius: 2, overflowX: 'auto' }}>
            <Table sx={{ minWidth: 800 }}>
              <TableHead sx={{ bgcolor: '#f5f5f5' }}>
                <TableRow>
                  <TableCell><b>👤 Гость</b></TableCell>
                  <TableCell><b>📞 Телефон</b></TableCell>
                  <TableCell><b>🏠 Зал</b></TableCell>
                  <TableCell><b>🪑 Стол</b></TableCell>
                  <TableCell><b>📅 Дата</b></TableCell>
                  <TableCell><b>🕐 Время</b></TableCell>
                  <TableCell><b>⏱️ Длит.</b></TableCell>
                  <TableCell><b>⚡ Действие</b></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {bookings.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={8} align="center" sx={{ py: 8 }}>
                      <TableRestaurant sx={{ fontSize: 48, color: '#ccc', mb: 1 }} />
                      <Typography variant="h6" color="textSecondary">Нет активных бронирований</Typography>
                    </TableCell>
                  </TableRow>
                ) : (
                  bookings.map((booking) => {
                    const room = rooms.find(r => r.id === booking.roomId);
                    return (
                      <TableRow key={booking.id} sx={{ '&:hover': { bgcolor: '#f9f9f9' } }}>
                        <TableCell>
                          <Box display="flex" alignItems="center">
                            <Avatar sx={{ width: 32, height: 32, mr: 1, bgcolor: '#4facfe' }}>
                              <Person sx={{ fontSize: 18 }} />
                            </Avatar>
                            {booking.guestName}
                          </Box>
                        </TableCell>
                        <TableCell>{booking.phone}</TableCell>
                        <TableCell><Chip size="small" label={room?.name || 'Основной зал'} variant="outlined" /></TableCell>
                        <TableCell><Chip size="small" label={`Стол ${booking.tableNumber}`} color="secondary" variant="outlined" /></TableCell>
                        <TableCell>{formatDate(booking.bookingDate)}</TableCell>
                        <TableCell>{booking.startTime}</TableCell>
                        <TableCell>{booking.durationMinutes} мин</TableCell>
                        <TableCell>
                          <Button variant="outlined" color="error" size="small" onClick={() => cancelBooking(booking.id, booking.guestName)} startIcon={<Cancel />} sx={{ borderRadius: 2, textTransform: 'none' }}>
                            Отменить
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
        
        {/* ============================================ */}
        {/* ВКЛАДКА 3: НОВЫЙ СЕАНС - ЕДИНЫЙ СТИЛЬ */}
        {/* ============================================ */}
        {tabValue === 2 && (
          <Paper sx={{ p: 4, borderRadius: 3 }}>
            <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold', color: '#1a1a2e' }}>
              ➕ Начать новый сеанс
            </Typography>
            <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
              Текущий тариф: <strong>{stats.currentTariff} ₽/минута</strong> | Минимальная длительность — <strong>30 минут</strong>
            </Typography>
            
            <Divider sx={{ mb: 3 }} />
            
            <Grid container spacing={4}>
              <Grid item xs={12} md={7}>
                <Stepper activeStep={activeStep} orientation="vertical">
                  {steps.map((step, index) => (
                    <Step key={step.label}>
                      <StepLabel StepIconComponent={() => step.icon}>
                        <Typography variant="subtitle1" fontWeight="bold">{step.label}</Typography>
                        <Typography variant="caption" color="textSecondary">{step.description}</Typography>
                      </StepLabel>
                      <StepContent>
                        {index === 0 && (
                          <Box sx={{ py: 2 }}>
                            <TextField
                              fullWidth
                              label="Имя гостя"
                              placeholder="Введите имя гостя"
                              value={newSession.guestName}
                              onChange={(e) => { setNewSession({ ...newSession, guestName: e.target.value }); setErrors({ ...errors, guestName: '' }); }}
                              error={!!errors.guestName}
                              helperText={errors.guestName}
                              InputProps={{ startAdornment: <InputAdornment position="start"><Person color="primary" /></InputAdornment> }}
                            />
                            <TextField
                              fullWidth
                              label="Телефон"
                              placeholder="Введите номер телефона"
                              value={newSession.phone}
                              onChange={(e) => setNewSession({ ...newSession, phone: e.target.value })}
                              sx={{ mt: 2 }}
                              InputProps={{ startAdornment: <InputAdornment position="start"><Phone color="primary" /></InputAdornment> }}
                            />
                          </Box>
                        )}
                        
                        {index === 1 && (
                          <Box sx={{ py: 2 }}>
                            <TextField
                              select
                              fullWidth
                              label="Выберите зал"
                              value={newSession.roomId}
                              onChange={(e) => setNewSession({ ...newSession, roomId: e.target.value, tableNumber: '' })}
                              InputProps={{ startAdornment: <InputAdornment position="start"><MeetingRoom color="primary" /></InputAdornment> }}
                            >
                              {rooms.map((room) => (
                                <MenuItem key={room.id} value={room.id}>
                                  <Box display="flex" alignItems="center" justifyContent="space-between" sx={{ width: '100%' }}>
                                    <Box display="flex" alignItems="center">
                                      {getRoomIcon(room.type)}
                                      <Typography sx={{ ml: 1 }}>{room.name}</Typography>
                                    </Box>
                                    <Typography variant="caption" color="textSecondary">
                                      {room.capacity} мест
                                    </Typography>
                                  </Box>
                                </MenuItem>
                              ))}
                            </TextField>
                            
                            <TextField
                              select
                              fullWidth
                              label="Выберите стол"
                              value={newSession.tableNumber}
                              onChange={(e) => { setNewSession({ ...newSession, tableNumber: e.target.value }); setErrors({ ...errors, tableNumber: '' }); }}
                              error={!!errors.tableNumber}
                              helperText={errors.tableNumber}
                              sx={{ mt: 2 }}
                              InputProps={{ startAdornment: <InputAdornment position="start"><Chair color="primary" /></InputAdornment> }}
                            >
                              {availableTables.length === 0 ? (
                                <MenuItem disabled value="">Нет свободных столов</MenuItem>
                              ) : (
                                availableTables.map((table) => (
                                  <MenuItem key={table.id} value={table.tableNumber}>
                                    <Box display="flex" alignItems="center">
                                      <Chair sx={{ mr: 1 }} />
                                      <Typography>Стол {table.tableNumber}</Typography>
                                    </Box>
                                  </MenuItem>
                                ))
                              )}
                            </TextField>
                            
                            {availableTables.length > 0 && newSession.tableNumber && (
                              <Alert severity="success" sx={{ mt: 2 }}>
                                ✅ Стол {newSession.tableNumber} свободен в выбранное время
                              </Alert>
                            )}
                          </Box>
                        )}
                        
                        {index === 2 && (
                          <Box sx={{ py: 2 }}>
                            <Box display="flex" alignItems="center" sx={{ mb: 2, gap: 1 }}>
                              <TextField
                                type="date"
                                label="Дата"
                                value={newSession.startDate}
                                onChange={(e) => setNewSession({ ...newSession, startDate: e.target.value })}
                                InputLabelProps={{ shrink: true }}
                                sx={{ flex: 1 }}
                                InputProps={{ startAdornment: <InputAdornment position="start"><Event color="primary" /></InputAdornment> }}
                              />
                              <Button variant="outlined" onClick={setCurrentTime} sx={{ height: 56, minWidth: 100 }}>
                                🟢 Сейчас
                              </Button>
                            </Box>
                            <TextField
                              type="time"
                              fullWidth
                              label="Время начала"
                              value={newSession.startTime}
                              onChange={(e) => setNewSession({ ...newSession, startTime: e.target.value })}
                              InputLabelProps={{ shrink: true }}
                              sx={{ mb: 2 }}
                              InputProps={{ startAdornment: <InputAdornment position="start"><AccessTime color="primary" /></InputAdornment> }}
                            />
                            <TextField
                              type="number"
                              fullWidth
                              label="Длительность (минуты)"
                              value={newSession.durationMinutes}
                              onChange={(e) => {
                                const val = parseInt(e.target.value) || 60;
                                setNewSession({ ...newSession, durationMinutes: val });
                                if (val >= 30) setErrors({ ...errors, durationMinutes: '' });
                              }}
                              error={!!errors.durationMinutes}
                              helperText={errors.durationMinutes || `💰 Стоимость: ~${formatNumber(newSession.durationMinutes * stats.currentTariff)} ₽`}
                              inputProps={{ min: 30, step: 15 }}
                              InputProps={{ startAdornment: <InputAdornment position="start"><Timeline color="primary" /></InputAdornment> }}
                            />
                          </Box>
                        )}
                        
                        {index === 3 && (
                          <Box sx={{ py: 2 }}>
                            <Card sx={{ bgcolor: '#f8f9fa', borderRadius: 2 }}>
                              <CardContent>
                                <Typography variant="subtitle1" fontWeight="bold" gutterBottom>📋 Проверьте данные</Typography>
                                <Box display="grid" gridTemplateColumns={{ xs: '1fr', sm: '1fr 1fr' }} gap={1}>
                                  <Typography variant="body2" color="textSecondary">Имя:</Typography>
                                  <Typography variant="body2" fontWeight="500">{newSession.guestName || '—'}</Typography>
                                  <Typography variant="body2" color="textSecondary">Телефон:</Typography>
                                  <Typography variant="body2">{newSession.phone || '—'}</Typography>
                                  <Typography variant="body2" color="textSecondary">Зал:</Typography>
                                  <Typography variant="body2">{rooms.find(r => r.id === newSession.roomId)?.name || '—'}</Typography>
                                  <Typography variant="body2" color="textSecondary">Стол:</Typography>
                                  <Typography variant="body2" fontWeight="500" color="primary">{newSession.tableNumber ? `Стол ${newSession.tableNumber}` : '—'}</Typography>
                                  <Typography variant="body2" color="textSecondary">Дата:</Typography>
                                  <Typography variant="body2">{newSession.startDate}</Typography>
                                  <Typography variant="body2" color="textSecondary">Время:</Typography>
                                  <Typography variant="body2">{newSession.startTime}</Typography>
                                  <Typography variant="body2" color="textSecondary">Длительность:</Typography>
                                  <Typography variant="body2">{newSession.durationMinutes} мин</Typography>
                                  <Typography variant="body2" color="textSecondary">Стоимость:</Typography>
                                  <Typography variant="body2" fontWeight="bold" color="success.main">~{formatNumber(newSession.durationMinutes * stats.currentTariff)} ₽</Typography>
                                </Box>
                              </CardContent>
                            </Card>
                          </Box>
                        )}
                        
                        <Box sx={{ mt: 2 }}>
                          <Button 
                            variant="contained" 
                            onClick={() => setActiveStep((prev) => prev + 1)} 
                            disabled={index === 0 && !newSession.guestName} 
                            sx={{ mr: 1, borderRadius: 2, textTransform: 'none' }}
                          >
                            Продолжить
                          </Button>
                          {index > 0 && (
                            <Button onClick={() => setActiveStep((prev) => prev - 1)} sx={{ textTransform: 'none' }}>
                              Назад
                            </Button>
                          )}
                        </Box>
                      </StepContent>
                    </Step>
                  ))}
                </Stepper>
                
                {activeStep === steps.length && (
                  <Box sx={{ mt: 3 }}>
                    <Alert severity="info" sx={{ mb: 2 }}>🎉 Все данные заполнены! Нажмите "Начать сеанс" для подтверждения.</Alert>
                    <Button 
                      fullWidth 
                      variant="contained" 
                      color="primary" 
                      onClick={startSession} 
                      disabled={loading || !newSession.guestName || !newSession.tableNumber} 
                      size="large" 
                      startIcon={loading ? <CircularProgress size={20} /> : <CheckCircle />} 
                      sx={{ py: 1.5, borderRadius: 2, textTransform: 'none', fontWeight: 'bold' }}
                    >
                      {loading ? 'Создание...' : '✅ Начать сеанс'}
                    </Button>
                    <Button 
                      fullWidth 
                      onClick={() => setActiveStep(0)} 
                      sx={{ mt: 1, textTransform: 'none' }}
                    >
                      Назад к редактированию
                    </Button>
                  </Box>
                )}
              </Grid>
              
              <Grid item xs={12} md={5}>
                <Card sx={{ background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)', borderRadius: 3, color: 'white', boxShadow: 3 }}>
                  <CardContent>
                    <Typography variant="h6" gutterBottom sx={{ color: 'white', fontWeight: 'bold' }}>💡 Полезная информация</Typography>
                    <Divider sx={{ bgcolor: 'rgba(255,255,255,0.3)', my: 2 }} />
                    <Box display="flex" alignItems="center" sx={{ mb: 2 }}>
                      <AccessTime sx={{ mr: 2, color: 'white' }} />
                      <Box>
                        <Typography variant="body2" sx={{ color: 'white', fontWeight: 'bold' }}>Минимальная длительность</Typography>
                        <Typography variant="caption" sx={{ color: 'rgba(255,255,255,0.9)' }}>30 минут</Typography>
                      </Box>
                    </Box>
                    <Box display="flex" alignItems="center" sx={{ mb: 2 }}>
                      <AttachMoney sx={{ mr: 2, color: 'white' }} />
                      <Box>
                        <Typography variant="body2" sx={{ color: 'white', fontWeight: 'bold' }}>Текущий тариф</Typography>
                        <Typography variant="caption" sx={{ color: 'rgba(255,255,255,0.9)' }}>{stats.currentTariff} ₽/минута</Typography>
                      </Box>
                    </Box>
                    <Box display="flex" alignItems="center" sx={{ mb: 2 }}>
                      <People sx={{ mr: 2, color: 'white' }} />
                      <Box>
                        <Typography variant="body2" sx={{ color: 'white', fontWeight: 'bold' }}>Гостей сегодня</Typography>
                        <Typography variant="caption" sx={{ color: 'rgba(255,255,255,0.9)' }}>{formatNumber(stats.totalGuests)} человек</Typography>
                      </Box>
                    </Box>
                    <Box display="flex" alignItems="center">
                      <TrendingUp sx={{ mr: 2, color: 'white' }} />
                      <Box>
                        <Typography variant="body2" sx={{ color: 'white', fontWeight: 'bold' }}>Выручка сегодня</Typography>
                        <Typography variant="caption" sx={{ color: 'rgba(255,255,255,0.9)' }}>{formatNumber(stats.todayRevenue)} ₽</Typography>
                      </Box>
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Paper>
        )}
        
        {/* ============================================ */}
        {/* ВКЛАДКА 4: ОТЧЁТЫ */}
        {/* ============================================ */}
        {tabValue === 3 && (
          <Paper sx={{ p: 4, borderRadius: 3 }}>
            <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold' }}>📊 Отчёты</Typography>
            <Button 
              variant="contained" 
              startIcon={<TrendingUp />} 
              onClick={fetchReport} 
              disabled={loading} 
              sx={{ borderRadius: 2, textTransform: 'none', py: 1, px: 3 }}
            >
              {loading ? <CircularProgress size={24} /> : 'Отчёт за сегодня'}
            </Button>
            
            {reportData && showReport && (
              <Box sx={{ mt: 4 }}>
                <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 2 }}>📈 Выручка за сегодня</Typography>
                <Grid container spacing={3}>
                  <Grid item xs={12} sm={4}>
                    <Card sx={{ bgcolor: '#27ae60', color: 'white', borderRadius: 3 }}>
                      <CardContent>
                        <Typography variant="h4" sx={{ fontWeight: 'bold', color: 'white' }}>{formatNumber(reportData.totalRevenue)} ₽</Typography>
                        <Typography sx={{ color: 'rgba(255,255,255,0.9)' }}>Общая выручка</Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={12} sm={4}>
                    <Card sx={{ bgcolor: '#3498db', color: 'white', borderRadius: 3 }}>
                      <CardContent>
                        <Typography variant="h4" sx={{ fontWeight: 'bold', color: 'white' }}>{reportData.totalMinutes}</Typography>
                        <Typography sx={{ color: 'rgba(255,255,255,0.9)' }}>Всего минут</Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                  <Grid item xs={12} sm={4}>
                    <Card sx={{ bgcolor: '#e67e22', color: 'white', borderRadius: 3 }}>
                      <CardContent>
                        <Typography variant="h4" sx={{ fontWeight: 'bold', color: 'white' }}>{formatNumber(reportData.averageCheck)} ₽</Typography>
                        <Typography sx={{ color: 'rgba(255,255,255,0.9)' }}>Средний чек</Typography>
                      </CardContent>
                    </Card>
                  </Grid>
                </Grid>
                <Typography variant="subtitle1" sx={{ mt: 3 }}>📊 Всего сеансов: <strong>{reportData.sessionsCount}</strong></Typography>
                
                <Button variant="outlined" onClick={() => setShowReport(false)} sx={{ mt: 3, borderRadius: 2, textTransform: 'none' }}>
                  Скрыть отчёт
                </Button>
              </Box>
            )}
          </Paper>
        )}
      </Container>

      {/* Диалог чека */}
      <Dialog open={receiptDialogOpen} onClose={() => setReceiptDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ bgcolor: '#1a1a2e', color: 'white' }}>🧾 Чек об оплате</DialogTitle>
        <DialogContent id="receipt-content" sx={{ pt: 2 }}>
          {currentReceipt && (
            <Box sx={{ p: 3 }}>
              <Typography variant="h5" align="center" gutterBottom sx={{ fontWeight: 'bold' }}>🍵 Анти-кафе</Typography>
              <Typography variant="body2" align="center" color="textSecondary" gutterBottom>{currentReceipt.date || new Date().toLocaleString()}</Typography>
              <Divider sx={{ my: 2 }} />
              <Box display="grid" gridTemplateColumns="1fr 1fr" gap={1}>
                <Typography variant="body2" color="textSecondary">Гость:</Typography>
                <Typography variant="body2" fontWeight="500">{currentReceipt.guestName}</Typography>
                <Typography variant="body2" color="textSecondary">Телефон:</Typography>
                <Typography variant="body2">{currentReceipt.phone || '—'}</Typography>
                <Typography variant="body2" color="textSecondary">Стол:</Typography>
                <Typography variant="body2">{currentReceipt.tableNumber}</Typography>
                <Typography variant="body2" color="textSecondary">Начало:</Typography>
                <Typography variant="body2">{formatDateTime(currentReceipt.startTime)}</Typography>
                <Typography variant="body2" color="textSecondary">Окончание:</Typography>
                <Typography variant="body2">{currentReceipt.endTime}</Typography>
                <Typography variant="body2" color="textSecondary">Длительность:</Typography>
                <Typography variant="body2">{currentReceipt.hours} ч {currentReceipt.minutes} мин</Typography>
              </Box>
              <Divider sx={{ my: 2 }} />
              <Box display="flex" justifyContent="space-between" alignItems="center">
                <Typography variant="body2" color="textSecondary">Тариф:</Typography>
                <Typography>{currentReceipt.tariffRate} ₽/мин</Typography>
              </Box>
              <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mt: 1 }}>
                <Typography variant="h6" fontWeight="bold">ИТОГО:</Typography>
                <Typography variant="h5" fontWeight="bold" color="success.main">{formatNumber(currentReceipt.totalCost)} ₽</Typography>
              </Box>
              <Divider sx={{ my: 2 }} />
              <Typography variant="body2" align="center" color="textSecondary">{currentReceipt.message}</Typography>
              <Typography variant="body2" align="center" color="textSecondary" sx={{ mt: 1 }}>Спасибо за посещение!</Typography>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={printReceipt} startIcon={<Print />} sx={{ borderRadius: 2 }}>Печать</Button>
          <Button onClick={() => setReceiptDialogOpen(false)} variant="contained" sx={{ borderRadius: 2 }}>Закрыть</Button>
        </DialogActions>
      </Dialog>
    </>
  );
}