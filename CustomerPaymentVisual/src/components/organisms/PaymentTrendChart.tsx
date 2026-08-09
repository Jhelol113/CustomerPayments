import React from 'react';
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';

// TUTOR IA: Gráfico de área para visualizar la tendencia de pagos (monto total)
// a lo largo del tiempo, agrupados por mes.

interface MonthlyTrendData {
  mes: string;
  total: number;
}

interface PaymentTrendChartProps {
  data: MonthlyTrendData[];
}

const PaymentTrendChart: React.FC<PaymentTrendChartProps> = ({ data }) => {
  return (
    <div style={{ width: '100%', height: 320 }}>
      <ResponsiveContainer>
        <AreaChart data={data} margin={{ top: 10, right: 30, left: 0, bottom: 0 }}>
          <defs>
            <linearGradient id="colorTotal" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="#10b981" stopOpacity={0.8}/>
              <stop offset="95%" stopColor="#10b981" stopOpacity={0}/>
            </linearGradient>
          </defs>
          <XAxis 
            dataKey="mes" 
            tick={{ fill: '#94a3b8', fontSize: 12 }}
            axisLine={{ stroke: 'rgba(255,255,255,0.08)' }}
            tickLine={false}
          />
          <YAxis 
            tick={{ fill: '#94a3b8', fontSize: 12 }}
            axisLine={{ stroke: 'rgba(255,255,255,0.08)' }}
            tickLine={false}
            tickFormatter={(v) => `$${v}`}
          />
          <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" vertical={false} />
          <Tooltip 
            contentStyle={{
              background: 'rgba(17, 20, 39, 0.95)',
              border: '1px solid rgba(255,255,255,0.1)',
              borderRadius: '10px',
              color: '#f1f5f9',
              fontSize: '0.9rem',
            }}
            formatter={(value: number) => [`$${value.toFixed(2)}`, 'Recaudación']}
          />
          <Area type="monotone" dataKey="total" stroke="#10b981" fillOpacity={1} fill="url(#colorTotal)" />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
};

export default PaymentTrendChart;
