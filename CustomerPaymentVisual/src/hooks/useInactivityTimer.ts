import { useEffect, useRef, useCallback } from 'react';

// Hook personalizado para detectar inactividad del usuario.
// Escucha eventos de interacción (mouse, teclado, scroll, touch).
// Si no hay actividad durante el tiempo configurado, ejecuta el callback.

const INACTIVITY_TIMEOUT_MS = 5 * 60 * 1000; // 5 minutos

const useInactivityTimer = (onTimeout: () => void, enabled: boolean = true) => {
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const onTimeoutRef = useRef(onTimeout);

  // Mantener la referencia al callback actualizada
  useEffect(() => {
    onTimeoutRef.current = onTimeout;
  }, [onTimeout]);

  const resetTimer = useCallback(() => {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
    }
    timerRef.current = setTimeout(() => {
      onTimeoutRef.current();
    }, INACTIVITY_TIMEOUT_MS);
  }, []);

  useEffect(() => {
    if (!enabled) return;

    // Eventos que indican actividad del usuario
    const events: (keyof DocumentEventMap)[] = [
      'mousemove', 'mousedown', 'keydown', 'scroll', 'touchstart', 'click'
    ];

    const handleActivity = () => resetTimer();

    // Registrar todos los listeners
    events.forEach(event => document.addEventListener(event, handleActivity, { passive: true }));

    // Iniciar el timer
    resetTimer();

    // Cleanup: remover listeners y limpiar timer
    return () => {
      events.forEach(event => document.removeEventListener(event, handleActivity));
      if (timerRef.current) {
        clearTimeout(timerRef.current);
      }
    };
  }, [enabled, resetTimer]);
};

export default useInactivityTimer;
