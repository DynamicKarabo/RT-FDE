import { useState } from 'react';
import { mockRules } from '../data/mockData';
import { Shield, ShieldOff, Edit2 } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';

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

      <Card>
        <CardHeader>
          <CardTitle>Rules List</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[80px]">Status</TableHead>
                  <TableHead>Rule Name</TableHead>
                  <TableHead>Description</TableHead>
                  <TableHead>ID</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rules.map((rule) => (
                  <TableRow key={rule.id}>
                    <TableCell>
                      <div className={`p-2 rounded-md inline-block ${rule.active ? 'bg-emerald-100 text-emerald-600' : 'bg-slate-100 text-slate-400'}`}>
                        {rule.active ? <Shield size={16} /> : <ShieldOff size={16} />}
                      </div>
                    </TableCell>
                    <TableCell className="font-medium">
                      {rule.name}
                      <div className="mt-1">
                        <Badge variant={rule.active ? 'default' : 'secondary'} className={rule.active ? 'bg-emerald-500 hover:bg-emerald-600 text-white' : ''}>
                          {rule.active ? 'Active' : 'Disabled'}
                        </Badge>
                      </div>
                    </TableCell>
                    <TableCell className="text-slate-600">{rule.description}</TableCell>
                    <TableCell className="text-slate-400 font-mono text-xs">{rule.id}</TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end items-center space-x-2">
                        <Dialog>
                          <DialogTrigger render={<Button variant="ghost" size="icon" className="h-8 w-8 text-slate-400 hover:text-slate-600"><Edit2 size={16} /></Button>}/>
                          <DialogContent>
                            <DialogHeader>
                              <DialogTitle>Edit Rule: {rule.name}</DialogTitle>
                              <DialogDescription>
                                Make changes to this fraud detection rule.
                              </DialogDescription>
                            </DialogHeader>
                            <div className="py-4">
                              <p className="text-sm text-slate-500">Edit form placeholder. Add inputs here to edit {rule.name}.</p>
                            </div>
                          </DialogContent>
                        </Dialog>

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
