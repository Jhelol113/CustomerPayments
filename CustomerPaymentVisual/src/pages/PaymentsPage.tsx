import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { HiPlus, HiPencil, HiTrash, HiCheckCircle } from 'react-icons/hi';
import paymentService from '../services/paymentService';
import customerService from '../services/customerService';
import { Payment, PaymentRequest, Customer } from '../types';
import { Button } from '../components/atoms/Button';
import { Modal } from '../components/atoms/Modal';
import FormField from '../components/molecules/FormField';
import DataTable from '../components/molecules/DataTable';
import { StatusBadge } from '../components/atoms/StatusBadge';
import './PaymentsPage.css';

const PaymentsPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const urlCustomerId = searchParams.get('customerId');
  
  const [payments, setPayments] = useState<Payment[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  
  // Filtro
  const [selectedCustomerId, setSelectedCustomerId] = useState<string>(urlCustomerId || '');
  
  // Modal de formulario
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formData, setFormData] = useState<PaymentRequest>({
    customerId: 0,
    monto: 0,
    metodoPago: 'Transferencia',
    estado: 'Completado'
  });

  // Modal de eliminar
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [deletingPayment, setDeletingPayment] = useState<Payment | null>(null);
  const [toast, setToast] = useState<{ message: string, type: 'success' | 'error' } | null>(null);

  const fetchInitialData = async () => {
    setLoading(true);
    try {
      const [paymentsData, customersData] = await Promise.all([
        paymentService.getAll(),
        customerService.getAll()
      ]);
      setPayments(paymentsData);
      setCustomers(customersData);
    } catch (error) {
      showToast('Error al cargar datos', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchInitialData();
  }, []);

  const showToast = (message: string, type: 'success' | 'error') => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 3000);
  };

  // Filtrado de pagos según el cliente seleccionado
  const filteredPayments = selectedCustomerId 
    ? payments.filter(p => p.customerId.toString() === selectedCustomerId)
    : payments;

  const handleFilterChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const val = e.target.value;
    setSelectedCustomerId(val);
    if (val) {
      setSearchParams({ customerId: val });
    } else {
      setSearchParams({});
    }
  };

  const openCreateModal = () => {
    setEditingId(null);
    setFormData({
      customerId: selectedCustomerId ? parseInt(selectedCustomerId) : (customers[0]?.id || 0),
      monto: 0,
      metodoPago: 'Transferencia',
      estado: 'Pendiente'
    });
    setIsFormModalOpen(true);
  };

  const openEditModal = (payment: Payment) => {
    setEditingId(payment.id);
    setFormData({
      customerId: payment.customerId,
      monto: payment.monto,
      metodoPago: payment.metodoPago,
      estado: payment.estado
    });
    setIsFormModalOpen(true);
  };

  const openDeleteModal = (payment: Payment) => {
    setDeletingPayment(payment);
    setIsDeleteModalOpen(true);
  };

  const handleFormChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ 
      ...prev, 
      [name]: name === 'customerId' || name === 'monto' ? Number(value) : value 
    }));
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingId) {
        await paymentService.update(editingId, formData);
        showToast('Pago actualizado correctamente', 'success');
      } else {
        await paymentService.create(formData);
        showToast('Pago registrado correctamente', 'success');
      }
      setIsFormModalOpen(false);
      // Recargar pagos
      const data = await paymentService.getAll();
      setPayments(data);
    } catch (error) {
      showToast('Error al guardar el pago', 'error');
    }
  };

  const handleDelete = async () => {
    if (!deletingPayment) return;
    try {
      await paymentService.delete(deletingPayment.id);
      showToast('Pago eliminado correctamente', 'success');
      setIsDeleteModalOpen(false);
      const data = await paymentService.getAll();
      setPayments(data);
    } catch (error) {
      showToast('Error al eliminar el pago', 'error');
    }
  };

  const handleCompleteStatus = async (id: number) => {
    showToast('Actualizando estado...', 'success');
    try {
      await paymentService.updateStatus(id, 'Completado');
      showToast('Pago completado correctamente', 'success');
      const data = await paymentService.getAll();
      setPayments(data);
    } catch (error) {
      showToast('Error al completar el pago', 'error');
    }
  };

  const customerOptions = customers.map(c => ({ value: c.id.toString(), label: c.nombre }));

  const columns = [
    { key: 'id', title: 'ID' },
    { key: 'customerNombre', title: 'Cliente' },
    { key: 'monto', title: 'Monto', render: (val: number) => `$${val.toFixed(2)}` },
    { key: 'metodoPago', title: 'Método' },
    { key: 'fechaPago', title: 'Fecha', render: (val: string) => new Date(val).toLocaleDateString() },
    { key: 'estado', title: 'Estado', render: (val: string) => <StatusBadge status={val} /> },
    { 
      key: 'acciones', 
      title: 'Acciones', 
      render: (_: any, item: Payment) => (
        <div style={{ display: 'flex', gap: '8px' }}>
          <div style={{ visibility: item.estado === 'Pendiente' ? 'visible' : 'hidden' }}>
            <Button variant="icon" size="sm" onClick={() => handleCompleteStatus(item.id)} title="Completar">
              <HiCheckCircle size={18} />
            </Button>
          </div>
          <Button variant="icon" size="sm" onClick={() => openEditModal(item)} title="Editar">
            <HiPencil size={18} />
          </Button>
          <Button variant="danger" size="sm" onClick={() => openDeleteModal(item)} title="Eliminar">
            <HiTrash size={18} />
          </Button>
        </div>
      ) 
    }
  ];

  return (
    <div className="page-container">
      {toast && (
        <div className={`toast-notification toast-${toast.type}`}>
          {toast.message}
        </div>
      )}

      <header className="page-header">
        <div>
          <h1 className="page-title">Gestión de Pagos</h1>
          <p className="page-subtitle">Total de pagos: {filteredPayments.length}</p>
        </div>
        <div className="page-actions">
          <div className="filters-container">
            <select 
              className="filter-select"
              value={selectedCustomerId}
              onChange={handleFilterChange}
            >
              <option value="">Todos los clientes</option>
              {customers.map(c => (
                <option key={c.id} value={c.id}>{c.nombre}</option>
              ))}
            </select>
          </div>
          <Button variant="primary" onClick={openCreateModal}>
            <HiPlus size={18} style={{ marginRight: '8px' }} />
            Registrar Pago
          </Button>
        </div>
      </header>

      <DataTable 
        columns={columns} 
        data={filteredPayments} 
        loading={loading} 
        emptyMessage="No se encontraron pagos."
      />

      {/* Modal Crear/Editar */}
      <Modal 
        isOpen={isFormModalOpen} 
        onClose={() => setIsFormModalOpen(false)} 
        title={editingId ? 'Editar Pago' : 'Registrar Pago'}
      >
        <form onSubmit={handleSave}>
          <div className="modal-form-grid">
            <FormField 
              label="Cliente" 
              name="customerId" 
              type="select" 
              value={formData.customerId.toString()} 
              onChange={handleFormChange}
              options={customerOptions}
              required 
            />
            <FormField 
              label="Monto" 
              name="monto" 
              type="number" 
              value={formData.monto.toString()} 
              onChange={handleFormChange} 
              required 
            />
            <FormField 
              label="Método de Pago" 
              name="metodoPago" 
              type="select" 
              value={formData.metodoPago} 
              onChange={handleFormChange}
              options={[
                { value: 'Transferencia', label: 'Transferencia' },
                { value: 'Efectivo', label: 'Efectivo' },
                { value: 'Tarjeta', label: 'Tarjeta' }
              ]}
              required 
            />
            {editingId && (
              <FormField 
                label="Estado" 
                name="estado" 
                type="select" 
                value={formData.estado || 'Completado'} 
                onChange={handleFormChange}
                options={[
                  { value: 'Pendiente', label: 'Pendiente' },
                  { value: 'Completado', label: 'Completado' },
                  { value: 'Fallido', label: 'Fallido' }
                ]}
              />
            )}
          </div>
          <div className="modal-footer">
            <Button type="button" variant="secondary" onClick={() => setIsFormModalOpen(false)}>Cancelar</Button>
            <Button type="submit" variant="primary">Guardar</Button>
          </div>
        </form>
      </Modal>

      {/* Modal Eliminar */}
      <Modal 
        isOpen={isDeleteModalOpen} 
        onClose={() => setIsDeleteModalOpen(false)} 
        title="Confirmar Eliminación"
      >
        <p style={{ marginTop: '16px', color: '#cbd5e1' }}>
          ¿Estás seguro de eliminar este pago por <strong>${deletingPayment?.monto.toFixed(2)}</strong>? Esta acción no se puede deshacer.
        </p>
        <div className="modal-footer">
          <Button variant="secondary" onClick={() => setIsDeleteModalOpen(false)}>Cancelar</Button>
          <Button variant="danger" onClick={handleDelete}>Eliminar</Button>
        </div>
      </Modal>
    </div>
  );
};

export default PaymentsPage;
