
import { mockKPIs, mockRiskData, mockAlerts } from '../data/mockData';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { AlertCircle, ShieldAlert, Activity, Ban } from 'lucide-react';

export default function Dashboard() {
  return (
    <div className="p-6 space-y-6">
      <h1 className="text-2xl font-bold text-slate-800">Dashboard</h1>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white p-4 rounded-lg shadow-sm border border-slate-200 flex items-center space-x-4">
          <div className="p-3 bg-blue-100 text-blue-600 rounded-full">
            <Activity size={24} />
          </div>
          <div>
            <p className="text-sm text-slate-500 font-medium">Total Transactions</p>
            <p className="text-2xl font-bold text-slate-800">{mockKPIs.totalTransactions.toLocaleString()}</p>
          </div>
        </div>

        <div className="bg-white p-4 rounded-lg shadow-sm border border-slate-200 flex items-center space-x-4">
          <div className="p-3 bg-red-100 text-red-600 rounded-full">
            <ShieldAlert size={24} />
          </div>
          <div>
            <p className="text-sm text-slate-500 font-medium">Fraud Alerts</p>
            <p className="text-2xl font-bold text-slate-800">{mockKPIs.fraudAlerts}</p>
          </div>
        </div>

        <div className="bg-white p-4 rounded-lg shadow-sm border border-slate-200 flex items-center space-x-4">
          <div className="p-3 bg-amber-100 text-amber-600 rounded-full">
            <AlertCircle size={24} />
          </div>
          <div>
            <p className="text-sm text-slate-500 font-medium">Avg Risk Score</p>
            <p className="text-2xl font-bold text-slate-800">{mockKPIs.riskScoreAvg}</p>
          </div>
        </div>

        <div className="bg-white p-4 rounded-lg shadow-sm border border-slate-200 flex items-center space-x-4">
          <div className="p-3 bg-emerald-100 text-emerald-600 rounded-full">
            <Ban size={24} />
          </div>
          <div>
            <p className="text-sm text-slate-500 font-medium">Blocked</p>
            <p className="text-2xl font-bold text-slate-800">{mockKPIs.blockedTransactions}</p>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Risk Score Chart */}
        <div className="bg-white p-4 rounded-lg shadow-sm border border-slate-200 lg:col-span-2">
          <h2 className="text-lg font-semibold text-slate-800 mb-4">Risk Score Over Time</h2>
          <div className="h-72">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={mockRiskData}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                <XAxis dataKey="time" axisLine={false} tickLine={false} />
                <YAxis axisLine={false} tickLine={false} />
                <Tooltip cursor={{ fill: '#f1f5f9' }} />
                <Bar dataKey="score" fill="#10b981" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Alerts Table */}
        <div className="bg-white p-4 rounded-lg shadow-sm border border-slate-200 lg:col-span-1">
          <h2 className="text-lg font-semibold text-slate-800 mb-4">Recent Alerts</h2>
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead className="text-xs text-slate-500 uppercase bg-slate-50">
                <tr>
                  <th className="px-4 py-3">ID</th>
                  <th className="px-4 py-3">Reason</th>
                  <th className="px-4 py-3">Status</th>
                </tr>
              </thead>
              <tbody>
                {mockAlerts.map((alert) => (
                  <tr key={alert.id} className="border-b last:border-0 hover:bg-slate-50">
                    <td className="px-4 py-3 font-medium text-slate-900">{alert.id}</td>
                    <td className="px-4 py-3 text-slate-600">{alert.reason}</td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                        alert.status === 'Open' ? 'bg-red-100 text-red-700' :
                        alert.status === 'Investigating' ? 'bg-amber-100 text-amber-700' :
                        'bg-emerald-100 text-emerald-700'
                      }`}>
                        {alert.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}
