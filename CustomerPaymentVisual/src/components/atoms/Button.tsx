import React from 'react';
import './Button.css';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger' | 'ghost' | 'icon';
  fullWidth?: boolean;
  size?: 'sm' | 'md';
}

export const Button: React.FC<ButtonProps> = ({
  children,
  variant = 'primary',
  fullWidth = false,
  size = 'md',
  className = '',
  ...props
}) => {
  const classes = ['atm-button', `atm-button--${variant}`, `atm-button--${size}`];
  if (fullWidth) classes.push('atm-button--full-width');
  if (className) classes.push(className);

  return (
    <button className={classes.join(' ')} {...props}>
      {children}
    </button>
  );
};
