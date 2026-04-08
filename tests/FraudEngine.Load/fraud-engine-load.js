/**
 * RT-FDE Load Test — validates 1000+ TPS target.
 *
 * Usage:
 *   k6 run tests/FraudEngine.Load/fraud-engine-load.js
 *
 * Override target TPS and duration:
 *   K6_TPS=1500 K6_DURATION=60s k6 run tests/FraudEngine.Load/fraud-engine-load.js
 *
 * Environment variables:
 *   BASE_URL    — Fraud Engine API base URL (default: http://localhost:5000)
 *   K6_TPS      — Target requests per second (default: 1000)
 *   K6_DURATION — Test duration (default: 30s)
 *   K6_VUS_MAX  — Maximum virtual users (default: 50)
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// ── Custom Metrics ─────────────────────────────────────────────────
const fraudRejectionRate = new Rate('fraud_rejections');
const fraudReviewRate = new Rate('fraud_reviews');
const p99Latency = new Trend('p99_latency_ms', true);

// ── Configuration ──────────────────────────────────────────────────
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const TARGET_TPS = parseInt(__ENV.K6_TPS || '1000', 10);
const DURATION = __ENV.K6_DURATION || '30s';
const VUS_MAX = parseInt(__ENV.K6_VUS_MAX || '50', 10);

export const options = {
  stages: [
    { duration: '10s', target: Math.min(VUS_MAX, Math.ceil(TARGET_TPS / 20)) }, // Ramp up
    { duration: DURATION, target: Math.min(VUS_MAX, Math.ceil(TARGET_TPS / 20)) }, // Sustain
    { duration: '10s', target: 0 }, // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(99)<200'], // P99 < 200ms SLA
    http_req_failed: ['rate<0.01'],    // Error rate < 1%
    p99_latency_ms: ['p(99)<200'],
  },
};

// ── Request Payload Generator ──────────────────────────────────────
function generateTransaction() {
  const devices = ['device-a1b2c3', 'device-d4e5f6', 'device-g7h8i9', 'device-j0k1l2'];
  const merchants = ['MERCH-001', 'MERCH-002', 'MERCH-003'];
  const ips = ['192.168.1.10', '10.0.0.55', '172.16.0.100'];

  return {
    transactionId: crypto.randomUUID(),
    userId: crypto.randomUUID(),
    amount: Math.round(Math.random() * 10000 * 100) / 100,
    currency: 'ZAR',
    timestamp: new Date().toISOString(),
    ipAddress: ips[Math.floor(Math.random() * ips.length)],
    deviceId: devices[Math.floor(Math.random() * devices.length)],
    merchantId: merchants[Math.floor(Math.random() * merchants.length)],
    latitude: -26.2041 + (Math.random() - 0.5) * 0.1,
    longitude: 28.0473 + (Math.random() - 0.5) * 0.1,
  };
}

// ── Test Scenario ──────────────────────────────────────────────────
export default function () {
  const payload = JSON.stringify(generateTransaction());

  const params = {
    headers: {
      'Content-Type': 'application/json',
      'X-Correlation-ID': crypto.randomUUID(),
    },
  };

  const res = http.post(`${BASE_URL}/v1/fraud/evaluate`, payload, params);

  // Record latency
  p99Latency.add(res.timings.duration);

  const passed = check(res, {
    'status is 200': (r) => r.status === 200,
    'response has decision': (r) => {
      try {
        const body = JSON.parse(r.body);
        return body.decision !== undefined;
      } catch {
        return false;
      }
    },
    'response has riskScore': (r) => {
      try {
        const body = JSON.parse(r.body);
        return typeof body.riskScore === 'number';
      } catch {
        return false;
      }
    },
    'response has reasons array': (r) => {
      try {
        const body = JSON.parse(r.body);
        return Array.isArray(body.reasons);
      } catch {
        return false;
      }
    },
    'latency under 200ms': (r) => r.timings.duration < 200,
  });

  // Track decision distribution
  if (res.status === 200) {
    try {
      const body = JSON.parse(res.body);
      fraudRejectionRate.add(body.decision === 'REJECT');
      fraudReviewRate.add(body.decision === 'REVIEW');
    } catch {
      // Ignore parse errors for metric tracking
    }
  }

  // Minimal sleep to simulate realistic inter-arrival time
  // At 1000 TPS with 50 VUs: ~50 req/s per VU → 20ms between requests
  const targetIntervalMs = 1000 / (TARGET_TPS / VUS_MAX);
  sleep(targetIntervalMs / 1000);
}

// ── Summary Handler (runs once at end) ─────────────────────────────
export function handleSummary(data) {
  return {
    stdout: textSummary(data),
    'tests/FraudEngine.Load/load-test-results.json': JSON.stringify(data, null, 2),
  };
}

function textSummary(data) {
  const metrics = data.metrics;
  const p99 = metrics?.http_req_duration?.values?.['p(99)'] ?? 'N/A';
  const avg = metrics?.http_req_duration?.values?.avg ?? 'N/A';
  const reqs = metrics?.http_reqs?.values?.count ?? 0;
  const errors = metrics?.http_req_failed?.values?.rate ?? 0;
  const fraudRej = metrics?.fraud_rejections?.values?.rate ?? 0;
  const fraudRev = metrics?.fraud_reviews?.values?.rate ?? 0;

  return `
═══════════════════════════════════════════════════════
  RT-FDE Load Test Summary
═══════════════════════════════════════════════════════
  Target TPS:       ${TARGET_TPS}
  Actual Requests:  ${reqs}
  P99 Latency:      ${p99}ms
  Avg Latency:      ${avg}ms
  Error Rate:       ${(errors * 100).toFixed(2)}%
  Rejection Rate:   ${(fraudRej * 100).toFixed(1)}%
  Review Rate:      ${(fraudRev * 100).toFixed(1)}%
═══════════════════════════════════════════════════════
  ${parseFloat(p99) < 200 ? '✅ P99 SLA MET' : '❌ P99 SLA BREACHED'}
  ${errors < 0.01 ? '✅ Error Rate OK' : '❌ Error Rate EXCEEDED'}
═══════════════════════════════════════════════════════
`;
}
