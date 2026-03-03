import { useEffect, useMemo, useState } from 'react';
import {
  closestCenter,
  DndContext,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core';
import {
  SortableContext,
  arrayMove,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import {
  ChevronDown,
  ChevronRight,
  FolderTree,
  GripVertical,
  Layers3,
  Plus,
  Save,
  Trash2,
} from 'lucide-react';
import { toast } from 'sonner';
import { ConfirmModal } from '@/components/admin/ConfirmModal';
import { EmptyState } from '@/components/admin/EmptyState';
import { StatusBadge } from '@/components/admin/StatusBadge';
import { Badge } from '@/components/common/badge';
import { Button } from '@/components/common/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/common/card';
import { Checkbox } from '@/components/common/checkbox';
import { Input } from '@/components/common/input';
import { Label } from '@/components/common/label';
import { ScrollArea } from '@/components/common/scroll-area';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/common/select';
import { Separator } from '@/components/common/separator';
import { Skeleton } from '@/components/common/skeleton';
import { Textarea } from '@/components/common/textarea';
import {
  useCreateCategoryMutation,
  useDeleteCategoryMutation,
  useGetAdminCategoriesQuery,
  useReorderCategoriesMutation,
  useUpdateCategoryMutation,
} from '@/features/admin/adminApi';
import type {
  Category,
  CreateCategoryRequest,
  UpdateCategoryRequest,
} from '@/features/products/types';

type CategoryTreeNode = Category & { children: CategoryTreeNode[] };
type FormMode = 'create' | 'edit';
type ParentValue = 'root' | `${number}`;

type CategoryFormState = {
  name: string;
  description: string;
  isActive: boolean;
  parentCategoryId: ParentValue;
};

const ROOT_PARENT = 'root';

function sortCategories(items: Category[]) {
  return [...items].sort((a, b) => {
    if (a.sortOrder !== b.sortOrder) {
      return a.sortOrder - b.sortOrder;
    }

    return a.name.localeCompare(b.name, 'tr');
  });
}

function buildCategoryTree(categories: Category[], parentCategoryId: number | null = null): CategoryTreeNode[] {
  return sortCategories(
    categories.filter((category) => (category.parentCategoryId ?? null) === parentCategoryId),
  ).map((category) => ({
    ...category,
    children: buildCategoryTree(categories, category.id),
  }));
}

function getParentValue(parentCategoryId?: number | null): ParentValue {
  return parentCategoryId ? `${parentCategoryId}` as ParentValue : ROOT_PARENT;
}

function createFormState(category?: Category, forcedParentId?: number | null): CategoryFormState {
  return {
    name: category?.name ?? '',
    description: category?.description ?? '',
    isActive: category?.isActive ?? true,
    parentCategoryId: getParentValue(forcedParentId ?? category?.parentCategoryId ?? null),
  };
}

function getParentCategoryId(value: ParentValue): number | null {
  return value === ROOT_PARENT ? null : Number(value);
}

function CategoryTreeBranch({
  nodes,
  selectedCategoryId,
  collapsedIds,
  onSelect,
  onToggleCollapse,
  onCreateChild,
}: {
  nodes: CategoryTreeNode[];
  selectedCategoryId: number | null;
  collapsedIds: Set<number>;
  onSelect: (categoryId: number) => void;
  onToggleCollapse: (categoryId: number) => void;
  onCreateChild: (categoryId: number) => void;
}) {
  if (nodes.length === 0) {
    return null;
  }

  return (
    <SortableContext items={nodes.map((node) => node.id)} strategy={verticalListSortingStrategy}>
      <div className="space-y-2">
        {nodes.map((node) => (
          <SortableCategoryTreeItem
            key={node.id}
            node={node}
            selectedCategoryId={selectedCategoryId}
            collapsedIds={collapsedIds}
            onSelect={onSelect}
            onToggleCollapse={onToggleCollapse}
            onCreateChild={onCreateChild}
          />
        ))}
      </div>
    </SortableContext>
  );
}

function SortableCategoryTreeItem({
  node,
  selectedCategoryId,
  collapsedIds,
  onSelect,
  onToggleCollapse,
  onCreateChild,
}: {
  node: CategoryTreeNode;
  selectedCategoryId: number | null;
  collapsedIds: Set<number>;
  onSelect: (categoryId: number) => void;
  onToggleCollapse: (categoryId: number) => void;
  onCreateChild: (categoryId: number) => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id: node.id });
  const isCollapsed = collapsedIds.has(node.id);
  const hasChildren = node.children.length > 0;

  return (
    <div
      ref={setNodeRef}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.7 : 1,
      }}
      className="space-y-2"
    >
      <div
        className={`rounded-xl border p-3 transition-colors ${
          selectedCategoryId === node.id
            ? 'border-primary/50 bg-primary/5'
            : 'border-border/60 bg-background hover:border-primary/30'
        }`}
      >
        <div className="flex items-start gap-3">
          <button
            type="button"
            className="mt-0.5 text-muted-foreground transition hover:text-foreground"
            onClick={() => (hasChildren ? onToggleCollapse(node.id) : undefined)}
          >
            {hasChildren ? (
              isCollapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />
            ) : (
              <span className="block h-4 w-4" />
            )}
          </button>
          <button
            type="button"
            className="mt-0.5 cursor-grab text-muted-foreground transition hover:text-foreground active:cursor-grabbing"
            aria-label="Kategoriyi sürükle"
            {...attributes}
            {...listeners}
          >
            <GripVertical className="h-4 w-4" />
          </button>
          <button type="button" className="min-w-0 flex-1 text-left" onClick={() => onSelect(node.id)}>
            <div className="flex items-center gap-2">
              <FolderTree className="h-4 w-4 text-muted-foreground" />
              <span className="truncate font-medium">{node.name}</span>
              <StatusBadge label={node.isActive ? 'Aktif' : 'Pasif'} tone={node.isActive ? 'success' : 'neutral'} />
            </div>
            <div className="mt-2 flex flex-wrap gap-2 text-xs text-muted-foreground">
              <span>{node.productCount} ürün</span>
              <span>{node.childCount} alt kategori</span>
              <span>#{node.sortOrder}</span>
            </div>
          </button>
          <Button variant="ghost" size="sm" onClick={() => onCreateChild(node.id)}>
            <Plus className="mr-1 h-4 w-4" />
            Alt
          </Button>
        </div>
      </div>

      {hasChildren && !isCollapsed ? (
        <div className="ml-8 border-l border-border/60 pl-4">
          <CategoryTreeBranch
            nodes={node.children}
            selectedCategoryId={selectedCategoryId}
            collapsedIds={collapsedIds}
            onSelect={onSelect}
            onToggleCollapse={onToggleCollapse}
            onCreateChild={onCreateChild}
          />
        </div>
      ) : null}
    </div>
  );
}

