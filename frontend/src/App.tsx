import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Provider } from 'react-redux';
import { store } from './app/store';
import { Toaster } from '@/components/common/sonner';
import { MainLayout } from '@/components/layout/MainLayout';
import { AdminLayout } from '@/components/layout/AdminLayout';
import { SellerLayout } from '@/components/layout/SellerLayout';
import { ProtectedRoute } from '@/components/common/ProtectedRoute';
import { ThemeProvider } from '@/components/common/ThemeProvider';
import { DevToolsProvider } from '@/components/common/DevToolsProvider';

// Pages
import Home from '@/pages/Home';
import Login from '@/pages/Login';
import Register from '@/pages/Register';
import Cart from '@/pages/Cart';
import Checkout from '@/pages/Checkout';
import Orders from '@/pages/Orders';
import OrderDetail from '@/pages/OrderDetail';
import ProductDetail from '@/pages/ProductDetail';
import AdminDashboard from '@/pages/admin/Dashboard';
import AdminProducts from '@/pages/admin/ProductsAdmin';
import ProductForm from '@/pages/admin/ProductForm';
import AdminCategories from '@/pages/admin/CategoriesAdmin';
import AdminOrders from '@/pages/admin/OrdersAdmin';
import AdminCoupons from '@/pages/admin/CouponsAdmin';
import Account from '@/pages/Account';
import Addresses from '@/pages/Addresses';
import Help from '@/pages/Help';

// Seller Pages
import SellerDashboard from '@/pages/seller/Dashboard';
import SellerProducts from '@/pages/seller/Products';
import SellerProductForm from '@/pages/seller/ProductForm';
import SellerProfile from '@/pages/seller/Profile';

function App() {
  return (
    <Provider store={store}>
      <ThemeProvider defaultTheme="system" storageKey="ecommerce-theme">
        <DevToolsProvider>
          <BrowserRouter>
            <Routes>
              
              <Route element={<MainLayout />}>
                <Route path="/" element={<Home />} />
                <Route path="/login" element={<Login />} />
                <Route path="/register" element={<Register />} />
                <Route path="/products/:id" element={<ProductDetail />} />
                
                {/* Protected customer routes */}
                <Route
                  path="/cart"
                  element={
                    <ProtectedRoute>
                      <Cart />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/checkout"
                  element={
                    <ProtectedRoute>
                      <Checkout />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/orders"
                  element={
                    <ProtectedRoute>
                      <Orders />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/orders/:id"
                  element={
                    <ProtectedRoute>
                      <OrderDetail />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/account"
                  element={
                    <ProtectedRoute>
                      <Account />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/account/addresses"
                  element={
                    <ProtectedRoute>
                      <Addresses />
                    </ProtectedRoute>
                  }
                />
                <Route path="/help" element={<Help />} />
              </Route>

              {/* Admin routes */}
              <Route
                path="/admin"
                element={
                  <ProtectedRoute requiredRole="Admin">
                    <AdminLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<AdminDashboard />} />
                <Route path="products" element={<AdminProducts />} />
                <Route path="products/new" element={<ProductForm />} />
                <Route path="products/:id" element={<ProductForm />} />
                <Route path="categories" element={<AdminCategories />} />
                <Route path="orders" element={<AdminOrders />} />
                <Route path="coupons" element={<AdminCoupons />} />
              </Route>

              {/* Seller routes */}
              <Route
                path="/seller"
                element={
                  <ProtectedRoute requiredRole="Seller">
                    <SellerLayout />
                  </ProtectedRoute>
                }
              >
                <Route index element={<SellerDashboard />} />
                <Route path="products" element={<SellerProducts />} />
                <Route path="products/new" element={<SellerProductForm />} />
                <Route path="products/:id" element={<SellerProductForm />} />
                <Route path="profile" element={<SellerProfile />} />
              </Route>
            </Routes>
            <Toaster position="top-right" richColors closeButton style={{ top: '80px' }} />
          </BrowserRouter>
        </DevToolsProvider>
      </ThemeProvider>
    </Provider>
  );
}

export default App;

