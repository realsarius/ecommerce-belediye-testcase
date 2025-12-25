import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

const SITE_NAME = 'Belediye E-Ticaret';

const pageTitles: Record<string, string> = {
  '/': 'Ana Sayfa',
  '/login': 'Giriş Yap',
  '/register': 'Kayıt Ol',
  '/cart': 'Sepetim',
  '/checkout': 'Ödeme',
  '/orders': 'Siparişlerim',
  '/account': 'Hesabım',
  '/account/addresses': 'Adreslerim',
  '/help': 'Yardım',
  '/admin': 'Yönetim Paneli',
  '/admin/products': 'Ürün Yönetimi',
  '/admin/products/new': 'Yeni Ürün',
  '/admin/categories': 'Kategori Yönetimi',
  '/admin/orders': 'Sipariş Yönetimi',
  '/admin/coupons': 'Kupon Yönetimi',
  '/seller': 'Satıcı Paneli',
  '/seller/products': 'Ürünlerim',
  '/seller/products/new': 'Yeni Ürün Ekle',
  '/seller/profile': 'Satıcı Profili',
};

/**
 * Dynamically updates the document title based on current route.
 * Falls back to site name if route is not defined.
 */
export function usePageTitle(customTitle?: string) {
  const location = useLocation();

  useEffect(() => {
    let title: string;

    if (customTitle) {
      title = `${customTitle} | ${SITE_NAME}`;
    } else {
      // Check for exact match first
      const exactMatch = pageTitles[location.pathname];
      
      if (exactMatch) {
        title = `${exactMatch} | ${SITE_NAME}`;
      } else {
        // Check for dynamic routes (e.g., /products/:id, /orders/:id)
        const pathParts = location.pathname.split('/').filter(Boolean);
        
        if (pathParts[0] === 'products' && pathParts.length === 2) {
          title = `Ürün Detayı | ${SITE_NAME}`;
        } else if (pathParts[0] === 'orders' && pathParts.length === 2) {
          title = `Sipariş Detayı | ${SITE_NAME}`;
        } else if (pathParts[0] === 'admin' && pathParts[1] === 'products' && pathParts.length === 3) {
          title = `Ürün Düzenle | ${SITE_NAME}`;
        } else if (pathParts[0] === 'seller' && pathParts[1] === 'products' && pathParts.length === 3) {
          title = `Ürün Düzenle | ${SITE_NAME}`;
        } else {
          title = SITE_NAME;
        }
      }
    }

    document.title = title;
  }, [location.pathname, customTitle]);
}

export default usePageTitle;
