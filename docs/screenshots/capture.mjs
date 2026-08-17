import { mkdir } from 'node:fs/promises'
import { dirname, join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium } from 'playwright'

const BASE = process.env.CIVICFLOW_URL ?? 'http://localhost'
const PASS = 'CivicFlow!dev1'
const OUT = dirname(fileURLToPath(import.meta.url))

async function loginApi(email) {
  const res = await fetch(`${BASE}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password: PASS }),
  })
  if (!res.ok) {
    throw new Error(`login ${email} failed: ${res.status} ${await res.text()}`)
  }
  return res.json()
}

async function api(token, path, options = {}) {
  const res = await fetch(`${BASE}${path}`, {
    ...options,
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
      ...(options.headers ?? {}),
    },
  })
  const text = await res.text()
  if (!res.ok) {
    throw new Error(`${options.method ?? 'GET'} ${path} failed: ${res.status} ${text}`)
  }
  return text ? JSON.parse(text) : null
}

async function seed() {
  const citizen = await loginApi('citizen@civicflow.local')
  const types = await api(citizen.token, '/api/catalog/request-types')
  const residential =
    types.find((t) => t.name === 'Residential Permit') ?? types[0]

  const timelineCase = await api(citizen.token, '/api/requests', {
    method: 'POST',
    body: JSON.stringify({
      requestTypeId: residential.serviceRequestTypeId,
      title: 'Deck addition permit',
      description: '12x16 pressure-treated deck in the rear yard, under 30 inches above grade.',
      priority: 'Medium',
    }),
  })

  const actionCase = await api(citizen.token, '/api/requests', {
    method: 'POST',
    body: JSON.stringify({
      requestTypeId: residential.serviceRequestTypeId,
      title: 'Fence replacement along Maple St',
      description: 'Replace 6-foot privacy fence on the side lot line.',
      priority: 'High',
    }),
  })

  const employee = await loginApi('employee@civicflow.local')
  const putStatus = (id, status, reason) =>
    api(employee.token, `/api/requests/${id}/status`, {
      method: 'PUT',
      body: JSON.stringify({ status, reason }),
    })

  await putStatus(timelineCase.requestId, 'UnderReview', 'Assigned for completeness check.')
  await api(employee.token, `/api/requests/${timelineCase.requestId}/notes`, {
    method: 'POST',
    body: JSON.stringify({
      noteText: 'Plans look complete. Routing for recommendation.',
      isInternal: true,
    }),
  })
  await putStatus(timelineCase.requestId, 'EmployeeRecommendation', 'Recommend approval.')
  await putStatus(timelineCase.requestId, 'SupervisorReview', 'Ready for supervisor decision.')

  const supervisor = await loginApi('supervisor@civicflow.local')
  await api(supervisor.token, `/api/requests/${timelineCase.requestId}/approve`, {
    method: 'POST',
    body: JSON.stringify({ reason: 'Meets residential accessory structure rules.' }),
  })

  await putStatus(actionCase.requestId, 'UnderReview', 'Intake complete.')

  return {
    timelineId: timelineCase.requestId,
    actionId: actionCase.requestId,
  }
}

async function signIn(page, roleLabel) {
  await page.goto(`${BASE}/login`)
  await page.evaluate(() => localStorage.clear())
  await page.reload()
  await page.getByRole('heading', { name: 'Sign in' }).waitFor()
  await page.getByRole('button', { name: new RegExp(roleLabel) }).click()
  await page.waitForURL((url) => !url.pathname.startsWith('/login'))
}

async function shot(page, name) {
  await page.waitForTimeout(400)
  await page.screenshot({
    path: join(OUT, `${name}.png`),
    fullPage: true,
  })
  console.log(`wrote ${name}.png`)
}

const ids = await seed()
await mkdir(OUT, { recursive: true })

const browser = await chromium.launch()
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } })

await page.goto(`${BASE}/login`)
await page.getByRole('heading', { name: 'Sign in' }).waitFor()
await shot(page, '01-login')

await signIn(page, 'Citizen portal')
await page.goto(`${BASE}/citizen/requests/${ids.timelineId}`)
await page.getByRole('heading', { name: 'Deck addition permit' }).waitFor()
await shot(page, '02-citizen-request-detail')

await signIn(page, 'Staff work queue')
await page.goto(`${BASE}/staff`)
await page.getByRole('heading', { name: 'Work queue' }).waitFor()
await page.getByText('Fence replacement along Maple St').waitFor()
await shot(page, '03-staff-queue')
await page.goto(`${BASE}/staff/requests/${ids.actionId}`)
await page.getByRole('heading', { name: 'Fence replacement along Maple St' }).waitFor()
await shot(page, '04-staff-case-actions')

await signIn(page, 'Staff \\+ approvals')
await page.goto(`${BASE}/staff/dashboard`)
await page.getByRole('heading', { name: 'Supervisor dashboard' }).waitFor()
await page.getByText('Open cases').waitFor()
await shot(page, '05-supervisor-dashboard')

await signIn(page, 'Admin console')
await page.goto(`${BASE}/admin/audit`)
await page.getByRole('heading', { name: 'Audit log' }).waitFor()
await page.locator('table, p').first().waitFor()
await page.getByText('Loading…').waitFor({ state: 'hidden' }).catch(() => {})
await shot(page, '06-admin-audit-log')

await browser.close()