export default function AdminCategories() {
  const { data: categories = [], isLoading } = useGetAdminCategoriesQuery();
  const [createCategory, { isLoading: isCreating }] = useCreateCategoryMutation();
  const [updateCategory, { isLoading: isUpdating }] = useUpdateCategoryMutation();
  const [deleteCategory, { isLoading: isDeleting }] = useDeleteCategoryMutation();
  const [reorderCategories, { isLoading: isReordering }] = useReorderCategoriesMutation();

  const [mode, setMode] = useState<FormMode>('edit');
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null);
  const [formState, setFormState] = useState<CategoryFormState>(() => createFormState());
  const [deleteTarget, setDeleteTarget] = useState<Category | null>(null);
  const [collapsedIds, setCollapsedIds] = useState<Set<number>>(new Set());

  const categoryTree = useMemo(() => buildCategoryTree(categories), [categories]);
  const selectedCategory = useMemo(
    () => categories.find((category) => category.id === selectedCategoryId) ?? null,
    [categories, selectedCategoryId],
  );

  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  useEffect(() => {
    if (categories.length === 0) {
      setMode('create');
      setSelectedCategoryId(null);
      setFormState(createFormState());
      return;
    }

    if (mode === 'create') {
      return;
    }

    if (!selectedCategoryId && categories.length > 0) {
      setSelectedCategoryId(categories[0].id);
      setMode('edit');
      setFormState(createFormState(categories[0]));
      return;
    }

    if (!selectedCategoryId) {
      return;
    }

    const nextSelectedCategory = categories.find((category) => category.id === selectedCategoryId);
    if (!nextSelectedCategory) {
      setSelectedCategoryId(null);
      setMode('create');
      setFormState(createFormState());
      return;
    }

    if (mode === 'edit') {
      setFormState(createFormState(nextSelectedCategory));
    }
  }, [categories, mode, selectedCategoryId]);

  const openRootCreate = () => {
    setMode('create');
    setSelectedCategoryId(null);
    setFormState(createFormState(undefined, null));
  };

  const openChildCreate = (parentCategoryId: number) => {
    setMode('create');
    setSelectedCategoryId(parentCategoryId);
    setFormState(createFormState(undefined, parentCategoryId));
  };

  const openEdit = (categoryId: number) => {
    const category = categories.find((item) => item.id === categoryId);
    if (!category) {
      return;
    }

    setMode('edit');
    setSelectedCategoryId(category.id);
    setFormState(createFormState(category));
  };

  const handleToggleCollapse = (categoryId: number) => {
    setCollapsedIds((current) => {
      const next = new Set(current);
      if (next.has(categoryId)) {
        next.delete(categoryId);
      } else {
        next.add(categoryId);
      }

      return next;
    });
  };

  const handleSubmit = async () => {
    if (!formState.name.trim()) {
      toast.error('Kategori adı gereklidir.');
      return;
    }

    const payload: CreateCategoryRequest | UpdateCategoryRequest = {
      name: formState.name.trim(),
      description: formState.description.trim() || undefined,
      isActive: formState.isActive,
      parentCategoryId: getParentCategoryId(formState.parentCategoryId),
    };

    try {
      if (mode === 'create') {
        const created = await createCategory(payload as CreateCategoryRequest).unwrap();
        toast.success('Kategori oluşturuldu.');
        setMode('edit');
        setSelectedCategoryId(created.id);
      } else if (selectedCategoryId) {
        await updateCategory({ id: selectedCategoryId, data: payload as UpdateCategoryRequest }).unwrap();
        toast.success('Kategori güncellendi.');
      }
    } catch {
      toast.error(mode === 'create' ? 'Kategori oluşturulamadı.' : 'Kategori güncellenemedi.');
    }
  };

  const handleDelete = async () => {
    if (!deleteTarget) {
      return;
    }

    try {
      await deleteCategory(deleteTarget.id).unwrap();
      toast.success('Kategori pasife alındı.');
      if (selectedCategoryId === deleteTarget.id) {
        openRootCreate();
      }
      setDeleteTarget(null);
    } catch {
      toast.error('Kategori silinemedi.');
    }
  };

  const handleDragEnd = async ({ active, over }: DragEndEvent) => {
    if (!over || active.id === over.id) {
      return;
    }

    const activeCategory = categories.find((category) => category.id === Number(active.id));
    const overCategory = categories.find((category) => category.id === Number(over.id));

    if (!activeCategory || !overCategory) {
      return;
    }

    const activeParentId = activeCategory.parentCategoryId ?? null;
    const overParentId = overCategory.parentCategoryId ?? null;

    if (activeParentId !== overParentId) {
      toast.info('Sürükle bırak şu an aynı seviye içindeki kategorileri sıralar. Üst kategori değişimini sağ panelden yapabilirsiniz.');
      return;
    }

    const siblings = sortCategories(categories.filter((category) => (category.parentCategoryId ?? null) === activeParentId));
    const oldIndex = siblings.findIndex((category) => category.id === activeCategory.id);
    const newIndex = siblings.findIndex((category) => category.id === overCategory.id);

    if (oldIndex < 0 || newIndex < 0 || oldIndex === newIndex) {
      return;
    }

    const reordered = arrayMove(siblings, oldIndex, newIndex).map((category, index) => ({
      id: category.id,
      parentCategoryId: activeParentId,
      sortOrder: index,
    }));

    try {
      await reorderCategories({ items: reordered }).unwrap();
      toast.success('Kategori sıralaması güncellendi.');
    } catch {
      toast.error('Kategori sıralaması güncellenemedi.');
    }
  };

  const selectedCategoryCanDelete = selectedCategory
    ? selectedCategory.productCount === 0 && selectedCategory.childCount === 0
    : false;

  const availableParents = useMemo(() => {
    if (mode !== 'edit' || !selectedCategoryId) {
      return categories;
    }

    return categories.filter((category) => category.id !== selectedCategoryId);
  }, [categories, mode, selectedCategoryId]);

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-48" />
        <div className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
          <Skeleton className="h-[620px] rounded-2xl" />
          <Skeleton className="h-[620px] rounded-2xl" />
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
        <div className="space-y-2">
          <h1 className="text-3xl font-bold tracking-tight">Kategoriler</h1>
          <p className="max-w-3xl text-muted-foreground">
            Kategori ağacını yönetin, sürükle bırak ile aynı seviye sıralamasını güncelleyin ve seçili kategoriyi sağ panelden düzenleyin.
          </p>
        </div>
        <Button onClick={openRootCreate}>
          <Plus className="mr-2 h-4 w-4" />
          Yeni Ana Kategori
        </Button>
      </div>

      <div className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
        <Card className="border-border/70">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Layers3 className="h-5 w-5 text-primary" />
              Kategori Ağacı
            </CardTitle>
            <CardDescription>
              Aynı seviye içindeki kategorileri sürükleyerek yeniden sıralayabilirsiniz.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex flex-wrap gap-2">
              <Badge variant="secondary">{categories.length} kategori</Badge>
              <Badge variant="secondary">
                {categories.filter((category) => !category.parentCategoryId).length} ana kategori
              </Badge>
            </div>

            <Separator />

            <ScrollArea className="h-[560px] pr-4">
              {categoryTree.length === 0 ? (
                <EmptyState
                  icon={FolderTree}
                  title="Henüz kategori yok"
                  description="İlk ana kategoriyi oluşturduğunuzda hiyerarşi ve sıralama ağacı bu alanda görünecek."
                  className="border-dashed shadow-none"
                />
              ) : (
                <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={(event) => void handleDragEnd(event)}>
                  <CategoryTreeBranch
                    nodes={categoryTree}
                    selectedCategoryId={selectedCategoryId}
                    collapsedIds={collapsedIds}
                    onSelect={openEdit}
                    onToggleCollapse={handleToggleCollapse}
                    onCreateChild={openChildCreate}
                  />
                </DndContext>
              )}
            </ScrollArea>
          </CardContent>
        </Card>

        <Card className="border-border/70">
          <CardHeader>
            <CardTitle>{mode === 'create' ? 'Yeni Kategori' : 'Kategori Düzenle'}</CardTitle>
            <CardDescription>
              {mode === 'create'
                ? 'Yeni kategori oluşturabilir veya seçili kategori altında alt kategori ekleyebilirsiniz.'
                : 'Seçili kategorinin adını, açıklamasını, üst kategorisini ve durumunu güncelleyin.'}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="category-name">Kategori Adı</Label>
                <Input
                  id="category-name"
                  placeholder="Elektronik"
                  value={formState.name}
                  onChange={(event) => setFormState((current) => ({ ...current, name: event.target.value }))}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="category-parent">Üst Kategori</Label>
                <Select
                  value={formState.parentCategoryId}
                  onValueChange={(value) => setFormState((current) => ({ ...current, parentCategoryId: value as ParentValue }))}
                >
                  <SelectTrigger id="category-parent">
                    <SelectValue placeholder="Ana kategori" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={ROOT_PARENT}>Ana kategori</SelectItem>
                    {availableParents.map((category) => (
                      <SelectItem key={category.id} value={`${category.id}`}>
                        {category.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="category-description">Açıklama</Label>
              <Textarea
                id="category-description"
                rows={5}
                placeholder="Kategori hakkında kısa bir açıklama girin."
                value={formState.description}
                onChange={(event) => setFormState((current) => ({ ...current, description: event.target.value }))}
              />
            </div>

            <div className="flex items-center gap-3 rounded-xl border bg-muted/20 px-4 py-3">
              <Checkbox
                id="category-active"
                checked={formState.isActive}
                onCheckedChange={(checked) => setFormState((current) => ({ ...current, isActive: !!checked }))}
              />
              <Label htmlFor="category-active" className="cursor-pointer">
                Kategori aktif olarak listelensin
              </Label>
            </div>

            {selectedCategory ? (
              <div className="rounded-2xl border bg-muted/20 p-4">
                <div className="flex flex-wrap items-center gap-2">
                  <Badge variant="secondary">{selectedCategory.productCount} ürün</Badge>
                  <Badge variant="secondary">{selectedCategory.childCount} alt kategori</Badge>
                  <Badge variant="secondary">Sıra #{selectedCategory.sortOrder}</Badge>
                  <StatusBadge
                    label={selectedCategory.isActive ? 'Aktif' : 'Pasif'}
                    tone={selectedCategory.isActive ? 'success' : 'neutral'}
                  />
                </div>
                <p className="mt-3 text-sm text-muted-foreground">
                  Ürün bağlı kategori silinemez. Alt kategori varsa önce onları taşımanız veya pasife almanız gerekir.
                </p>
              </div>
            ) : null}

            <div className="flex flex-wrap gap-3">
              <Button onClick={() => void handleSubmit()} disabled={isCreating || isUpdating || isReordering}>
                <Save className="mr-2 h-4 w-4" />
                {mode === 'create' ? 'Kategoriyi Oluştur' : 'Değişiklikleri Kaydet'}
              </Button>
              {mode === 'edit' && selectedCategory ? (
                <>
                  <Button variant="outline" onClick={() => openChildCreate(selectedCategory.id)}>
                    <Plus className="mr-2 h-4 w-4" />
                    Alt Kategori Ekle
                  </Button>
                  <Button
                    variant="destructive"
                    disabled={!selectedCategoryCanDelete || isDeleting}
                    onClick={() => setDeleteTarget(selectedCategory)}
                  >
                    <Trash2 className="mr-2 h-4 w-4" />
                    Kategoriyi Sil
                  </Button>
                </>
              ) : null}
            </div>
          </CardContent>
        </Card>
      </div>

      <ConfirmModal
        open={!!deleteTarget}
        onOpenChange={(open) => (!open ? setDeleteTarget(null) : undefined)}
        title="Kategoriyi sil"
        description={
          deleteTarget
            ? `"${deleteTarget.name}" kategorisi pasife alınacak. Bu işlem yalnızca bağlı ürün ve alt kategori yoksa devam eder.`
            : ''
        }
        confirmLabel="Sil"
        isLoading={isDeleting}
        onConfirm={() => void handleDelete()}
      />
    </div>
  );
}
