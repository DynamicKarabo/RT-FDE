
import { BrowserRouter as Router, Routes, Route, NavLink } from 'react-router-dom';
import { LayoutDashboard, ListOrdered, ShieldCheck, ClipboardList, LogOut } from 'lucide-react';
import Dashboard from './pages/Dashboard';
import Transactions from './pages/Transactions';
import Rules from './pages/Rules';
import AuditLog from './pages/AuditLog';

function Sidebar() {
  const navItems = [
    { path: '/', icon: <LayoutDashboard size={20} />, label: 'Dashboard' },
    { path: '/transactions', icon: <ListOrdered size={20} />, label: 'Transactions' },
    { path: '/rules', icon: <ShieldCheck size={20} />, label: 'Rules' },
    { path: '/audit-log', icon: <ClipboardList size={20} />, label: 'Audit Log' },
  ];

  return (
    <div className="w-64 bg-slate-900 text-slate-300 flex flex-col h-screen fixed left-0 top-0">
      <div className="p-6 flex items-center space-x-3">
        <div className="w-8 h-8 bg-emerald-500 rounded-md flex items-center justify-center text-white font-bold">
          R
        </div>
        <span className="text-xl font-bold text-white tracking-wide">RiskGuard</span>
      </div>

      <nav className="flex-1 px-4 space-y-2 mt-4">
        {navItems.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            className={({ isActive }) =>
              `flex items-center space-x-3 px-4 py-3 rounded-lg transition-colors ${
                isActive
                  ? 'bg-slate-800 text-emerald-400 font-medium border-l-4 border-emerald-500'
                  : 'hover:bg-slate-800 hover:text-white'
              }`
            }
          >
            {item.icon}
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>

      <div className="p-4 border-t border-slate-800">
        <button className="flex items-center space-x-3 px-4 py-2 w-full text-left text-slate-400 hover:text-white transition-colors rounded-lg hover:bg-slate-800">
          <LogOut size={20} />
          <span>Sign Out</span>
        </button>
      </div>
    </div>
  );
}

function App() {
  return (
    <Router>
      <div className="flex min-h-screen bg-slate-50">
        <Sidebar />
        <main className="flex-1 ml-64 bg-slate-50 min-h-screen">
          <Routes>
            <Route path="/" element={<Dashboard />} />
            <Route path="/transactions" element={<Transactions />} />
            <Route path="/rules" element={<Rules />} />
            <Route path="/audit-log" element={<AuditLog />} />
          </Routes>
        </main>
      </div>
    </Router>
  );
}

export default App;
