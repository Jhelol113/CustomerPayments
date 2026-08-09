import React from 'react';
import type { ReactNode } from 'react';
import './KPICard.css';

// TUTOR IA: Tarjeta individual de KPI para el Dashboard.
// Muestra un icono, un valor numérico destacado y una etiqueta descriptiva.

interface KPICardProps {
  icon: ReactNode;
  value: string | number;
  label: string;
  color: 'accent' | 'success' | 'warning' | 'danger';
}

const KPICard: React.FC<KPICardProps> = ({ icon, value, label, color }) => {
  return (
    <div className={`kpi-card kpi-card--${color}`}>
      <div className="kpi-card__icon">{icon}</div>
      <div className="kpi-card__content">
        <span className="kpi-card__value">{value}</span>
        <span className="kpi-card__label">{label}</span>
      </div>
    </div>
  );
};

export default KPICard;
