import api from './api';
import type { Customer, CustomerRequest } from '../types';

const customerService = {
  getAll: async (): Promise<Customer[]> => {
    const response = await api.get<Customer[]>('/customers');
    return response.data;
  },
  getById: async (id: number): Promise<Customer> => {
    const response = await api.get<Customer>(`/customers/${id}`);
    return response.data;
  },
  create: async (data: CustomerRequest): Promise<Customer> => {
    const response = await api.post<Customer>('/customers', data);
    return response.data;
  },
  update: async (id: number, data: CustomerRequest): Promise<void> => {
    await api.put(`/customers/${id}`, data);
  },
  delete: async (id: number): Promise<void> => {
    await api.delete(`/customers/${id}`);
  },
};

export default customerService;
