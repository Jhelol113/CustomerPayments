import React, { useContext } from 'react';
import { NavLink } from 'react-router-dom';
import { HiUsers, HiCreditCard, HiLogout, HiChartPie } from 'react-icons/hi';
import { AuthContext } from '../../context/AuthContext';
import './Sidebar.css';

const Sidebar: React.FC = () => {
  const { currentUser, currentRole, logout } = useContext(AuthContext);

  // Obtener inicial del usuario
  const getInitial = () => {
    return currentUser ? currentUser.charAt(0).toUpperCase() : 'U';
  };

  return (
    <aside className="sidebar">
      <div className="sidebar-logo">
        <span>✧</span>
        <span>CustomerPayment</span>
      </div>
      
      <nav className="sidebar-nav">
        <NavLink to="/dashboard" className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}>
          <HiChartPie className="sidebar-link-icon" />
          <span>Dashboard</span>
        </NavLink>
        <NavLink to="/customers" className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}>
          <HiUsers size={20} />
          <span>Clientes</span>
        </NavLink>
        <NavLink to="/payments" className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}>
          <HiCreditCard size={20} />
          <span>Pagos</span>
        </NavLink>
      </nav>

      <div className="sidebar-footer">
        <div className="user-profile">
          <div className="user-avatar">{getInitial()}</div>
          <div className="user-info">
            <span className="user-name">{currentUser || 'Usuario'}</span>
            <span className="user-role">{currentRole || 'Rol'}</span>
          </div>
        </div>
        <button onClick={logout} className="logout-btn">
          <HiLogout size={18} />
          <span>Cerrar Sesión</span>
        </button>
      </div>
    </aside>
  );
};

export default Sidebar;
