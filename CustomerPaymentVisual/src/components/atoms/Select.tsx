import React from 'react';
import { HiChevronDown } from 'react-icons/hi';
import './Select.css';

interface SelectOption {
  value: string;
  label: string;
}

interface SelectProps {
  options: SelectOption[];
  value: string;
  onChange: (e: React.ChangeEvent<HTMLSelectElement>) => void;
  name?: string;
  placeholder?: string;
  disabled?: boolean;
  error?: boolean;
  className?: string;
}

const Select: React.FC<SelectProps> = ({
  options, value, onChange, name, placeholder = 'Seleccione...', disabled = false, error = false, className = ''
}) => {
  return (
    <div className={`atm-select-wrapper ${error ? 'atm-select-wrapper--error' : ''} ${className}`}>
      <select
        name={name}
        value={value}
        onChange={onChange}
        disabled={disabled}
        className="atm-select"
      >
        <option value="" disabled>{placeholder}</option>
        {options.map(opt => (
          <option key={opt.value} value={opt.value}>{opt.label}</option>
        ))}
      </select>
      <HiChevronDown className="atm-select-icon" />
    </div>
  );
};

export default Select;
