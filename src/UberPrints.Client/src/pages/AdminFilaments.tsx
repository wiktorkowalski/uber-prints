import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import { FilamentDto, CreateFilamentDto, UpdateFilamentDto } from '../types/api';
import { useToast } from '../hooks/use-toast';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../components/ui/dialog';
import { Label } from '../components/ui/label';
import { Input } from '../components/ui/input';
import { Badge } from '../components/ui/badge';
import { Skeleton } from '../components/ui/skeleton';
import { Progress } from '../components/ui/progress';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '../components/ui/alert-dialog';
import { Package, Loader2, ExternalLink, Edit2, Plus, Trash2, AlertCircle, Shield } from 'lucide-react';

export const AdminFilaments = () => {
  const { toast } = useToast();

  const [filaments, setFilaments] = useState<FilamentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [filamentDialogOpen, setFilamentDialogOpen] = useState(false);
  const [editingFilament, setEditingFilament] = useState<FilamentDto | null>(null);
  const [filamentFormData, setFilamentFormData] = useState<CreateFilamentDto>({
    name: '',
    material: '',
    brand: '',
    colour: '',
    stockAmount: 0,
    stockUnit: 'g',
    link: '',
    photoUrl: '',
    isAvailable: true,
  });
  const [filamentSubmitting, setFilamentSubmitting] = useState(false);
  const [deleteFilamentDialogOpen, setDeleteFilamentDialogOpen] = useState(false);
  const [filamentToDelete, setFilamentToDelete] = useState<FilamentDto | null>(null);
  const [deletingFilament, setDeletingFilament] = useState(false);

  useEffect(() => {
    loadFilaments();
  }, []);

  const loadFilaments = async () => {
    try {
      setLoading(true);
      const data = await api.getFilaments();
      setFilaments(data);
    } catch (err) {
      toast({
        title: "Failed to load filaments",
        description: "Could not load filament inventory",
        variant: "destructive",
      });
    } finally {
      setLoading(false);
    }
  };

  const openCreateFilamentDialog = () => {
    setEditingFilament(null);
    setFilamentFormData({
      name: '',
      material: '',
      brand: '',
      colour: '',
      stockAmount: 0,
      stockUnit: 'g',
      link: '',
      photoUrl: '',
      isAvailable: true,
    });
    setFilamentDialogOpen(true);
  };

  const openEditFilamentDialog = (filament: FilamentDto) => {
    setEditingFilament(filament);
    setFilamentFormData({
      name: filament.name,
      material: filament.material,
      brand: filament.brand,
      colour: filament.colour,
      stockAmount: filament.stockAmount,
      stockUnit: filament.stockUnit,
      link: filament.link || '',
      photoUrl: filament.photoUrl || '',
      isAvailable: filament.isAvailable,
    });
    setFilamentDialogOpen(true);
  };

  const handleFilamentSubmit = async () => {
    try {
      setFilamentSubmitting(true);
      if (editingFilament) {
        await api.updateFilament(editingFilament.id, filamentFormData as UpdateFilamentDto);
        toast({
          title: "Filament updated",
          description: "Filament has been updated successfully",
          variant: "success",
        });
      } else {
        await api.createFilament(filamentFormData);
        toast({
          title: "Filament created",
          description: "New filament has been added to inventory",
          variant: "success",
        });
      }
      setFilamentDialogOpen(false);
      await loadFilaments();
    } catch (err: any) {
      console.error('Error saving filament:', err);
      toast({
        title: "Failed to save filament",
        description: err.response?.data?.message || 'Could not save filament',
        variant: "destructive",
      });
    } finally {
      setFilamentSubmitting(false);
    }
  };

  const handleDeleteFilament = async () => {
    if (!filamentToDelete) return;

    try {
      setDeletingFilament(true);
      await api.deleteFilament(filamentToDelete.id);
      toast({
        title: "Filament deleted",
        description: "Filament has been removed from inventory",
        variant: "success",
      });
      await loadFilaments();
    } catch (err: any) {
      console.error('Error deleting filament:', err);
      toast({
        title: "Failed to delete filament",
        description: err.response?.data?.message || 'Could not delete filament',
        variant: "destructive",
      });
    } finally {
      setDeletingFilament(false);
      setDeleteFilamentDialogOpen(false);
      setFilamentToDelete(null);
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <div>
          <Skeleton className="h-10 w-64" />
          <Skeleton className="h-4 w-96 mt-1" />
        </div>
        <Card>
          <CardContent className="pt-6">
            <div className="space-y-4">
              {[1, 2, 3].map((i) => (
                <div key={i} className="border rounded-lg p-4 flex items-start gap-4">
                  <Skeleton className="w-20 h-20 rounded" />
                  <div className="flex-1 space-y-2">
                    <Skeleton className="h-6 w-48" />
                    <Skeleton className="h-4 w-64" />
                  </div>
                  <div className="flex gap-2">
                    <Skeleton className="h-9 w-20" />
                    <Skeleton className="h-9 w-9" />
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold flex items-center gap-2">
          <Shield className="w-8 h-8" />
          Manage Filaments
        </h1>
        <p className="text-muted-foreground mt-1">
          Manage your filament inventory and stock levels
        </p>
      </div>

      {/* Filament Management Card */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Filament Inventory</CardTitle>
              <CardDescription>View and manage all filaments</CardDescription>
            </div>
            <Button onClick={openCreateFilamentDialog}>
              <Plus className="w-4 h-4 mr-2" />
              Add Filament
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {filaments.length === 0 ? (
            <div className="text-center py-12">
              <Package className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
              <h3 className="text-lg font-semibold mb-2">No filaments yet</h3>
              <p className="text-muted-foreground mb-4">
                Add filaments to your inventory to start accepting print requests
              </p>
              <Button onClick={openCreateFilamentDialog}>
                <Plus className="w-4 h-4 mr-2" />
                Add First Filament
              </Button>
            </div>
          ) : (
            <div className="grid gap-4">
              {filaments.map((filament) => (
                <div
                  key={filament.id}
                  className="border rounded-lg p-4 flex items-start gap-4"
                >
                  {filament.photoUrl && (
                    <img
                      src={filament.photoUrl}
                      alt={filament.name}
                      className="w-20 h-20 object-cover rounded"
                    />
                  )}
                  <div className="flex-1">
                    <div className="flex items-start justify-between mb-2">
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <h3 className="text-lg font-semibold">{filament.name}</h3>
                          {!filament.isAvailable && (
                            <Badge variant="secondary" className="text-xs">
                              Hidden
                            </Badge>
                          )}
                        </div>
                        <p className="text-sm text-muted-foreground">
                          {filament.brand} • {filament.material} • {filament.colour}
                        </p>
                      </div>
                      <div className="min-w-[140px]">
                        {filament.stockAmount <= 0 ? (
                          <Badge variant="destructive" className="flex items-center gap-1">
                            <AlertCircle className="w-3 h-3" />
                            Out of Stock
                          </Badge>
                        ) : (
                          <div className="space-y-1">
                            <div className="flex justify-between text-xs">
                              <span className="text-muted-foreground">Stock</span>
                              <span className="font-medium">{filament.stockAmount} {filament.stockUnit}</span>
                            </div>
                            <Progress
                              value={Math.min((filament.stockAmount / 1000) * 100, 100)}
                              className="h-2"
                            />
                          </div>
                        )}
                      </div>
                    </div>
                    {filament.link && (
                      <a
                        href={filament.link}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-sm text-primary hover:underline flex items-center gap-1"
                      >
                        <ExternalLink className="w-3 h-3" />
                        Product Link
                      </a>
                    )}
                  </div>
                  <div className="flex gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => openEditFilamentDialog(filament)}
                    >
                      <Edit2 className="w-4 h-4 mr-2" />
                      Edit
                    </Button>
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => {
                        setFilamentToDelete(filament);
                        setDeleteFilamentDialogOpen(true);
                      }}
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Filament Create/Edit Dialog */}
      <Dialog open={filamentDialogOpen} onOpenChange={setFilamentDialogOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{editingFilament ? 'Edit Filament' : 'Add New Filament'}</DialogTitle>
            <DialogDescription>
              {editingFilament
                ? 'Update filament details and stock levels'
                : 'Add a new filament to your inventory'}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="name">Name *</Label>
                <Input
                  id="name"
                  value={filamentFormData.name}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, name: e.target.value })}
                  placeholder="e.g., PLA Black 1kg"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="material">Material *</Label>
                <Input
                  id="material"
                  value={filamentFormData.material}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, material: e.target.value })}
                  placeholder="e.g., PLA, PETG, ABS"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="brand">Brand *</Label>
                <Input
                  id="brand"
                  value={filamentFormData.brand}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, brand: e.target.value })}
                  placeholder="e.g., Prusa, eSun"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="colour">Colour *</Label>
                <Input
                  id="colour"
                  value={filamentFormData.colour}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, colour: e.target.value })}
                  placeholder="e.g., Black, Red, White"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="stockAmount">Stock Amount *</Label>
                <Input
                  id="stockAmount"
                  type="number"
                  min="0"
                  value={filamentFormData.stockAmount}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, stockAmount: parseFloat(e.target.value) || 0 })}
                  placeholder="0"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="stockUnit">Stock Unit *</Label>
                <Input
                  id="stockUnit"
                  value={filamentFormData.stockUnit}
                  onChange={(e) => setFilamentFormData({ ...filamentFormData, stockUnit: e.target.value })}
                  placeholder="g, kg, m"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="link">Product Link (Optional)</Label>
              <Input
                id="link"
                type="url"
                value={filamentFormData.link}
                onChange={(e) => setFilamentFormData({ ...filamentFormData, link: e.target.value })}
                placeholder="https://..."
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="photoUrl">Photo URL (Optional)</Label>
              <Input
                id="photoUrl"
                type="url"
                value={filamentFormData.photoUrl}
                onChange={(e) => setFilamentFormData({ ...filamentFormData, photoUrl: e.target.value })}
                placeholder="https://..."
              />
            </div>
          </div>

          <div className="flex items-center space-x-2 pt-2">
            <input
              type="checkbox"
              id="isAvailable"
              checked={filamentFormData.isAvailable ?? true}
              onChange={(e) => setFilamentFormData({ ...filamentFormData, isAvailable: e.target.checked })}
              className="w-4 h-4 rounded border-gray-300 text-primary focus:ring-primary"
            />
            <Label htmlFor="isAvailable" className="text-sm font-medium cursor-pointer">
              Available for selection in print requests
            </Label>
          </div>
          <p className="text-xs text-muted-foreground">
            When enabled, users can select this filament when creating print requests. Disable to hide from selection (e.g., for discontinued or special-use filaments).
          </p>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setFilamentDialogOpen(false)}
              disabled={filamentSubmitting}
            >
              Cancel
            </Button>
            <Button
              onClick={handleFilamentSubmit}
              disabled={
                filamentSubmitting ||
                !filamentFormData.name ||
                !filamentFormData.material ||
                !filamentFormData.brand ||
                !filamentFormData.colour ||
                !filamentFormData.stockUnit
              }
            >
              {filamentSubmitting && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
              {editingFilament ? 'Update Filament' : 'Add Filament'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Filament Confirmation Dialog */}
      <AlertDialog open={deleteFilamentDialogOpen} onOpenChange={setDeleteFilamentDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Filament</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete {filamentToDelete?.name}? This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deletingFilament}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteFilament}
              disabled={deletingFilament}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {deletingFilament ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Deleting...
                </>
              ) : (
                'Delete'
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
};
