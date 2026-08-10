import React, { useState, useContext } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { AuthContext } from '../context/AuthContext';
import authService from '../services/authService';
import FormField from '../components/molecules/FormField';
import { Button } from '../components/atoms/Button';
import './LoginPage.css';

const LoginPage: React.FC = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  
  const { login } = useContext(AuthContext);
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const sessionExpired = searchParams.get('expired') === 'true';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      // Llamar al servicio de autenticación
      const response = await authService.login({ username, password });
      
      // Actualizar el contexto con la respuesta
      login(response.token, response.username, response.rol);
      
      // Navegar a la página principal
      navigate('/customers');
    } catch (err: any) {
      // Mostrar el error provisto por el backend o uno genérico
      const msg = err.response?.data?.mensaje || 'Error de conexión. Verifica tus credenciales.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-header">
          <div className="login-logo">✧</div>
          <h1 className="login-title">CustomerPayment</h1>
          <p className="login-subtitle">Inicia sesión para continuar</p>
        </div>

        <form onSubmit={handleSubmit} className="login-form">
          {sessionExpired && (
            <div style={{ background: 'rgba(245, 158, 11, 0.15)', color: '#f59e0b', padding: '0.75rem 1rem', borderRadius: '10px', marginBottom: '1.5rem', textAlign: 'center', border: '1px solid rgba(245, 158, 11, 0.3)', fontSize: '0.9rem' }}>
              Tu sesión expiró por inactividad. Por favor, inicia sesión nuevamente.
            </div>
          )}
          {error && <div className="error-message">{error}</div>}
          
          <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', marginTop: '16px', marginBottom: '24px' }}>
            <FormField
              label="Nombre de usuario"
              name="username"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Ej. admin"
              required
            />
            
            <FormField
              label="Contraseña"
              name="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
            />
          </div>

          <Button type="submit" variant="primary" fullWidth disabled={loading}>
            {loading ? 'Iniciando...' : 'Iniciar Sesión'}
          </Button>
        </form>
      </div>
    </div>
  );
};

export default LoginPage;
