import { Button } from '@/components/common/button';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/common/dialog';
import { Input } from '@/components/common/input';

interface WishlistCreateCollectionDialogProps {
    open: boolean;
    title: string;
    description: string;
    placeholder: string;
    confirmLabel: string;
    name: string;
    isCreating: boolean;
    onOpenChange: (open: boolean) => void;
    onNameChange: (value: string) => void;
    onCreate: () => void;
}

export function WishlistCreateCollectionDialog({
    open,
    title,
    description,
    placeholder,
    confirmLabel,
    name,
    isCreating,
    onOpenChange,
    onNameChange,
    onCreate,
}: WishlistCreateCollectionDialogProps) {
    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="max-w-md">
                <DialogHeader>
                    <DialogTitle>{title}</DialogTitle>
                    <DialogDescription>{description}</DialogDescription>
                </DialogHeader>

                <Input
                    value={name}
                    onChange={(event) => onNameChange(event.target.value)}
                    onKeyDown={(event) => {
                        if (event.key === 'Enter') {
                            event.preventDefault();
                            onCreate();
                        }
                    }}
                    maxLength={80}
                    placeholder={placeholder}
                />

                <DialogFooter>
                    <Button variant="ghost" onClick={() => onOpenChange(false)}>
                        Vazgeç
                    </Button>
                    <Button onClick={onCreate} disabled={isCreating || name.trim().length < 2}>
                        {isCreating ? 'Oluşturuluyor...' : confirmLabel}
                    </Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
