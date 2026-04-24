import { BrowserRouter as Router, Routes, Route, NavLink } from 'react-router-dom';
import { LayoutDashboard, ListOrdered, ShieldCheck, ClipboardList, LogOut, Menu, X } from 'lucide-react';
import { useState, useEffect } from 'react';
import Dashboard from './pages/Dashboard';
import Transactions from './pages/Transactions';
import Rules from './pages/Rules';
import AuditLog from './pages/AuditLog';

function Sidebar({ mobileOpen, onClose }: { mobileOpen: boolean; onClose: () => void }) {
  const navItems = [
    { path: '/', icon: <LayoutDashboard size={20} />, label: 'Dashboard' },
    { path: '/transactions', icon: <ListOrdered size={20} />, label: 'Transactions' },
    { path: '/rules', icon: <ShieldCheck size={20} />, label: 'Rules' },
    { path: '/audit-log', icon: <ClipboardList size={20} />, label: 'Audit Log' },
  ];

  return (
    <>
      {/* Mobile overlay */}
      {mobileOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-40 lg:hidden"
          onClick={onClose}
        />
      )}
      <div
        className={`
          fixed lg:static inset-y-0 left-0 z-50 w-64 bg-slate-900 text-slate-300 flex flex-col h-screen
          transform transition-transform duration-300 ease-in-out
          lg:transform-none
          ${mobileOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}
        `}
      >
        <div className="p-6 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="w-8 h-8 bg-emerald-500 rounded-md flex items-center justify-center text-white font-bold">
              R
            </div>
            <span className="text-xl font-bold text-white tracking-wide">RiskGuard</span>
          </div>
          <button onClick={onClose} className="lg:hidden text-slate-400 hover:text-white">
            <X size={24} />
          </button>
        </div>

        <nav className="flex-1 px-4 space-y-2 mt-4">
          {navItems.map((item) => (
            <NavLink
              key={item.path}
              to={item.path}
              onClick={onClose}
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
    </>
  );
}

function App() {
  const [mobileOpen, setMobileOpen] = useState(false);

  useEffect(() => {
    const handleResize = () => {
      if (window.innerWidth >= 1024) setMobileOpen(false);
    };
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  return (
    <Router>
      <div className="flex min-h-screen bg-slate-50">
        <Sidebar mobileOpen={mobileOpen} onClose={() => setMobileOpen(false)} />
        <main className="flex-1 bg-slate-50 min-h-screen w-full lg:ml-0">
          {/* Mobile header */}
          <div className="lg:hidden flex items-center justify-between p-4 bg-slate-900 text-white">
            <div className="flex items-center space-x-3">
              <div className="w-8 h-8 bg-emerald-500 rounded-md flex items-center justify-center text-white font-bold">
                R
              </div>
              <span className="text-lg font-bold tracking-wide">RiskGuard</span>
            </div>
            <button onClick={() => setMobileOpen(true)} className="p-2 hover:bg-slate-800 rounded-lg">
              <Menu size={24} />
            </button>
          </div>
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
