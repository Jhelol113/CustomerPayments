import React from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

interface TopPaymentData {
  label: string;
  monto: number;
}

interface TopPaymentsChartProps {
  data: TopPaymentData[];
}

const TopPaymentsChart: React.FC<TopPaymentsChartProps> = ({ data }) => {
  return (
    <div style={{ width: '100%', height: 320 }}>
      <ResponsiveContainer>
        <BarChart data={data} margin={{ top: 5, right: 20, left: 10, bottom: 60 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
          <XAxis
            dataKey="label"
            tick={{ fill: '#94a3b8', fontSize: 11 }}
            axisLine={{ stroke: 'rgba(255,255,255,0.08)' }}
            tickLine={false}
            angle={-45}
            textAnchor="end"
          />
          <YAxis
            tick={{ fill: '#94a3b8', fontSize: 12 }}
            axisLine={{ stroke: 'rgba(255,255,255,0.08)' }}
            tickLine={false}
            tickFormatter={(v) => `$${v}`}
          />
          <Tooltip
            contentStyle={{
              background: 'rgba(17, 20, 39, 0.95)',
              border: '1px solid rgba(255,255,255,0.1)',
              borderRadius: '10px',
              color: '#f1f5f9',
              fontSize: '0.9rem',
            }}
            formatter={(value: number) => [`$${value.toFixed(2)}`, 'Monto']}
          />
          <Bar dataKey="monto" fill="#8b5cf6" radius={[6, 6, 0, 0]} maxBarSize={50} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
};

export default TopPaymentsChart;
