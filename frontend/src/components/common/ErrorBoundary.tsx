import { Component } from 'react';
import type { ErrorInfo, ReactNode } from 'react';

interface Props {
    children: ReactNode;
    fallback?: ReactNode;
}

interface State {
    hasError: boolean;
}

export class ErrorBoundary extends Component<Props, State> {
    constructor(props: Props) {
        super(props);
        this.state = { hasError: false };
    }

    static getDerivedStateFromError(): State {
        return { hasError: true };
    }

    componentDidCatch(error: Error, errorInfo: ErrorInfo) {
        console.error('[ErrorBoundary] Yakalanan hata:', error);
        console.error('[ErrorBoundary] Bileşen yığını:', errorInfo.componentStack);
    }

    private handleReload = () => {
        window.location.reload();
    };

    render() {
        if (this.state.hasError) {
            if (this.props.fallback) {
                return this.props.fallback;
            }

            return (
                <div className="flex flex-col items-center justify-center min-h-screen gap-4 p-8 text-center">
                    <div className="text-6xl">⚠️</div>
                    <h1 className="text-2xl font-bold text-foreground">
                        Bir şeyler ters gitti
                    </h1>
                    <p className="text-muted-foreground max-w-md">
                        Beklenmedik bir hata oluştu. Lütfen sayfayı yenilemeyi deneyin.
                    </p>
                    <button
                        onClick={this.handleReload}
                        className="mt-2 px-6 py-2.5 rounded-md bg-primary text-primary-foreground font-medium hover:bg-primary/90 transition-colors"
                    >
                        Sayfayı Yenile
                    </button>
                </div>
            );
        }

        return this.props.children;
    }
}
