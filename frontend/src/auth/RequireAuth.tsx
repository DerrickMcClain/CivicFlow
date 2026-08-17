import { Navigate, Outlet } from 'react-router-dom'
import { homePathForRole, useAuth } from '../auth/AuthContext'

type Props = {
  roles?: string[]
}

export function RequireAuth({ roles }: Props) {
  const { user } = useAuth()

  if (!user) {
    return <Navigate to="/login" replace />
  }

  if (roles && !roles.includes(user.role)) {
    return <Navigate to={homePathForRole(user.role)} replace />
  }

  return <Outlet />
}
