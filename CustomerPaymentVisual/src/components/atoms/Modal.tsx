import React, { useEffect } from 'react';
import ReactDOM from 'react-dom';
import './Modal.css';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  size?: 'sm' | 'md' | 'lg';
}

export const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  children,
  size = 'md',
}) => {
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };

    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      // Prevenir scroll en el body
      document.body.style.overflow = 'hidden';
    }

    return () => {
      document.removeEventListener('keydown', handleEscape);
      document.body.style.overflow = 'unset';
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return ReactDOM.createPortal(
    <div className="atm-modal-overlay animate-fade" onClick={onClose}>
      <div 
        className={`atm-modal-card atm-modal--${size} animate-scale`} 
        onClick={(e) => e.stopPropagation()} // Prevenir cierre al hacer clic en la tarjeta
      >
        <div className="atm-modal-header">
          <h2 className="atm-modal-title">{title}</h2>
          <button className="atm-modal-close" onClick={onClose} aria-label="Cerrar modal">
            ✕
          </button>
        </div>
        <div className="atm-modal-content">
          {children}
        </div>
      </div>
    </div>,
    document.body
  );
};
