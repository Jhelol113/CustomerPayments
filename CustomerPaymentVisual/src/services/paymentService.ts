import api from './api';
import type { Payment, PaymentRequest } from '../types';

export interface UpdateStatusPayload {
  estado: string;
}

const paymentService = {
  getAll: async (customerId?: number): Promise<Payment[]> => {
    const url = customerId ? `/payments?customerId=${customerId}` : '/payments';
    const response = await api.get<Payment[]>(url);
    return response.data;
  },
  getById: async (id: number): Promise<Payment> => {
    const response = await api.get<Payment>(`/payments/${id}`);
    return response.data;
  },
  create: async (data: PaymentRequest): Promise<Payment> => {
    const response = await api.post<Payment>('/payments', data);
    return response.data;
  },
  update: async (id: number, data: PaymentRequest): Promise<void> => {
    await api.put(`/payments/${id}`, data);
  },
  delete: async (id: number): Promise<void> => {
    await api.delete(`/payments/${id}`);
  },
  updateStatus: async (id: number, estado: string): Promise<void> => {
    const payload: UpdateStatusPayload = { estado };
    await api.patch(`/payments/${id}/status`, payload);
  },
};

export default paymentService;
