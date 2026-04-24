import { useState } from 'react';
import { mockRules } from '../data/mockData';
import { Shield, ShieldOff } from 'lucide-react';

export default function Rules() {
  const [rules, setRules] = useState(mockRules);

  const toggleRule = (id: string) => {
    setRules(rules.map(rule =>
      rule.id === id ? { ...rule, active: !rule.active } : rule
    ));
  };

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-800">Rule Management</h1>
        <p className="text-slate-500 mt-1">Configure automated fraud detection rules.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
        {rules.map((rule) => (
          <div key={rule.id} className="bg-white rounded-lg shadow-sm border border-slate-200 p-5 flex flex-col">
            <div className="flex justify-between items-start mb-4">
              <div className="flex items-center space-x-3">
                <div className={`p-2 rounded-md ${rule.active ? 'bg-emerald-100 text-emerald-600' : 'bg-slate-100 text-slate-400'}`}>
                  {rule.active ? <Shield size={20} /> : <ShieldOff size={20} />}
                </div>
                <h3 className="font-semibold text-slate-800">{rule.name}</h3>
              </div>
              <button
                onClick={() => toggleRule(rule.id)}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2 ${
                  rule.active ? 'bg-emerald-500' : 'bg-slate-300'
                }`}
              >
                <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                  rule.active ? 'translate-x-6' : 'translate-x-1'
                }`} />
              </button>
            </div>

            <p className="text-sm text-slate-600 flex-grow">{rule.description}</p>

            <div className="mt-4 pt-4 border-t border-slate-100 flex justify-between items-center text-xs">
              <span className="text-slate-400">ID: {rule.id}</span>
              <span className={`font-medium ${rule.active ? 'text-emerald-600' : 'text-slate-500'}`}>
                {rule.active ? 'Active' : 'Disabled'}
              </span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
