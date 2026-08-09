import api from './api';
import type { LoginRequest, LoginResponse } from '../types';

const authService = {
  login: async (credentials: LoginRequest): Promise<LoginResponse> => {
    const response = await api.post<LoginResponse>('/auth/login', credentials);
    return response.data;
  },
  register: async (username: string, password: string, rol: string = 'User') => {
    const response = await api.post(`/auth/register?rol=${rol}`, { username, password });
    return response.data;
  },
  saveSession: (token: string, username: string, rol: string) => {
    localStorage.setItem('token', token);
    localStorage.setItem('username', username);
    localStorage.setItem('rol', rol);
  },
  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    localStorage.removeItem('rol');
  },
  isAuthenticated: (): boolean => !!localStorage.getItem('token'),
  getCurrentUser: (): string | null => localStorage.getItem('username'),
  getCurrentRole: (): string | null => localStorage.getItem('rol'),
};

export default authService;
