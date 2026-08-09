import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { HiPlus, HiSearch, HiPencil, HiTrash, HiCreditCard } from 'react-icons/hi';
import customerService from '../services/customerService';
import { Customer, CustomerRequest } from '../types';
import { Button } from '../components/atoms/Button';
import { Modal } from '../components/atoms/Modal';
import FormField from '../components/molecules/FormField';
import DataTable from '../components/molecules/DataTable';
import './CustomersPage.css';

const CustomersPage: React.FC = () => {
  const navigate = useNavigate();
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  
  // Modal de formulario
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [formData, setFormData] = useState<CustomerRequest>({
    nombre: '',
    email: '',
    telefono: '',
    direccion: ''
  });

  // Modal de eliminar
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [deletingCustomer, setDeletingCustomer] = useState<Customer | null>(null);

  // Notificaciones
  const [toast, setToast] = useState<{ message: string, type: 'success' | 'error' } | null>(null);

  const fetchCustomers = async () => {
    setLoading(true);
    try {
      const data = await customerService.getAll();
      setCustomers(data);
    } catch (error) {
      showToast('Error al cargar los clientes', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCustomers();
  }, []);

  const showToast = (message: string, type: 'success' | 'error') => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 3000);
  };

  // Filtrado local
  const filteredCustomers = customers.filter(c => 
    c.nombre.toLowerCase().includes(search.toLowerCase()) || 
    c.email.toLowerCase().includes(search.toLowerCase())
  );

  const openCreateModal = () => {
    setEditingId(null);
    setFormData({ nombre: '', email: '', telefono: '', direccion: '' });
    setIsFormModalOpen(true);
  };

  const openEditModal = (customer: Customer) => {
    setEditingId(customer.id);
    setFormData({
      nombre: customer.nombre,
      email: customer.email,
      telefono: customer.telefono || '',
      direccion: customer.direccion || ''
    });
    setIsFormModalOpen(true);
  };

  const openDeleteModal = (customer: Customer) => {
    setDeletingCustomer(customer);
    setIsDeleteModalOpen(true);
  };

  const handleFormChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      if (editingId) {
        await customerService.update(editingId, formData);
        showToast('Cliente actualizado correctamente', 'success');
      } else {
        await customerService.create(formData);
        showToast('Cliente creado correctamente', 'success');
      }
      setIsFormModalOpen(false);
      fetchCustomers();
    } catch (error) {
      showToast('Error al guardar el cliente', 'error');
    }
  };

  const handleDelete = async () => {
    if (!deletingCustomer) return;
    try {
      await customerService.delete(deletingCustomer.id);
      showToast('Cliente eliminado correctamente', 'success');
      setIsDeleteModalOpen(false);
      fetchCustomers();
    } catch (error: any) {
      if (error.response?.status === 409) {
        showToast(error.response.data?.mensaje || 'No se puede eliminar el cliente porque tiene pagos asociados', 'error');
      } else {
        showToast('Error al eliminar el cliente', 'error');
      }
    }
  };


  // Columnas para la tabla
  const columns = [
    { key: 'id', title: 'ID' },
    { key: 'nombre', title: 'Nombre' },
    { key: 'email', title: 'Email' },
    { key: 'telefono', title: 'Teléfono', render: (val: any) => val || 'N/A' },
    { key: 'fechaCreacion', title: 'Fecha', render: (val: string) => new Date(val).toLocaleDateString() },
    { 
      key: 'acciones', 
      title: 'Acciones', 
      render: (_: any, item: Customer) => (
        <div style={{ display: 'flex', gap: '8px' }}>
          <Button variant="ghost" size="sm" onClick={() => navigate(`/payments?customerId=${item.id}`)} title="Ver Pagos">
            <HiCreditCard size={18} />
          </Button>
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
          <h1 className="page-title">Gestión de Clientes</h1>
          <p className="page-subtitle">Total de clientes registrados: {customers.length}</p>
        </div>
        <div className="page-actions">
          <div className="search-bar">
            <HiSearch className="search-bar-icon" size={18} />
            <input 
              type="text" 
              className="search-input"
              placeholder="Buscar por nombre o email..." 
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <Button variant="primary" onClick={openCreateModal}>
            <HiPlus size={18} style={{ marginRight: '8px' }} />
            Nuevo Cliente
          </Button>
        </div>
      </header>

      <DataTable 
        columns={columns} 
        data={filteredCustomers} 
        loading={loading} 
        emptyMessage="No se encontraron clientes."
      />

      {/* Modal Crear/Editar */}
      <Modal 
        isOpen={isFormModalOpen} 
        onClose={() => setIsFormModalOpen(false)} 
        title={editingId ? 'Editar Cliente' : 'Nuevo Cliente'}
      >
        <form onSubmit={handleSave}>
          <div className="modal-form-grid">
            <FormField label="Nombre" name="nombre" value={formData.nombre} onChange={handleFormChange} required />
            <FormField label="Email" name="email" type="email" value={formData.email} onChange={handleFormChange} required />
            <FormField label="Teléfono" name="telefono" value={formData.telefono || ''} onChange={handleFormChange} />
            <FormField label="Dirección" name="direccion" value={formData.direccion || ''} onChange={handleFormChange} />
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
          ¿Estás seguro de eliminar a <strong>{deletingCustomer?.nombre}</strong>? Esta acción no se puede deshacer.
        </p>
        <div className="modal-footer">
          <Button variant="secondary" onClick={() => setIsDeleteModalOpen(false)}>Cancelar</Button>
          <Button variant="danger" onClick={handleDelete}>Eliminar</Button>
        </div>
      </Modal>
    </div>
  );
};

export default CustomersPage;
