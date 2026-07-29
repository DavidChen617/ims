import { useQueryClient } from '@tanstack/react-query'
import { Navigate, Route, Routes } from 'react-router-dom'
import { Shell } from './components/Shell'
import { useSession } from './hooks/useSession'
import { InboundHistoryPage } from './pages/InboundHistoryPage'
import { InboundOperationsPage } from './pages/InboundOperationsPage'
import { LoginPage } from './pages/LoginPage'
import { OutboundHistoryPage } from './pages/OutboundHistoryPage'
import { OutboundOperationsPage } from './pages/OutboundOperationsPage'
import { ProductsPage } from './pages/ProductsPage'
import { StocksPage } from './pages/StocksPage'
import { UsersPage } from './pages/UsersPage'
import { WarehousesPage } from './pages/WarehousesPage'

export default function App() {
  const session = useSession()
  const queryClient = useQueryClient()

  if (!session.token || !session.profile) {
    return (
      <LoginPage
        onLogin={(token, refreshToken) => {
          session.signIn(token, refreshToken)
          queryClient.clear()
        }}
      />
    )
  }

  return (
    <Routes>
      <Route
        element={
          <Shell
            profile={session.profile}
            signOut={() => {
              session.signOut()
              queryClient.clear()
            }}
            token={session.token}
          />
        }
      >
        <Route element={<Navigate replace to="/products" />} index />
        <Route element={<ProductsPage />} path="products" />
        <Route element={<InboundOperationsPage />} path="inbound/operations" />
        <Route element={<OutboundOperationsPage />} path="outbound/operations" />
        <Route element={<InboundHistoryPage />} path="inbound/history" />
        <Route element={<OutboundHistoryPage />} path="outbound/history" />
        <Route element={<StocksPage />} path="stocks" />
        <Route element={<WarehousesPage />} path="warehouses" />
        <Route element={<UsersPage />} path="users" />
        <Route element={<Navigate replace to="/products" />} path="*" />
      </Route>
    </Routes>
  )
}
