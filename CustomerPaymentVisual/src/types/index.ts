// Interfaces TypeScript para todo el sistema

export interface Customer {
  id: number;
  nombre: string;
  email: string;
  telefono?: string;
  direccion?: string;
  fechaCreacion: string;
  activo: boolean;
}

export interface CustomerRequest {
  nombre: string;
  email: string;
  telefono?: string;
  direccion?: string;
}

export interface Payment {
  id: number;
  customerId: number;
  customerNombre?: string;
  monto: number;
  metodoPago: string;
  fechaPago: string;
  estado: string;
  fechaCreacion: string;
}

export interface PaymentRequest {
  customerId: number;
  monto: number;
  metodoPago: string;
  estado?: string;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  username: string;
  rol: string;
  expiracion: string;
}
