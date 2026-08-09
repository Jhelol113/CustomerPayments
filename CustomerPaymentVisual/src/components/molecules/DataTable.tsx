import React from 'react';

interface Column {
  key: string;
  title: string;
  render?: (value: any, item: any) => React.ReactNode;
}

interface DataTableProps {
  columns: Column[];
  data: any[];
  loading?: boolean;
  emptyMessage?: string;
}

const DataTable: React.FC<DataTableProps> = ({ columns, data, loading, emptyMessage = 'No hay datos' }) => {
  if (loading) {
    return <div style={{ padding: '20px', textAlign: 'center', color: '#94a3b8' }}>Cargando...</div>;
  }

  if (!data.length) {
    return <div style={{ padding: '20px', textAlign: 'center', color: '#94a3b8' }}>{emptyMessage}</div>;
  }

  return (
    <div style={{ overflowX: 'auto', background: 'rgba(30, 41, 59, 0.5)', borderRadius: '12px', border: '1px solid rgba(255,255,255,0.1)' }}>
      <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
        <thead>
          <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
            {columns.map(col => (
              <th key={col.key} style={{ padding: '16px', color: '#94a3b8', fontWeight: 'normal' }}>{col.title}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((row, i) => (
            <tr key={i} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
              {columns.map(col => (
                <td key={col.key} style={{ padding: '16px' }}>
                  {col.render ? col.render(row[col.key], row) : row[col.key]}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};

export default DataTable;
