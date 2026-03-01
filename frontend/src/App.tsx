import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Provider } from 'react-redux';
import { lazy, Suspense } from 'react';
import { store } from './app/store';
import { Toaster } from '@/components/common/sonner';
import { MainLayout } from '@/components/layout/MainLayout';
import { AdminLayout } from '@/components/layout/AdminLayout';
import { SellerLayout } from '@/components/layout/SellerLayout';
import { ProtectedRoute } from '@/components/common/ProtectedRoute';
import { ErrorBoundary } from '@/components/common/ErrorBoundary';
import { ThemeProvider } from '@/components/common/ThemeProvider';
import { DevToolsProvider } from '@/components/common/DevToolsProvider';

// Lazy-loaded pages (Code Splitting)
const Home = lazy(() => import('@/pages/Home'));
const Login = lazy(() => import('@/pages/Login'));
const Register = lazy(() => import('@/pages/Register'));
const Cart = lazy(() => import('@/pages/Cart'));
const Checkout = lazy(() => import('@/pages/Checkout'));
const Orders = lazy(() => import('@/pages/Orders'));
const OrderDetail = lazy(() => import('@/pages/OrderDetail'));
const Returns = lazy(() => import('@/pages/Returns'));
const Notifications = lazy(() => import('@/pages/Notifications'));
const Loyalty = lazy(() => import('@/pages/Loyalty'));
const ProductDetail = lazy(() => import('@/pages/ProductDetail'));
const Account = lazy(() => import('@/pages/Account'));
const Addresses = lazy(() => import('@/pages/Addresses'));
const CreditCards = lazy(() => import('@/pages/CreditCards'));
const Help = lazy(() => import('@/pages/Help'));
const Support = lazy(() => import('@/pages/Support'));
const Wishlist = lazy(() => import('@/pages/Wishlist'));
const SharedWishlist = lazy(() => import('@/pages/SharedWishlist'));
const About = lazy(() => import('@/pages/About'));
const PrivacyPolicy = lazy(() => import('@/pages/PrivacyPolicy'));
const TermsOfService = lazy(() => import('@/pages/TermsOfService'));
const Kvkk = lazy(() => import('@/pages/Kvkk'));
const RefundPolicy = lazy(() => import('@/pages/RefundPolicy'));
const DistanceSalesContract = lazy(() => import('@/pages/DistanceSalesContract'));
const Shipping = lazy(() => import('@/pages/Shipping'));
const Faq = lazy(() => import('@/pages/Faq'));
const Contact = lazy(() => import('@/pages/Contact'));
const CookiePolicy = lazy(() => import('@/pages/CookiePolicy'));
const SellerGuide = lazy(() => import('@/pages/SellerGuide'));
const SellerPricing = lazy(() => import('@/pages/SellerPricing'));
const SellerRegister = lazy(() => import('@/pages/SellerRegister'));

// Admin pages
const AdminDashboard = lazy(() => import('@/pages/admin/Dashboard'));
const AdminProducts = lazy(() => import('@/pages/admin/ProductsAdmin'));
const ProductForm = lazy(() => import('@/pages/admin/ProductForm'));
const AdminCategories = lazy(() => import('@/pages/admin/CategoriesAdmin'));
const AdminOrders = lazy(() => import('@/pages/admin/OrdersAdmin'));
const AdminCoupons = lazy(() => import('@/pages/admin/CouponsAdmin'));

// Seller pages
const SellerDashboard = lazy(() => import('@/pages/seller/Dashboard'));
const SellerProducts = lazy(() => import('@/pages/seller/Products'));
const SellerProductForm = lazy(() => import('@/pages/seller/ProductForm'));
const SellerProfile = lazy(() => import('@/pages/seller/Profile'));

function App() {
  return (
    <Provider store={store}>
      <ThemeProvider defaultTheme="system" storageKey="ecommerce-theme">
        <DevToolsProvider>
          <BrowserRouter>
            <ErrorBoundary>
              <Suspense fallback={
                <div className="flex items-center justify-center min-h-screen">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary" />
                </div>
              }>
                <Routes>

                  <Route element={<MainLayout />}>
                    <Route path="/" element={<Home />} />
                    <Route path="/login" element={<Login />} />
                    <Route path="/register" element={<Register />} />
                    <Route path="/products/:id" element={<ProductDetail />} />
                    <Route path="/about" element={<About />} />
                    <Route path="/privacy-policy" element={<PrivacyPolicy />} />
                    <Route path="/terms-of-service" element={<TermsOfService />} />
                    <Route path="/kvkk" element={<Kvkk />} />
                    <Route path="/refund-policy" element={<RefundPolicy />} />
                    <Route path="/distance-sales-contract" element={<DistanceSalesContract />} />
                    <Route path="/shipping" element={<Shipping />} />
                    <Route path="/faq" element={<Faq />} />
                    <Route path="/contact" element={<Contact />} />
                    <Route path="/cookie-policy" element={<CookiePolicy />} />
                    <Route path="/seller/guide" element={<SellerGuide />} />
                    <Route path="/seller/pricing" element={<SellerPricing />} />
                    <Route path="/seller/register" element={<SellerRegister />} />

                    <Route
                      path="/cart"
                      element={
                        <ProtectedRoute>
                          <Cart />
                        </ProtectedRoute>
                      }
                    />
                    <Route path="/wishlist" element={<Wishlist />} />
                    <Route path="/wishlist/share/:token" element={<SharedWishlist />} />
                    <Route
                      path="/checkout"
                      element={
                        <ProtectedRoute>
                          <Checkout />
                        </ProtectedRoute>
                      }
                    />
                    <Route
                      path="/loyalty"
                      element={
                        <ProtectedRoute>
                          <Loyalty />
                        </ProtectedRoute>
                      }
                    />
                    <Route
                      path="/notifications"
                      element={
                        <ProtectedRoute>
                          <Notifications />
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
                      path="/returns"
                      element={
                        <ProtectedRoute>
                          <Returns />
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
                    <Route
                      path="/account/credit-cards"
                      element={
                        <ProtectedRoute>
                          <CreditCards />
                        </ProtectedRoute>
                      }
                    />
                    <Route
                      path="/support"
                      element={
                        <ProtectedRoute>
                          <Support />
                        </ProtectedRoute>
                      }
                    />
                    <Route path="/help" element={<Help />} />
                  </Route>

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
              </Suspense>
            </ErrorBoundary>
            <Toaster position="top-right" richColors closeButton style={{ top: '80px' }} />
          </BrowserRouter>
        </DevToolsProvider>
      </ThemeProvider>
    </Provider>
  );
}

export default App;
