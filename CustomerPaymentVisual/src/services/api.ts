import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5263/api',
  headers: { 'Content-Type': 'application/json' },
});

// Interceptor: inyecta JWT automáticamente
api.interceptors.request.use(
  (config) => {
    // Seguridad: Prevenir fuga de tokens a dominios externos
    const isApiUrl = config.url?.startsWith('/') || config.url?.startsWith(config.baseURL || '');
    
    if (isApiUrl) {
      const token = localStorage.getItem('token');
      if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Interceptor de respuesta: detecta 401
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('username');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
