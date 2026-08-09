import React from 'react';
import './StatusBadge.css';

interface StatusBadgeProps {
  status: 'Pendiente' | 'Completado' | string;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status }) => {
  let statusClass = 'atm-status--default';
  
  const statusLower = status.toLowerCase();
  if (statusLower === 'pendiente') {
    statusClass = 'atm-status--warning';
  } else if (statusLower === 'completado') {
    statusClass = 'atm-status--success';
  }

  return (
    <span className={`atm-status-badge ${statusClass}`}>
      {status}
    </span>
  );
};
