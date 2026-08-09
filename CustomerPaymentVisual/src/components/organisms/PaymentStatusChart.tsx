import React from 'react';
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip, Legend } from 'recharts';

//  Gráfico de pastel (PieChart) que muestra la distribución
// de estados de pagos: Completados vs Pendientes vs Fallidos.

interface StatusData {
  name: string;
  value: number;
}

interface PaymentStatusChartProps {
  data: StatusData[];
}

const COLORS: Record<string, string> = {
  'Completado': '#10b981',
  'Pendiente': '#f59e0b',
  'Fallido': '#ef4444',
};

const PaymentStatusChart: React.FC<PaymentStatusChartProps> = ({ data }) => {
  return (
    <div style={{ width: '100%', height: 320 }}>
      <ResponsiveContainer>
        <PieChart>
          <Pie
            data={data}
            cx="50%"
            cy="50%"
            innerRadius={70}
            outerRadius={110}
            paddingAngle={4}
            dataKey="value"
            stroke="none"
          >
            {data.map((entry) => (
              <Cell key={entry.name} fill={COLORS[entry.name] || '#6366f1'} />
            ))}
          </Pie>
          <Tooltip
            contentStyle={{
              background: 'rgba(17, 20, 39, 0.95)',
              border: '1px solid rgba(255,255,255,0.1)',
              borderRadius: '10px',
              color: '#f1f5f9',
              fontSize: '0.9rem',
            }}
          />
          <Legend
            wrapperStyle={{ color: '#94a3b8', fontSize: '0.85rem' }}
          />
        </PieChart>
      </ResponsiveContainer>
    </div>
  );
};

export default PaymentStatusChart;
