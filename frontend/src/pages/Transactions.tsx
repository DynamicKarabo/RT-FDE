import { useState } from 'react';
import { mockTransactions } from '../data/mockData';
import { Filter, ArrowUpDown } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { Button } from '@/components/ui/button';

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
        
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm">
              <Filter className="mr-2 h-4 w-4" />
              {filterStatus === 'All' ? 'All Statuses' : filterStatus}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => setFilterStatus('All')}>All Statuses</DropdownMenuItem>
            <DropdownMenuItem onClick={() => setFilterStatus('Approved')}>Approved</DropdownMenuItem>
            <DropdownMenuItem onClick={() => setFilterStatus('Declined')}>Declined</DropdownMenuItem>
            <DropdownMenuItem onClick={() => setFilterStatus('Pending')}>Pending</DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>ID</TableHead>
                <TableHead 
                  className="cursor-pointer hover:bg-slate-50"
                  onClick={() => handleSort('date')}
                >
                  <div className="flex items-center space-x-1">
                    <span>Date</span>
                    <ArrowUpDown size={14} />
                  </div>
                </TableHead>
                <TableHead>Merchant</TableHead>
                <TableHead 
                  className="cursor-pointer hover:bg-slate-50"
                  onClick={() => handleSort('amount')}
                >
                  <div className="flex items-center space-x-1">
                    <span>Amount</span>
                    <ArrowUpDown size={14} />
                  </div>
                </TableHead>
                <TableHead 
                  className="cursor-pointer hover:bg-slate-50"
                  onClick={() => handleSort('riskScore')}
                >
                  <div className="flex items-center space-x-1">
                    <span>Risk Score</span>
                    <ArrowUpDown size={14} />
                  </div>
                </TableHead>
                <TableHead>Status</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredTransactions.map((tx) => (
                <TableRow key={tx.id}>
                  <TableCell className="font-medium">{tx.id}</TableCell>
                  <TableCell>{tx.date}</TableCell>
                  <TableCell>{tx.merchant}</TableCell>
                  <TableCell className="font-medium">${tx.amount.toFixed(2)}</TableCell>
                  <TableCell>
                    <span className={`font-semibold ${
                      tx.riskScore > 75 ? 'text-red-600' : 
                      tx.riskScore > 40 ? 'text-amber-600' : 
                      'text-emerald-600'
                    }`}>
                      {tx.riskScore}
                    </span>
                  </TableCell>
                  <TableCell>
                    <Badge variant={
                      tx.status === 'Approved' ? 'default' :
                      tx.status === 'Declined' ? 'destructive' :
                      'secondary'
                    }>
                      {tx.status}
                    </Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
