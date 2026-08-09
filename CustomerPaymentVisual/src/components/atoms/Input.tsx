import React from 'react';
import './Input.css';

interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
}

export const Input: React.FC<InputProps> = ({ label, error, className = '', ...props }) => {
  return (
    <div className={`atm-input-wrapper ${className}`}>
      {label && <label className="atm-input-label">{label}</label>}
      <input 
        className={`atm-input ${error ? 'atm-input--error' : ''}`} 
        {...props} 
      />
      {error && <span className="atm-input-error">{error}</span>}
    </div>
  );
};
