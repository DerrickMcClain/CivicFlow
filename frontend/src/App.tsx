import { Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider, homePathForRole, useAuth } from './auth/AuthContext'
import { RequireAuth } from './auth/RequireAuth'
import { AdminHomePage } from './pages/AdminHomePage'
import { LoginPage } from './pages/LoginPage'
import { StaffHomePage } from './pages/StaffHomePage'
import { CitizenLayout } from './pages/citizen/CitizenLayout'
import { DashboardPage } from './pages/citizen/DashboardPage'
import { RequestDetailPage } from './pages/citizen/RequestDetailPage'
import { SubmitRequestPage } from './pages/citizen/SubmitRequestPage'

function HomeRedirect() {
  const { user } = useAuth()
  if (!user) {
    return <Navigate to="/login" replace />
  }
  return <Navigate to={homePathForRole(user.role)} replace />
}

export default function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<HomeRedirect />} />

        <Route element={<RequireAuth roles={['Citizen']} />}>
          <Route path="/citizen" element={<CitizenLayout />}>
            <Route index element={<DashboardPage />} />
            <Route path="new" element={<SubmitRequestPage />} />
            <Route path="requests/:id" element={<RequestDetailPage />} />
          </Route>
        </Route>

        <Route element={<RequireAuth roles={['Employee', 'Supervisor']} />}>
          <Route path="/staff/*" element={<StaffHomePage />} />
        </Route>

        <Route element={<RequireAuth roles={['Administrator']} />}>
          <Route path="/admin/*" element={<AdminHomePage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  )
}
