import { useState, useEffect } from 'react';
import { mockAuditLogs } from '../data/mockData';
import { Clock, User, FileText } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';

export default function AuditLog() {
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const timer = setTimeout(() => {
      setLoading(false);
    }, 1000);
    return () => clearTimeout(timer);
  }, []);

  return (
    <div className="p-6 max-w-5xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-800">Audit Log</h1>
        <p className="text-slate-500 mt-1">System activity and user actions.</p>
      </div>

      <Card>
        <CardContent className="p-0">
          {loading ? (
             <div className="p-6 space-y-4">
               {[1, 2, 3].map((i) => (
                 <div key={i} className="flex flex-col space-y-3">
                   <Skeleton className="h-4 w-1/3" />
                   <Skeleton className="h-4 w-1/2" />
                   <div className="flex space-x-2">
                     <Skeleton className="h-4 w-[100px]" />
                     <Skeleton className="h-4 w-[100px]" />
                   </div>
                 </div>
               ))}
             </div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Event</TableHead>
                    <TableHead>User</TableHead>
                    <TableHead>Details</TableHead>
                    <TableHead>Time</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {mockAuditLogs.map((log) => (
                    <TableRow key={log.id}>
                      <TableCell>
                        <div className="flex items-center space-x-2">
                          <Badge variant="outline" className="uppercase tracking-wider">{log.action.split(' ')[0]}</Badge>
                          <span className="font-semibold text-slate-800">{log.action}</span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <span className="flex items-center text-slate-600">
                          <User size={14} className="mr-1" />
                          {log.user}
                        </span>
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-col space-y-1">
                          <span className="text-slate-600">{log.details}</span>
                          <span className="flex items-center text-xs text-slate-400 font-mono">
                            <FileText size={12} className="mr-1" />
                            ID: {log.id}
                          </span>
                        </div>
                      </TableCell>
                      <TableCell>
                        <time className="text-xs font-medium text-slate-500 flex items-center">
                          <Clock size={12} className="mr-1" />
                          {log.timestamp}
                        </time>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
