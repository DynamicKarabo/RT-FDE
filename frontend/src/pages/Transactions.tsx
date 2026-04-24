import { useState } from 'react';
import { mockTransactions } from '../data/mockData';
import { Filter, ArrowUpDown } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';

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

      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="px-6 py-4">ID</TableHead>
                  <TableHead className="px-6 py-4 cursor-pointer hover:bg-slate-50 transition-colors" onClick={() => handleSort('date')}>
                    <div className="flex items-center space-x-1">
                      <span>Date</span>
                      <ArrowUpDown size={14} />
                    </div>
                  </TableHead>
                  <TableHead className="px-6 py-4">Merchant</TableHead>
                  <TableHead className="px-6 py-4 cursor-pointer hover:bg-slate-50 transition-colors" onClick={() => handleSort('amount')}>
                    <div className="flex items-center space-x-1">
                      <span>Amount</span>
                      <ArrowUpDown size={14} />
                    </div>
                  </TableHead>
                  <TableHead className="px-6 py-4 cursor-pointer hover:bg-slate-50 transition-colors" onClick={() => handleSort('riskScore')}>
                    <div className="flex items-center space-x-1">
                      <span>Risk Score</span>
                      <ArrowUpDown size={14} />
                    </div>
                  </TableHead>
                  <TableHead className="px-6 py-4">Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredTransactions.map((tx) => (
                  <TableRow key={tx.id}>
                    <TableCell className="px-6 py-4 font-medium">{tx.id}</TableCell>
                    <TableCell className="px-6 py-4">{tx.date}</TableCell>
                    <TableCell className="px-6 py-4">{tx.merchant}</TableCell>
                    <TableCell className="px-6 py-4 font-medium">${tx.amount.toFixed(2)}</TableCell>
                    <TableCell className="px-6 py-4">
                      <span className={`font-semibold ${
                        tx.riskScore > 75 ? 'text-red-600' :
                        tx.riskScore > 40 ? 'text-amber-600' :
                        'text-emerald-600'
                      }`}>
                        {tx.riskScore}
                      </span>
                    </TableCell>
                    <TableCell className="px-6 py-4">
                      <Badge variant={
                        tx.status === 'Approved' ? 'secondary' :
                        tx.status === 'Declined' ? 'destructive' :
                        'default'
                      } className={
                        tx.status === 'Approved' ? 'bg-emerald-500 text-white hover:bg-emerald-600' :
                        tx.status === 'Pending' ? 'bg-amber-500 hover:bg-amber-600' : ''
                      }>
                        {tx.status}
                      </Badge>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
