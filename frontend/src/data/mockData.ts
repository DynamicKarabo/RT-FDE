export interface KPIData {
  totalTransactions: number;
  fraudAlerts: number;
  riskScoreAvg: number;
  blockedTransactions: number;
}

export interface RiskDataPoint {
  time: string;
  score: number;
}

export interface Alert {
  id: string;
  transactionId: string;
  amount: number;
  reason: string;
  status: 'Open' | 'Investigating' | 'Resolved';
  timestamp: string;
}

export interface Transaction {
  id: string;
  date: string;
  amount: number;
  merchant: string;
  status: 'Approved' | 'Declined' | 'Pending';
  riskScore: number;
}

export interface Rule {
  id: string;
  name: string;
  description: string;
  active: boolean;
}

export interface AuditLogEntry {
  id: string;
  timestamp: string;
  user: string;
  action: string;
  details: string;
}

export const mockKPIs: KPIData = {
  totalTransactions: 125430,
  fraudAlerts: 342,
  riskScoreAvg: 12.4,
  blockedTransactions: 89,
};

export const mockRiskData: RiskDataPoint[] = [
  { time: '00:00', score: 10 },
  { time: '04:00', score: 12 },
  { time: '08:00', score: 25 },
  { time: '12:00', score: 18 },
  { time: '16:00', score: 35 },
  { time: '20:00', score: 15 },
  { time: '24:00', score: 11 },
];

export const mockAlerts: Alert[] = [
  { id: 'AL-101', transactionId: 'TX-9876', amount: 5000.00, reason: 'High velocity', status: 'Open', timestamp: '2023-10-25 10:30:00' },
  { id: 'AL-102', transactionId: 'TX-9877', amount: 150.00, reason: 'Location mismatch', status: 'Investigating', timestamp: '2023-10-25 10:45:00' },
  { id: 'AL-103', transactionId: 'TX-9878', amount: 12000.00, reason: 'Unusual amount', status: 'Resolved', timestamp: '2023-10-25 11:15:00' },
];

export const mockTransactions: Transaction[] = [
  { id: 'TX-9876', date: '2023-10-25', amount: 5000.00, merchant: 'Acme Electronics', status: 'Declined', riskScore: 85 },
  { id: 'TX-9877', date: '2023-10-25', amount: 150.00, merchant: 'Coffee Shop', status: 'Pending', riskScore: 45 },
  { id: 'TX-9878', date: '2023-10-25', amount: 12000.00, merchant: 'Luxury Autos', status: 'Declined', riskScore: 92 },
  { id: 'TX-9879', date: '2023-10-24', amount: 45.00, merchant: 'Grocery Store', status: 'Approved', riskScore: 5 },
  { id: 'TX-9880', date: '2023-10-24', amount: 200.00, merchant: 'Online Retailer', status: 'Approved', riskScore: 12 },
];

export const mockRules: Rule[] = [
  { id: 'R-1', name: 'Velocity Check', description: 'Triggers if >5 transactions within 10 minutes', active: true },
  { id: 'R-2', name: 'High Amount', description: 'Triggers if transaction amount > $10,000', active: true },
  { id: 'R-3', name: 'Location Mismatch', description: 'Triggers if billing and shipping countries differ', active: false },
  { id: 'R-4', name: 'New Device', description: 'Triggers on first transaction from a new device', active: true },
  { id: 'R-5', name: 'Multiple Failures', description: 'Triggers after 3 consecutive failed attempts', active: true },
  { id: 'R-6', name: 'Time of Day', description: 'Flags transactions between 2 AM and 4 AM local time', active: false },
];

export const mockAuditLogs: AuditLogEntry[] = [
  { id: 'L-1', timestamp: '2023-10-25 14:30:00', user: 'admin@example.com', action: 'Rule Updated', details: 'Activated rule "Location Mismatch"' },
  { id: 'L-2', timestamp: '2023-10-25 13:15:00', user: 'system', action: 'Alert Generated', details: 'Alert AL-103 created for TX-9878' },
  { id: 'L-3', timestamp: '2023-10-24 09:00:00', user: 'analyst@example.com', action: 'Alert Resolved', details: 'Resolved AL-099 as false positive' },
  { id: 'L-4', timestamp: '2023-10-23 16:45:00', user: 'admin@example.com', action: 'User Added', details: 'Added new analyst account' },
];
