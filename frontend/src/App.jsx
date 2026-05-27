import React, { useState, useEffect } from 'react';
import Login from './pages/Login';
import AdminDashboard from './pages/AdminDashboard';
import { ThemeProvider, createTheme } from '@mui/material/styles';

const adminTheme = createTheme({
  palette: {
    primary: { main: '#2c3e50' },
    secondary: { main: '#e74c3c' },
    background: { default: '#ecf0f1' }
  },
  typography: {
    fontFamily: '"Segoe UI", "Roboto", sans-serif'
  }
});

function App() {
  const [user, setUser] = useState(null);

  useEffect(() => {
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
      setUser(JSON.parse(savedUser));
    }
  }, []);

  if (!user) {
    return <Login onLogin={setUser} />;
  }

  return (
    <ThemeProvider theme={adminTheme}>
      <AdminDashboard user={user} onLogout={() => {
        localStorage.removeItem('user');
        setUser(null);
      }} />
    </ThemeProvider>
  );
}

export default App;