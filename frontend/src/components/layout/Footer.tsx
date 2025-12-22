import { Package } from 'lucide-react';

export function Footer() {
  return (
    <footer className="border-t bg-muted/50">
      <div className="container mx-auto px-4 py-8">
        <div className="flex flex-col md:flex-row justify-between items-center space-y-4 md:space-y-0">
          <div className="flex items-center space-x-2">
            <Package className="h-5 w-5 text-primary" />
            <span className="font-semibold">E-Ticaret</span>
          </div>
          <p className="text-sm text-muted-foreground">
            © {new Date().getFullYear()} E-Ticaret. Tüm hakları saklıdır.
          </p>
        </div>
      </div>
    </footer>
  );
}
