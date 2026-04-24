
import { mockAuditLogs } from '../data/mockData';
import { Clock, User, FileText } from 'lucide-react';

export default function AuditLog() {
  return (
    <div className="p-6 max-w-5xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-800">Audit Log</h1>
        <p className="text-slate-500 mt-1">System activity and user actions.</p>
      </div>

      <div className="bg-white rounded-lg shadow-sm border border-slate-200 p-6">
        <div className="relative border-l border-slate-200 ml-3 space-y-8">
          {mockAuditLogs.map((log) => (
            <div key={log.id} className="relative pl-6">
              <span className="absolute -left-1.5 top-1.5 w-3 h-3 rounded-full bg-emerald-500 ring-4 ring-white"></span>
              
              <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start mb-1">
                <h3 className="font-semibold text-slate-800">{log.action}</h3>
                <time className="text-xs font-medium text-slate-400 flex items-center mt-1 sm:mt-0">
                  <Clock size={12} className="mr-1" />
                  {log.timestamp}
                </time>
              </div>
              
              <p className="text-sm text-slate-600 mb-2">{log.details}</p>
              
              <div className="flex items-center text-xs text-slate-500 space-x-4">
                <span className="flex items-center">
                  <User size={14} className="mr-1" />
                  {log.user}
                </span>
                <span className="flex items-center">
                  <FileText size={14} className="mr-1" />
                  ID: {log.id}
                </span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
