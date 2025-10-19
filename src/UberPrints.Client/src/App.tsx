import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { Layout } from './components/layout/Layout';
import { ProtectedRoute } from './components/auth/ProtectedRoute';
import { Toaster } from './components/ui/toaster';
import { Home } from './pages/Home';
import { AuthCallback } from './pages/AuthCallback';
import { RequestList } from './pages/RequestList';
import { RequestDetail } from './pages/RequestDetail';
import { NewRequest } from './pages/NewRequest';
import { EditRequest } from './pages/EditRequest';
import { TrackRequest } from './pages/TrackRequest';
import { Filaments } from './pages/Filaments';
import { FilamentRequests } from './pages/FilamentRequests';
import { Dashboard } from './pages/Dashboard';
import { Profile } from './pages/Profile';
import { AdminDashboard } from './pages/AdminDashboard';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Toaster />
        <Routes>
          {/* Public routes */}
          <Route path="/" element={<Layout><Home /></Layout>} />
          <Route path="/auth/callback" element={<AuthCallback />} />
          <Route path="/requests" element={<Layout><RequestList /></Layout>} />
          <Route path="/request/:id" element={<Layout><RequestDetail /></Layout>} />
          <Route path="/request/:id/edit" element={<Layout><EditRequest /></Layout>} />
          <Route path="/request/new" element={<Layout><NewRequest /></Layout>} />
          <Route path="/track" element={<Layout><TrackRequest /></Layout>} />
          <Route path="/filaments" element={<Layout><Filaments /></Layout>} />
          <Route path="/filament-requests" element={<Layout><FilamentRequests /></Layout>} />
          <Route path="/dashboard" element={<Layout><Dashboard /></Layout>} />

          {/* Protected routes - require authentication */}
          <Route
            path="/profile"
            element={
              <ProtectedRoute>
                <Layout><Profile /></Layout>
              </ProtectedRoute>
            }
          />

          {/* Admin routes - require admin role */}
          <Route
            path="/admin"
            element={
              <ProtectedRoute requireAdmin>
                <Layout><AdminDashboard /></Layout>
              </ProtectedRoute>
            }
          />

          {/* 404 page */}
          <Route
            path="*"
            element={
              <Layout>
                <div className="text-center py-12">
                  <h1 className="text-4xl font-bold mb-4">404</h1>
                  <p className="text-muted-foreground">Page not found</p>
                </div>
              </Layout>
            }
          />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
