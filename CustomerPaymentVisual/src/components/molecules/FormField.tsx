import React from 'react';
import Select from '../atoms/Select';

interface Option {
  value: string;
  label: string;
}

interface FormFieldProps {
  label: string;
  name: string;
  value: string | number;
  onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => void;
  type?: string;
  placeholder?: string;
  required?: boolean;
  error?: string;
  options?: Option[];
}

const FormField: React.FC<FormFieldProps> = ({
  label, name, value, onChange, type = 'text', placeholder, required, error, options
}) => {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', marginBottom: '16px' }}>
      <label style={{ fontSize: '0.9rem', color: '#cbd5e1' }}>{label}</label>
      {type === 'select' && options ? (
        <Select
          name={name}
          value={value as string}
          onChange={onChange as any}
          options={options}
          required={required}
          error={!!error}
        />
      ) : (
        <input
          type={type}
          name={name}
          value={value}
          onChange={onChange}
          placeholder={placeholder}
          required={required}
          style={{ padding: '10px', borderRadius: '8px', border: '1px solid #475569', background: '#1e293b', color: 'white', width: '100%', boxSizing: 'border-box' }}
        />
      )}
      {error && <span style={{ color: '#ef4444', fontSize: '0.8rem' }}>{error}</span>}
    </div>
  );
};

export default FormField;
