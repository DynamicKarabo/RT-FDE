import { useState } from 'react';
import { mockTransactions } from '../data/mockData';
import { Filter, ArrowUpDown } from 'lucide-react';

export default function Transactions() {
  const [filterStatus, setFilterStatus] = useState<string>('All');
  const [sortField, setSortField] = useState<'date' | 'amount' | 'riskScore'>('date');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');

  const handleSort = (field: 'date' | 'amount' | 'riskScore') => {
    if (sortField === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortOrder('desc');
    }
  };

  const filteredTransactions = mockTransactions.filter(t =>
    filterStatus === 'All' ? true : t.status === filterStatus
  ).sort((a, b) => {
    let comparison = 0;
    if (sortField === 'date') {
      comparison = new Date(a.date).getTime() - new Date(b.date).getTime();
    } else {
      comparison = a[sortField] - b[sortField];
    }
    return sortOrder === 'asc' ? comparison : -comparison;
  });

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-slate-800">Transactions</h1>

        <div className="flex items-center space-x-2 bg-white px-3 py-2 rounded-md border border-slate-200 shadow-sm">
          <Filter size={18} className="text-slate-400" />
          <select
            className="bg-transparent border-none text-sm focus:ring-0 text-slate-600 cursor-pointer outline-none"
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
          >
            <option value="All">All Statuses</option>
            <option value="Approved">Approved</option>
            <option value="Declined">Declined</option>
            <option value="Pending">Pending</option>
          </select>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow-sm border border-slate-200 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left">
            <thead className="text-xs text-slate-500 uppercase bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="px-6 py-4">ID</th>
                <th className="px-6 py-4 cursor-pointer hover:bg-slate-100" onClick={() => handleSort('date')}>
                  <div className="flex items-center space-x-1">
                    <span>Date</span>
                    <ArrowUpDown size={14} />
                  </div>
                </th>
                <th className="px-6 py-4">Merchant</th>
                <th className="px-6 py-4 cursor-pointer hover:bg-slate-100" onClick={() => handleSort('amount')}>
                  <div className="flex items-center space-x-1">
                    <span>Amount</span>
                    <ArrowUpDown size={14} />
                  </div>
                </th>
                <th className="px-6 py-4 cursor-pointer hover:bg-slate-100" onClick={() => handleSort('riskScore')}>
                  <div className="flex items-center space-x-1">
                    <span>Risk Score</span>
                    <ArrowUpDown size={14} />
                  </div>
                </th>
                <th className="px-6 py-4">Status</th>
              </tr>
            </thead>
            <tbody>
              {filteredTransactions.map((tx) => (
                <tr key={tx.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50 transition-colors">
                  <td className="px-6 py-4 font-medium text-slate-900">{tx.id}</td>
                  <td className="px-6 py-4 text-slate-600">{tx.date}</td>
                  <td className="px-6 py-4 text-slate-600">{tx.merchant}</td>
                  <td className="px-6 py-4 text-slate-900 font-medium">${tx.amount.toFixed(2)}</td>
                  <td className="px-6 py-4">
                    <span className={`font-semibold ${
                      tx.riskScore > 75 ? 'text-red-600' :
                      tx.riskScore > 40 ? 'text-amber-600' :
                      'text-emerald-600'
                    }`}>
                      {tx.riskScore}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <span className={`px-2.5 py-1 rounded-full text-xs font-medium ${
                      tx.status === 'Approved' ? 'bg-emerald-100 text-emerald-700' :
                      tx.status === 'Declined' ? 'bg-red-100 text-red-700' :
                      'bg-amber-100 text-amber-700'
                    }`}>
                      {tx.status}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
