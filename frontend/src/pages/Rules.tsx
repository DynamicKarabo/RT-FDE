import { useState } from 'react';
import { mockRules } from '../data/mockData';
import { Shield, ShieldOff } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';

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
          <Card key={rule.id}>
            <CardHeader className="pb-3">
              <div className="flex justify-between items-start">
                <div className="flex items-center space-x-3">
                  <div className={`p-2 rounded-md ${rule.active ? 'bg-emerald-100 text-emerald-600' : 'bg-slate-100 text-slate-400'}`}>
                    {rule.active ? <Shield size={20} /> : <ShieldOff size={20} />}
                  </div>
                  <CardTitle className="text-base">{rule.name}</CardTitle>
                </div>
                <Switch 
                  checked={rule.active} 
                  onCheckedChange={() => toggleRule(rule.id)}
                />
              </div>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-slate-600 mb-4">{rule.description}</p>
              <Separator className="mb-3" />
              <div className="flex justify-between items-center text-xs">
                <span className="text-slate-400">ID: {rule.id}</span>
                <Badge variant={rule.active ? 'default' : 'secondary'}>
                  {rule.active ? 'Active' : 'Disabled'}
                </Badge>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
