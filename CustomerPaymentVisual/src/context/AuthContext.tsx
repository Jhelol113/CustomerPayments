import React, { createContext, useState, useEffect, useCallback, type ReactNode } from 'react';
import authService from '../services/authService';
import useInactivityTimer from '../hooks/useInactivityTimer';

interface AuthContextType {
  isAuthenticated: boolean;
  currentUser: string | null;
  currentRole: string | null;
  login: (token: string, username: string, rol: string) => void;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextType>({
  isAuthenticated: false,
  currentUser: null,
  currentRole: null,
  login: () => {},
  logout: () => {},
});

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [currentUser, setCurrentUser] = useState<string | null>(null);
  const [currentRole, setCurrentRole] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const authStatus = authService.isAuthenticated();
    setIsAuthenticated(authStatus);
    if (authStatus) {
      setCurrentUser(authService.getCurrentUser());
      setCurrentRole(authService.getCurrentRole());
    }
    setLoading(false);
  }, []);

  const login = (token: string, username: string, rol: string) => {
    authService.saveSession(token, username, rol);
    setIsAuthenticated(true);
    setCurrentUser(username);
    setCurrentRole(rol);
  };

  const logout = useCallback(() => {
    authService.logout();
    setIsAuthenticated(false);
    setCurrentUser(null);
    setCurrentRole(null);
  }, []);

  // TUTOR IA: Cierre automático de sesión por inactividad (5 minutos)
  const handleInactivityTimeout = useCallback(() => {
    if (isAuthenticated) {
      logout();
      // Redirigir al login con parámetro de sesión expirada
      window.location.href = '/login?expired=true';
    }
  }, [isAuthenticated, logout]);

  useInactivityTimer(handleInactivityTimeout, isAuthenticated);

  if (loading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', background: '#0c0e1a', color: '#f1f5f9' }}>
        <p>Cargando...</p>
      </div>
    );
  }

  return (
    <AuthContext.Provider value={{ isAuthenticated, currentUser, currentRole, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
