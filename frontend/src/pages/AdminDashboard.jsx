import React, { useState, useEffect } from 'react';
import axios from 'axios';
import {
  AppBar, Toolbar, Typography, Container, Grid, Card, CardContent,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Button, Paper, TextField, Alert, Tabs, Tab, IconButton, Box, Chip,
  Dialog, DialogTitle, DialogContent, DialogActions,
  FormControl, InputLabel, Select, MenuItem, FormHelperText,
  CircularProgress, Divider, Avatar,
  Stepper, Step, StepLabel, StepContent
} from '@mui/material';
import {
  Refresh, Logout, Person, TableRestaurant, TrendingUp,
  AccessTime, Phone, Event, Print, CheckCircle,
  People, MeetingRoom, AttachMoney,
  Star, SportsEsports, Settings, Save
} from '@mui/icons-material';

const API = 'http://localhost:5154/api';

export default function AdminDashboard({ user, onLogout }) {
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
    durationMinutes: 30
  });
  
  const [receiptDialogOpen, setReceiptDialogOpen] = useState(false);
  const [currentReceipt, setCurrentReceipt] = useState(null);
  const [reportData, setReportData] = useState(null);
  const [showReport, setShowReport] = useState(false);
  const [errors, setErrors] = useState({});

  // Настройки
  const [tempPrice, setTempPrice] = useState('3.5');
  const [hasChanges, setHasChanges] = useState(false);
  
  const [roomsList, setRoomsList] = useState([]);
  const [tablesList, setTablesList] = useState([]);
  const [newRoomName, setNewRoomName] = useState('');
  const [newRoomType, setNewRoomType] = useState('usual');
  const [newTableCount, setNewTableCount] = useState(1);
  const [selectedRoomId, setSelectedRoomId] = useState(1);

  const showMessage = (text, type) => {
    setMessage({ text, type, open: true });
    setTimeout(() => setMessage(prev => ({ ...prev, open: false })), 4000);
  };

  // Нормализация данных - ПРИВОДИМ К ЕДИНОМУ ФОРМАТУ
  const normalizeRoom = (room) => ({
    id: room.id ?? room.Id,
    name: room.name ?? room.Name,
    type: room.type ?? room.Type,
    isActive: room.isActive ?? room.IsActive,
    capacity: room.capacity ?? room.Capacity,
    createdAt: room.createdAt ?? room.CreatedAt
  });

  const normalizeTable = (table) => ({
    id: table.id ?? table.Id,
    roomId: table.roomId ?? table.RoomId,
    tableNumber: table.tableNumber ?? table.TableNumber,
    capacity: table.capacity ?? table.Capacity,
    isActive: table.isActive ?? table.IsActive,
    roomName: table.room?.name ?? table.Room?.Name ?? ''
  });

  // ЗАГРУЗКА ВСЕХ ДАННЫХ
  const loadAllData = async () => {
    setLoading(true);
    try {
      await Promise.all([
        fetchRoomsList(),
        fetchTablesList(),
        fetchRoomsFromApi(),
        fetchTablesFromApi(),
        fetchActiveSessions(),
        fetchBookings(),
        fetchTodayStats(),
        fetchSettings()
      ]);
    } catch (err) {
      console.error('Ошибка загрузки:', err);
      showMessage('Ошибка загрузки данных', 'error');
    } finally {
      setLoading(false);
      setHasChanges(false);
    }
  };

  const fetchSettings = async () => {
    try {
      const res = await axios.get(`${API}/settings`);
      if (res.data.PricePerMinute) {
        const price = parseFloat(res.data.PricePerMinute);
        setStats(prev => ({ ...prev, currentTariff: price }));
        setTempPrice(price.toString());
      }
    } catch (err) { 
      console.error(err);
    }
  };

  const saveAllSettings = async () => {
    setLoading(true);
    try {
      const price = parseFloat(tempPrice);
      if (!isNaN(price) && price >= 0.1) {
        await axios.post(`${API}/settings`, { key: 'PricePerMinute', value: price.toString() });
      }
      
      showMessage('✅ Все настройки сохранены', 'success');
      await fetchSettings();
      setHasChanges(false);
    } catch (err) { 
      showMessage('Ошибка при сохранении', 'error'); 
    } finally {
      setLoading(false);
    }
  };

  const fetchRoomsList = async () => {
    try {
      const res = await axios.get(`${API}/settings/rooms`);
      const normalized = (res.data || []).map(normalizeRoom);
      setRoomsList(normalized);
      console.log('Загружены залы:', normalized);
    } catch (err) { 
      console.error(err);
    }
  };

  const fetchTablesList = async () => {
    try {
      const res = await axios.get(`${API}/settings/tables`);
      const normalized = (res.data || []).map(normalizeTable);
      setTablesList(normalized);
      console.log('Загружены столы:', normalized);
    } catch (err) { 
      console.error(err);
    }
  };

  const getTableCountForRoom = (roomId) => {
    return tablesList.filter(t => t.roomId === roomId).length;
  };

  const addRoom = async () => {
    if (!newRoomName) { showMessage('Введите название зала', 'error'); return; }
    if (newTableCount < 1) { showMessage('Количество столов должно быть не менее 1', 'error'); return; }
    setLoading(true);
    try {
      const response = await axios.post(`${API}/settings/rooms`, { 
        name: newRoomName, 
        type: newRoomType, 
        tableCount: newTableCount 
      });
      if (response.data.success) {
        showMessage(`✅ ${response.data.message}`, 'success');
        await loadAllData();
        setNewRoomName('');
        setNewTableCount(1);
      } else {
        showMessage(response.data.error || 'Ошибка', 'error');
      }
    } catch (err) { 
      showMessage('Ошибка при добавлении зала', 'error'); 
    } finally {
      setLoading(false);
    }
  };

  const deleteRoom = async (id) => {
    if (id === 1) {
      showMessage('Нельзя удалить основной зал', 'error');
      return;
    }
    if (!window.confirm('Удалить зал? Все столы в нём будут удалены!')) return;
    setLoading(true);
    try {
      const response = await axios.delete(`${API}/settings/rooms/${id}`);
      if (response.data.success) {
        showMessage('✅ Зал удалён', 'success');
        await loadAllData();
      } else {
        showMessage(response.data.error || 'Ошибка', 'error');
      }
    } catch (err) { 
      showMessage('Ошибка при удалении зала', 'error'); 
    } finally {
      setLoading(false);
    }
  };

  const addTable = async () => {
    setLoading(true);
    try {
      const response = await axios.post(`${API}/settings/tables`, { roomId: selectedRoomId });
      if (response.data.success) {
        showMessage(`✅ ${response.data.message}`, 'success');
        await loadAllData();
      } else {
        showMessage(response.data.error || 'Ошибка', 'error');
      }
    } catch (err) { 
      showMessage('Ошибка при добавлении стола', 'error'); 
    } finally {
      setLoading(false);
    }
  };

  const deleteTable = async (id, tableNumber) => {
    if (!window.confirm(`Удалить стол ${tableNumber}?`)) return;
    setLoading(true);
    try {
      const response = await axios.delete(`${API}/settings/tables/${id}`);
      if (response.data.success) {
        showMessage(response.data.message || '✅ Стол удалён', 'success');
        await loadAllData();
      } else {
        showMessage(response.data.error || 'Ошибка', 'error');
      }
    } catch (err) { 
      showMessage('Ошибка при удалении стола', 'error'); 
    } finally {
      setLoading(false);
    }
  };

  const fetchActiveSessions = async () => {
    try {
      const res = await axios.get(`${API}/session/active`);
      setActiveSessions(res.data);
      setStats(prev => ({ ...prev, activeCount: res.data.length }));
    } catch (err) { console.error(err); }
  };

  const fetchBookings = async () => {
    try {
      const res = await axios.get(`${API}/booking/active`);
      setBookings(res.data);
    } catch (err) { console.error(err); }
  };

  const fetchTablesFromApi = async () => {
    try {
      const res = await axios.get(`${API}/session/tables`);
      setTables(res.data);
    } catch (err) { console.error(err); }
  };

  const fetchRoomsFromApi = async () => {
    try {
      const res = await axios.get(`${API}/session/rooms`);
      setRooms(res.data);
    } catch (err) { console.error(err); }
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
    } catch (err) { console.error(err); }
  };

  const fetchAvailableTables = async () => {
    if (!newSession.startDate || !newSession.startTime || !newSession.durationMinutes) return;
    const startDateTime = new Date(`${newSession.startDate}T${newSession.startTime}`);
    try {
        const res = await axios.get(`${API}/session/available-tables`, {
            params: {
                startTime: startDateTime.toISOString(),
                durationMinutes: newSession.durationMinutes,
                roomId: newSession.roomId 
            }
        });
        setAvailableTables(res.data || []);
    } catch (err) { console.error(err); }
  };

  useEffect(() => {
    loadAllData();
  }, []);

  useEffect(() => {
    const interval = setInterval(() => {
      const newRemaining = {};
      activeSessions.forEach(session => {
        if (session.isActive && session.plannedDurationMinutes) {
          const startTime = new Date(session.startTime);
          const endTime = new Date(startTime.getTime() + session.plannedDurationMinutes * 60000);
          const remaining = Math.max(0, Math.floor((endTime - new Date()) / 60000));
          newRemaining[session.id] = remaining;
        }
      });
      setRemainingTimes(newRemaining);
    }, 1000);
    return () => clearInterval(interval);
  }, [activeSessions]);

  useEffect(() => {
    if (newSession.startDate && newSession.startTime && newSession.durationMinutes) {
      const timer = setTimeout(() => fetchAvailableTables(), 500);
      return () => clearTimeout(timer);
    }
  }, [newSession.startDate, newSession.startTime, newSession.durationMinutes, newSession.roomId]);

  const validateForm = () => {
    const newErrors = {};
    if (!newSession.guestName.trim()) newErrors.guestName = 'Введите имя гостя';
    if (!newSession.tableNumber) newErrors.tableNumber = 'Выберите стол';
    if (newSession.durationMinutes < 30) newErrors.durationMinutes = 'Минимальная длительность - 30 минут';
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const setCurrentTime = () => {
    const now = new Date();
    setNewSession({
      ...newSession,
      startDate: now.toISOString().split('T')[0],
      startTime: now.toTimeString().slice(0, 5)
    });
  };

  const startSession = async () => {
    if (!validateForm()) return;
    setLoading(true);
    try {
      const startDateTime = new Date(`${newSession.startDate}T${newSession.startTime}`);
      await axios.post(`${API}/session/start`, {
        guestName: newSession.guestName,
        phone: newSession.phone,
        tableNumber: parseInt(newSession.tableNumber),
        roomId: newSession.roomId,
        startTime: startDateTime.toISOString(),
        durationMinutes: newSession.durationMinutes
      });
      showMessage(`✅ Сеанс начат!`, 'success');
      setNewSession({
        guestName: '', phone: '', tableNumber: '', roomId: 1,
        startDate: new Date().toISOString().split('T')[0],
        startTime: new Date().toTimeString().slice(0, 5),
        durationMinutes: 30
      });
      setActiveStep(0);
      await loadAllData();
    } catch (err) {
      showMessage(err.response?.data?.error || 'Ошибка', 'error');
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
      await loadAllData();
    } catch (err) {
      showMessage('Ошибка', 'error');
    } finally {
      setLoading(false);
    }
  };

  const cancelBooking = async (id, guestName) => {
    if (!window.confirm(`Отменить бронь для "${guestName}"?`)) return;
    try {
      await axios.delete(`${API}/booking/cancel/${id}`);
      await fetchBookings();
      showMessage('✅ Бронь отменена', 'success');
    } catch (err) {
      showMessage('Ошибка', 'error');
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
    const printContent = document.getElementById('receipt-content');
    const printWindow = window.open('', '_blank');
    printWindow.document.write(`
      <html><head><title>Чек</title>
      <style>body{font-family:Arial;padding:20px}</style>
      </head><body>${printContent.innerHTML}<script>window.print();window.close();<\/script></body></html>
    `);
    printWindow.document.close();
  };

  const formatNumber = (num) => num?.toLocaleString('ru-RU') || '0';
  const formatTime = (dateStr) => dateStr ? new Date(dateStr).toLocaleTimeString('ru-RU', { hour:'2-digit', minute:'2-digit' }) : '—';
  const formatDateTime = (dateStr) => dateStr ? new Date(dateStr).toLocaleString('ru-RU') : '—';
  const formatRemainingTime = (minutes) => {
    if (minutes <= 0) return 'Время вышло';
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return hours > 0 ? `${hours}ч ${mins}мин` : `${mins} мин`;
  };

  const StatCard = ({ title, value, icon, color }) => (
    <Card sx={{ background: color, borderRadius: 3, color: 'white' }}>
      <CardContent>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Box>
            <Typography variant="h3" sx={{ fontWeight: 'bold', color: 'white', fontSize: { xs: '1.5rem', md: '2.5rem' } }}>
              {typeof value === 'number' ? formatNumber(value) : value}
            </Typography>
            <Typography variant="body2" sx={{ opacity: 0.9, color: 'white' }}>{title}</Typography>
          </Box>
          <Avatar sx={{ bgcolor: 'rgba(255,255,255,0.2)', width: 56, height: 56 }}>{icon}</Avatar>
        </Box>
      </CardContent>
    </Card>
  );

  const steps = [
    { label: 'Информация о госте', icon: <Person />, description: 'Введите имя и телефон' },
    { label: 'Выбор места', icon: <MeetingRoom />, description: 'Выберите зал и стол' },
    { label: 'Время и длительность', icon: <AccessTime />, description: 'Укажите дату, время' },
    { label: 'Подтверждение', icon: <CheckCircle />, description: 'Проверьте данные' }
  ];

  return (
    <>
      <AppBar position="static" sx={{ bgcolor: '#1a1a2e' }}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1, fontWeight: 'bold' }}>🍵 Анти-кафе | {user?.fullName}</Typography>
          <Chip label="Администратор" size="small" sx={{ mr: 2, bgcolor: '#e94560', color: 'white' }} />
          <IconButton color="inherit" onClick={loadAllData}><Refresh /></IconButton>
          <Button color="inherit" onClick={onLogout} startIcon={<Logout />}>Выйти</Button>
        </Toolbar>
      </AppBar>
      
      <Container sx={{ mt: 3, mb: 4, maxWidth: '1400px' }}>
        {message.open && <Alert severity={message.type} onClose={() => setMessage({ ...message, open: false })} sx={{ mb: 2 }}>{message.text}</Alert>}
        
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard title="Активных сеансов" value={stats.activeCount} icon={<Person sx={{ fontSize: 32 }} />} color="linear-gradient(135deg, #667eea, #764ba2)" />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard title="Выручка сегодня" value={`${formatNumber(stats.todayRevenue)} ₽`} icon={<AttachMoney sx={{ fontSize: 32 }} />} color="linear-gradient(135deg, #f093fb, #f5576c)" />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard title="Гостей сегодня" value={stats.totalGuests} icon={<People sx={{ fontSize: 32 }} />} color="linear-gradient(135deg, #4facfe, #00f2fe)" />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <StatCard title="Цена за минуту" value={`${stats.currentTariff} ₽`} icon={<AccessTime sx={{ fontSize: 32 }} />} color="linear-gradient(135deg, #fa709a, #fee140)" />
          </Grid>
        </Grid>
        
        <Paper sx={{ mb: 2 }}>
          <Tabs value={tabValue} onChange={(e, v) => setTabValue(v)} variant="scrollable">
            <Tab icon={<Person />} label="Активные сеансы" />
            <Tab icon={<TableRestaurant />} label="Бронирования" />
            <Tab icon={<Event />} label="Новый сеанс" />
            <Tab icon={<TrendingUp />} label="Отчёты" />
            <Tab icon={<Settings />} label="Настройки" />
          </Tabs>
        </Paper>
        
        {tabValue === 0 && (
          <TableContainer component={Paper}>
            <Table>
              <TableHead sx={{ bgcolor: '#f5f5f5' }}>
                <TableRow>
                  <TableCell><b>👤 Гость</b></TableCell>
                  <TableCell><b>📞 Телефон</b></TableCell>
                  <TableCell><b>🏠 Зал</b></TableCell>
                  <TableCell><b>🪑 Стол</b></TableCell>
                  <TableCell><b>🕐 Начало</b></TableCell>
                  <TableCell><b>⏱️ Осталось</b></TableCell>
                  <TableCell><b>💰 Сумма</b></TableCell>
                  <TableCell><b>⚡ Действие</b></TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {activeSessions.length === 0 ? (
                  <TableRow><TableCell colSpan={8} align="center" sx={{ py: 8 }}>Нет активных сеансов</TableCell></TableRow>
                ) : (
                  activeSessions.map(s => {
                    const remaining = remainingTimes[s.id] || s.plannedDurationMinutes;
                    return (
                      <TableRow key={s.id}>
                        <TableCell><Box display="flex" alignItems="center"><Avatar sx={{ width: 32, height: 32, mr: 1, bgcolor: '#667eea' }}><Person fontSize="small" /></Avatar>{s.guestName}</Box></TableCell>
                        <TableCell>{s.phone || '—'}</TableCell>
                        <TableCell>{rooms.find(r => r.id === s.roomId)?.name || 'Основной'}</TableCell>
                        <TableCell><Chip label={`Стол ${s.tableNumber}`} size="small" /></TableCell>
                        <TableCell>{formatTime(s.startTime)}</TableCell>
                        <TableCell><Typography color={remaining < 5 ? 'error' : 'primary'} fontWeight="bold">{formatRemainingTime(remaining)}</Typography></TableCell>
                        <TableCell>{formatNumber(s.totalCost)} ₽</TableCell>
                        <TableCell><Button size="small" variant="contained" color="error" onClick={() => endSession(s.id, s.guestName)}>Завершить</Button></TableCell>
                      </TableRow>
                    );
                  })
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}
        
        {tabValue === 1 && (
          <TableContainer component={Paper}>
            <Table>
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
                  <TableRow><TableCell colSpan={8} align="center">Нет активных бронирований</TableCell></TableRow>
                ) : (
                  bookings.map(b => (
                    <TableRow key={b.id}>
                      <TableCell>{b.guestName}</TableCell>
                      <TableCell>{b.phone}</TableCell>
                      <TableCell>{rooms.find(r => r.id === b.roomId)?.name || 'Основной'}</TableCell>
                      <TableCell>Стол {b.tableNumber}</TableCell>
                      <TableCell>{new Date(b.bookingDate).toLocaleDateString('ru-RU')}</TableCell>
                      <TableCell>{b.startTime}</TableCell>
                      <TableCell>{b.durationMinutes} мин</TableCell>
                      <TableCell><Button size="small" variant="outlined" color="error" onClick={() => cancelBooking(b.id, b.guestName)}>Отменить</Button></TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </TableContainer>
        )}
        
        {tabValue === 2 && (
          <Paper sx={{ p: 4 }}>
            <Typography variant="h5" gutterBottom>➕ Начать новый сеанс</Typography>
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
                            <TextField fullWidth label="Имя гостя" value={newSession.guestName} onChange={(e) => setNewSession({ ...newSession, guestName: e.target.value })} error={!!errors.guestName} helperText={errors.guestName} sx={{ mb: 2 }} />
                            <TextField fullWidth label="Телефон" value={newSession.phone} onChange={(e) => setNewSession({ ...newSession, phone: e.target.value })} />
                          </Box>
                        )}
                        {index === 1 && (
                          <Box sx={{ py: 2 }}>
                            <FormControl fullWidth sx={{ mb: 2 }}>
                              <InputLabel>Зал</InputLabel>
                              <Select value={newSession.roomId} onChange={(e) => setNewSession({ ...newSession, roomId: e.target.value, tableNumber: '' })} label="Зал">
                                {rooms.map(room => <MenuItem key={room.id} value={room.id}>{room.name} ({getTableCountForRoom(room.id)} столов)</MenuItem>)}
                              </Select>
                            </FormControl>
                            <FormControl fullWidth error={!!errors.tableNumber}>
                              <InputLabel>Стол</InputLabel>
                              <Select value={newSession.tableNumber} onChange={(e) => setNewSession({ ...newSession, tableNumber: e.target.value })} label="Стол">
                                {availableTables.length === 0 ? <MenuItem disabled value="">Нет свободных столов</MenuItem> : availableTables.map(t => <MenuItem key={t.id} value={t.tableNumber}>Стол {t.tableNumber}</MenuItem>)}
                              </Select>
                              {errors.tableNumber && <FormHelperText>{errors.tableNumber}</FormHelperText>}
                            </FormControl>
                          </Box>
                        )}
                        {index === 2 && (
                          <Box sx={{ py: 2 }}>
                            <Box display="flex" gap={1} sx={{ mb: 2 }}>
                              <TextField type="date" label="Дата" value={newSession.startDate} onChange={(e) => setNewSession({ ...newSession, startDate: e.target.value })} InputLabelProps={{ shrink: true }} fullWidth />
                              <Button variant="outlined" onClick={setCurrentTime}>Сейчас</Button>
                            </Box>
                            <TextField type="time" fullWidth label="Время начала" value={newSession.startTime} onChange={(e) => setNewSession({ ...newSession, startTime: e.target.value })} InputLabelProps={{ shrink: true }} sx={{ mb: 2 }} />
                            <TextField type="number" fullWidth label="Длительность (минуты)" value={newSession.durationMinutes} onChange={(e) => setNewSession({ ...newSession, durationMinutes: parseInt(e.target.value) || 30 })} error={!!errors.durationMinutes} helperText={errors.durationMinutes || `💰 Стоимость: ~${formatNumber(newSession.durationMinutes * stats.currentTariff)} ₽`} inputProps={{ min: 30, step: 15 }} />
                          </Box>
                        )}
                        {index === 3 && (
                          <Box sx={{ py: 2 }}>
                            <Card sx={{ bgcolor: '#f8f9fa' }}>
                              <CardContent>
                                <Typography variant="subtitle1" fontWeight="bold">📋 Проверьте данные</Typography>
                                <Box display="grid" gridTemplateColumns="1fr 1fr" gap={1} sx={{ mt: 1 }}>
                                  <Typography color="textSecondary">Имя:</Typography><Typography fontWeight="500">{newSession.guestName || '—'}</Typography>
                                  <Typography color="textSecondary">Телефон:</Typography><Typography>{newSession.phone || '—'}</Typography>
                                  <Typography color="textSecondary">Зал:</Typography><Typography>{rooms.find(r => r.id === newSession.roomId)?.name || '—'}</Typography>
                                  <Typography color="textSecondary">Стол:</Typography><Typography color="primary">{newSession.tableNumber ? `Стол ${newSession.tableNumber}` : '—'}</Typography>
                                  <Typography color="textSecondary">Дата/Время:</Typography><Typography>{newSession.startDate} {newSession.startTime}</Typography>
                                  <Typography color="textSecondary">Длительность:</Typography><Typography>{newSession.durationMinutes} мин</Typography>
                                  <Typography color="textSecondary">Стоимость:</Typography><Typography color="success.main">~{formatNumber(newSession.durationMinutes * stats.currentTariff)} ₽</Typography>
                                </Box>
                              </CardContent>
                            </Card>
                          </Box>
                        )}
                        <Box sx={{ mt: 2 }}>
                          <Button variant="contained" onClick={() => setActiveStep(prev => prev + 1)} disabled={index === 0 && !newSession.guestName} sx={{ mr: 1 }}>Продолжить</Button>
                          {index > 0 && <Button onClick={() => setActiveStep(prev => prev - 1)}>Назад</Button>}
                        </Box>
                      </StepContent>
                    </Step>
                  ))}
                </Stepper>
                {activeStep === steps.length && (
                  <Box sx={{ mt: 3 }}>
                    <Alert severity="info" sx={{ mb: 2 }}>Все данные заполнены!</Alert>
                    <Button fullWidth variant="contained" onClick={startSession} disabled={loading || !newSession.guestName || !newSession.tableNumber} size="large" startIcon={loading ? <CircularProgress size={20} /> : <CheckCircle />}>Начать сеанс</Button>
                    <Button fullWidth onClick={() => setActiveStep(0)} sx={{ mt: 1 }}>Назад</Button>
                  </Box>
                )}
              </Grid>
              <Grid item xs={12} md={5}>
                <Card sx={{ background: 'linear-gradient(135deg, #667eea, #764ba2)', borderRadius: 3, color: 'white' }}>
                  <CardContent>
                    <Typography variant="h6" gutterBottom>💡 Информация</Typography>
                    <Divider sx={{ bgcolor: 'rgba(255,255,255,0.3)', my: 2 }} />
                    <Typography>Минимальная длительность: 30 минут</Typography>
                    <Typography sx={{ mt: 1 }}>Цена: {stats.currentTariff} ₽/минута</Typography>
                    <Typography sx={{ mt: 1 }}>Гостей сегодня: {formatNumber(stats.totalGuests)}</Typography>
                    <Typography sx={{ mt: 1 }}>Выручка: {formatNumber(stats.todayRevenue)} ₽</Typography>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Paper>
        )}
        
        {tabValue === 3 && (
          <Paper sx={{ p: 4 }}>
            <Typography variant="h5" gutterBottom>📊 Отчёты</Typography>
            <Button variant="contained" onClick={fetchReport} disabled={loading}>Отчёт за сегодня</Button>
            {reportData && showReport && (
              <Box sx={{ mt: 4 }}>
                <Typography variant="h6">📈 Выручка за сегодня</Typography>
                <Grid container spacing={2} sx={{ mt: 1 }}>
                  <Grid item xs={4}><Card sx={{ bgcolor: '#27ae60', color: 'white', p: 2 }}><Typography variant="h4">{formatNumber(reportData.totalRevenue)} ₽</Typography><Typography>Выручка</Typography></Card></Grid>
                  <Grid item xs={4}><Card sx={{ bgcolor: '#3498db', color: 'white', p: 2 }}><Typography variant="h4">{reportData.totalMinutes}</Typography><Typography>Минут</Typography></Card></Grid>
                  <Grid item xs={4}><Card sx={{ bgcolor: '#e67e22', color: 'white', p: 2 }}><Typography variant="h4">{formatNumber(reportData.averageCheck)} ₽</Typography><Typography>Средний чек</Typography></Card></Grid>
                </Grid>
                <Typography sx={{ mt: 2 }}>Всего сеансов: <strong>{reportData.sessionsCount}</strong></Typography>
                <Button variant="outlined" onClick={() => setShowReport(false)} sx={{ mt: 2 }}>Скрыть</Button>
              </Box>
            )}
          </Paper>
        )}
        
        {tabValue === 4 && (
          <Paper sx={{ p: 4 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
              <Typography variant="h5">⚙️ Настройки системы</Typography>
              <Button 
                variant="contained" 
                onClick={saveAllSettings} 
                disabled={!hasChanges || loading}
                startIcon={<Save />}
                color="primary"
                size="large"
              >
                Сохранить все изменения
              </Button>
            </Box>
            
            <Card sx={{ p: 2, mb: 3 }}>
              <Typography variant="h6">💰 Цены</Typography>
              <Box display="flex" gap={2} alignItems="center" sx={{ mt: 1, flexWrap: 'wrap' }}>
                <TextField 
                  label="Цена за минуту ₽" 
                  type="number" 
                  value={tempPrice} 
                  onChange={(e) => {
                    setTempPrice(e.target.value);
                    setHasChanges(true);
                  }} 
                  inputProps={{ step: 0.1, min: 0.1 }} 
                  sx={{ width: 200 }} 
                />
              </Box>
            </Card>

            <Card sx={{ p: 2 }}>
              <Typography variant="h6">🏢 Управление залами и столами</Typography>
              
              <Box display="flex" gap={2} sx={{ mb: 3, flexWrap: 'wrap' }}>
                <TextField size="small" label="Название зала" value={newRoomName} onChange={(e) => setNewRoomName(e.target.value)} sx={{ width: 180 }} />
                <FormControl size="small" sx={{ width: 100 }}>
                  <Select value={newRoomType} onChange={(e) => setNewRoomType(e.target.value)}>
                    <MenuItem value="usual">Обычный</MenuItem>
                    <MenuItem value="vip">VIP</MenuItem>
                  </Select>
                </FormControl>
                <TextField size="small" label="Количество столов" type="number" value={newTableCount} onChange={(e) => setNewTableCount(parseInt(e.target.value) || 1)} sx={{ width: 150 }} />
                <Button variant="outlined" onClick={addRoom} disabled={loading}>➕ Добавить зал</Button>
              </Box>

              <Typography variant="subtitle1">📋 Существующие залы:</Typography>
              <TableContainer sx={{ mb: 3 }}>
                <Table size="small">
                  <TableHead>
                    <TableRow sx={{ bgcolor: '#f5f5f5' }}>
                      <TableCell><b>Название</b></TableCell>
                      <TableCell><b>Тип</b></TableCell>
                      <TableCell><b>Столов</b></TableCell>
                      <TableCell><b>Действие</b></TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {roomsList.length === 0 ? (
                      <TableRow><TableCell colSpan={4} align="center">Нет залов</TableCell></TableRow>
                    ) : (
                      roomsList.map(room => {
                        const tableCount = getTableCountForRoom(room.id);
                        return (
                          <TableRow key={room.id}>
                            <TableCell>{room.name}</TableCell>
                            <TableCell>
                              <Chip label={room.type === 'vip' ? 'VIP' : 'Обычный'} size="small" color={room.type === 'vip' ? 'warning' : 'default'} />
                            </TableCell>
                            <TableCell><b>{tableCount}</b></TableCell>
                            <TableCell>
                              <Button 
                                size="small" 
                                color="error" 
                                variant="outlined" 
                                onClick={() => deleteRoom(room.id)} 
                                disabled={room.id === 1}
                              >
                                🗑️ Удалить зал
                              </Button>
                            </TableCell>
                          </TableRow>
                        );
                      })
                    )}
                  </TableBody>
                </Table>
              </TableContainer>

              <Divider sx={{ my: 2 }} />

              <Typography variant="subtitle1">➕ Добавить стол в зал:</Typography>
              <Box display="flex" gap={2} sx={{ mb: 3 }}>
                <FormControl size="small" sx={{ width: 200 }}>
                  <Select value={selectedRoomId} onChange={(e) => setSelectedRoomId(e.target.value)}>
                    {roomsList.map(room => <MenuItem key={room.id} value={room.id}>{room.name}</MenuItem>)}
                  </Select>
                </FormControl>
                <Button variant="outlined" onClick={addTable} disabled={loading}>➕ Добавить стол</Button>
              </Box>

              <Typography variant="subtitle1">📋 Все столы:</Typography>
              <TableContainer>
                <Table size="small">
                  <TableHead>
                    <TableRow sx={{ bgcolor: '#f5f5f5' }}>
                      <TableCell><b>Зал</b></TableCell>
                      <TableCell><b>№ стола</b></TableCell>
                      <TableCell><b>Действие</b></TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {tablesList.length === 0 ? (
                      <TableRow><TableCell colSpan={3} align="center">Нет столов</TableCell></TableRow>
                    ) : (
                      tablesList.map(table => {
                        const room = roomsList.find(r => r.id === table.roomId);
                        return (
                          <TableRow key={table.id}>
                            <TableCell>{room?.name || '—'}</TableCell>
                            <TableCell>Стол {table.tableNumber}</TableCell>
                            <TableCell>
                              <Button 
                                size="small" 
                                color="error" 
                                variant="outlined" 
                                onClick={() => deleteTable(table.id, table.tableNumber)}
                              >
                                🗑️ Удалить
                              </Button>
                            </TableCell>
                          </TableRow>
                        );
                      })
                    )}
                  </TableBody>
                </Table>
              </TableContainer>
            </Card>
          </Paper>
        )}
      </Container>

      <Dialog open={receiptDialogOpen} onClose={() => setReceiptDialogOpen(false)} maxWidth="sm">
        <DialogTitle sx={{ bgcolor: '#1a1a2e', color: 'white' }}>🧾 Чек об оплате</DialogTitle>
        <DialogContent id="receipt-content">
          {currentReceipt && (
            <Box sx={{ p: 2 }}>
              <Typography variant="h5" align="center">🍵 Анти-кафе</Typography>
              <Divider sx={{ my: 1 }} />
              <Typography>Гость: {currentReceipt.guestName}</Typography>
              <Typography>Стол: {currentReceipt.tableNumber}</Typography>
              <Typography>Начало: {formatDateTime(currentReceipt.startTime)}</Typography>
              <Typography>Окончание: {formatDateTime(currentReceipt.endTime)}</Typography>
              <Typography>Длительность: {currentReceipt.hours}ч {currentReceipt.minutes}мин</Typography>
              <Typography>Тариф: {currentReceipt.tariffRate} ₽/мин</Typography>
              <Divider sx={{ my: 1 }} />
              <Typography variant="h5" align="right" color="success.main">ИТОГО: {formatNumber(currentReceipt.totalCost)} ₽</Typography>
              <Typography align="center" sx={{ mt: 2 }}>{currentReceipt.message}</Typography>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={printReceipt} startIcon={<Print />}>Печать</Button>
          <Button onClick={() => setReceiptDialogOpen(false)} variant="contained">Закрыть</Button>
        </DialogActions>
      </Dialog>
    </>
  );
}