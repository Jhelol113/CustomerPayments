import React, { useState, useEffect } from 'react';
import { HiUsers, HiCreditCard, HiCash, HiExclamation } from 'react-icons/hi';
import customerService from '../services/customerService';
import paymentService from '../services/paymentService';
import type { Customer, Payment } from '../types';
import KPICard from '../components/organisms/KPICard';
import PaymentStatusChart from '../components/organisms/PaymentStatusChart';
import PaymentTrendChart from '../components/organisms/PaymentTrendChart';
import CustomerTotalPaymentsChart from '../components/organisms/CustomerTotalPaymentsChart';
import './KPIsPage.css';

// Página de Dashboard con KPIs .
// Carga datos reales de la API y calcula métricas en el frontend.

const KPIsPage: React.FC = () => {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [payments, setPayments] = useState<Payment[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedChartCustomerId, setSelectedChartCustomerId] = useState<string>('');

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [customersData, paymentsData] = await Promise.all([
          customerService.getAll(),
          paymentService.getAll(),
        ]);
        setCustomers(customersData);
        setPayments(paymentsData);
      } catch (err) {
        console.error('Error al cargar datos del dashboard:', err);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  // Calcular KPIs
  const totalClientes = customers.length;
  const totalPagos = payments.length;
  const montoTotal = payments.reduce((sum, p) => sum + p.monto, 0);
  const pagosPendientes = payments.filter(p => p.estado === 'Pendiente').length;

  const statusData = [
    { name: 'Completado', value: payments.filter(p => p.estado === 'Completado').length },
    { name: 'Pendiente', value: payments.filter(p => p.estado === 'Pendiente').length },
    { name: 'Fallido', value: payments.filter(p => p.estado === 'Fallido').length },
  ].filter(d => d.value > 0);

  // Datos para gráfico de tendencia mensual
  const monthlyDataMap = new Map<string, { mesStr: string, total: number, dateObj: Date }>();
  payments.forEach(p => {
    if (p.fechaCreacion) {
      const date = new Date(p.fechaCreacion);
      const key = `${date.getFullYear()}-${date.getMonth()}`;
      if (!monthlyDataMap.has(key)) {
        const mesStr = date.toLocaleString('es-ES', { month: 'short', year: 'numeric' });
        monthlyDataMap.set(key, { 
          mesStr: mesStr.charAt(0).toUpperCase() + mesStr.slice(1), 
          total: 0, 
          dateObj: new Date(date.getFullYear(), date.getMonth(), 1) 
        });
      }
      monthlyDataMap.get(key)!.total += p.monto;
    }
  });
  
  const trendChartData = Array.from(monthlyDataMap.values())
    .sort((a, b) => a.dateObj.getTime() - b.dateObj.getTime())
    .map(d => ({ mes: d.mesStr, total: d.total }));

  // Datos para pagos totales por cliente
  const activeCustomers = customers.filter(c => c.activo);
  const paymentsForCustomerChart = selectedChartCustomerId 
    ? payments.filter(p => p.customerId.toString() === selectedChartCustomerId)
    : payments;

  const customerTotalsMap = new Map<string, number>();
  paymentsForCustomerChart.forEach(p => {
    const name = p.customerNombre || `Cliente #${p.customerId}`;
    customerTotalsMap.set(name, (customerTotalsMap.get(name) || 0) + p.monto);
  });
  
  const customerTotalData = Array.from(customerTotalsMap.entries())
    .map(([nombre, total]) => {
      const nombreCorto = nombre.length > 15 ? nombre.substring(0, 15) + '...' : nombre;
      return { nombre: nombreCorto, total };
    })
    .sort((a, b) => b.total - a.total);

  if (loading) {
    return (
      <div className="kpi-loading">
        <p>Cargando dashboard...</p>
      </div>
    );
  }

  return (
    <div className="page-kpis animate-fade">
      <div className="kpis-header">
        <h2>Dashboard</h2>
        <p>Resumen general del sistema</p>
      </div>

      {/* Tarjetas de KPI */}
      <div className="kpis-grid">
        <KPICard icon={<HiUsers />} value={totalClientes} label="Clientes Activos" color="accent" />
        <KPICard icon={<HiCreditCard />} value={totalPagos} label="Pagos Registrados" color="success" />
        <KPICard icon={<HiCash />} value={`$${montoTotal.toFixed(2)}`} label="Monto Total" color="warning" />
        <KPICard icon={<HiExclamation />} value={pagosPendientes} label="Pagos Pendientes" color="danger" />
      </div>

      {/* Gráficos */}
      <div className="kpis-charts">
        <div className="chart-card">
          <h3>Estado de Pagos</h3>
          {statusData.length > 0 ? (
            <PaymentStatusChart data={statusData} />
          ) : (
            <p className="chart-empty">No hay pagos registrados</p>
          )}
        </div>
        <div className="chart-card">
          <h3>Tendencia de Recaudación Mensual</h3>
          {trendChartData.length > 0 ? (
            <PaymentTrendChart data={trendChartData} />
          ) : (
            <p className="chart-empty">No hay datos disponibles</p>
          )}
        </div>
        <div className="chart-card" style={{ gridColumn: '1 / -1' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px', flexWrap: 'wrap', gap: '10px' }}>
            <h3 style={{ margin: 0 }}>Pagos Totales por Cliente</h3>
            <select 
              className="filter-select" 
              style={{ minWidth: '250px', margin: 0 }}
              value={selectedChartCustomerId}
              onChange={(e) => setSelectedChartCustomerId(e.target.value)}
            >
              <option value="">Todos los clientes activos</option>
              {activeCustomers.map(c => (
                <option key={c.id} value={c.id.toString()}>{c.nombre}</option>
              ))}
            </select>
          </div>
          {customerTotalData.length > 0 ? (
            <CustomerTotalPaymentsChart data={customerTotalData} />
          ) : (
            <p className="chart-empty">No hay pagos para este cliente</p>
          )}
        </div>
      </div>
    </div>
  );
};

export default KPIsPage;
