import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import { FilamentDto } from '../types/api';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Badge } from '../components/ui/badge';
import { Skeleton } from '../components/ui/skeleton';
import { Progress } from '../components/ui/progress';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { Package, ExternalLink, AlertCircle } from 'lucide-react';

export const Filaments = () => {
  const [filaments, setFilaments] = useState<FilamentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadFilaments();
  }, []);

  const loadFilaments = async () => {
    try {
      setLoading(true);
      const data = await api.getFilaments();
      setFilaments(data);
    } catch (err) {
      console.error('Error loading filaments:', err);
      setError('Failed to load filaments');
    } finally {
      setLoading(false);
    }
  };

  const inStockFilaments = filaments.filter(f => f.stockAmount > 0);
  const outOfStockFilaments = filaments.filter(f => f.stockAmount <= 0);

  const renderFilamentCard = (filament: FilamentDto) => (
    <Card key={filament.id} className={filament.stockAmount <= 0 ? 'opacity-60' : ''}>
      <CardContent className="pt-6">
        <div className="flex items-start gap-4">
          {filament.photoUrl && (
            <img
              src={filament.photoUrl}
              alt={filament.name}
              className="w-24 h-24 object-cover rounded-lg flex-shrink-0"
            />
          )}
          <div className="flex-1 min-w-0">
            <div className="flex items-start justify-between gap-2 mb-2">
              <div className="flex-1">
                <h3 className="text-lg font-semibold mb-1">{filament.name}</h3>
                <div className="flex flex-wrap gap-2 text-sm text-muted-foreground">
                  <span className="px-2 py-1 bg-muted rounded">{filament.material}</span>
                  <span className="px-2 py-1 bg-muted rounded">{filament.brand}</span>
                  <span className="px-2 py-1 bg-muted rounded">{filament.colour}</span>
                </div>
              </div>
              <div className="flex-shrink-0 min-w-[120px]">
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
                className="text-sm text-primary hover:underline flex items-center gap-1 mt-2"
              >
                <ExternalLink className="w-3 h-3" />
                View Product
              </a>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );

  if (loading) {
    return (
      <div className="space-y-6">
        <div className="text-center space-y-2">
          <Skeleton className="h-10 w-64 mx-auto" />
          <Skeleton className="h-4 w-96 mx-auto" />
        </div>
        <div className="grid gap-4">
          {[1, 2, 3].map((i) => (
            <Card key={i}>
              <CardContent className="pt-6">
                <div className="flex items-start gap-4">
                  <Skeleton className="w-24 h-24 rounded-lg flex-shrink-0" />
                  <div className="flex-1 space-y-3">
                    <div className="flex justify-between items-start">
                      <div className="space-y-2 flex-1">
                        <Skeleton className="h-6 w-48" />
                        <div className="flex gap-2">
                          <Skeleton className="h-6 w-16" />
                          <Skeleton className="h-6 w-16" />
                          <Skeleton className="h-6 w-16" />
                        </div>
                      </div>
                      <Skeleton className="h-6 w-24" />
                    </div>
                    <Skeleton className="h-4 w-32" />
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <p className="text-red-600 mb-4">{error}</p>
        <Button onClick={loadFilaments}>Try Again</Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="text-center space-y-2">
        <h1 className="text-3xl font-bold flex items-center justify-center gap-2">
          <Package className="w-8 h-8" />
          Available Filaments
        </h1>
        <p className="text-muted-foreground">
          Browse our filament inventory and check availability
        </p>
      </div>

      {filaments.length === 0 ? (
        <Card>
          <CardContent className="pt-6">
            <div className="text-center py-12">
              <Package className="w-12 h-12 mx-auto mb-4 text-muted-foreground" />
              <h3 className="text-lg font-semibold mb-2">No filaments available</h3>
              <p className="text-muted-foreground">
                Check back later for available filaments
              </p>
            </div>
          </CardContent>
        </Card>
      ) : (
        <Tabs defaultValue="all" className="space-y-4">
          <TabsList>
            <TabsTrigger value="all">All ({filaments.length})</TabsTrigger>
            <TabsTrigger value="in-stock">In Stock ({inStockFilaments.length})</TabsTrigger>
            <TabsTrigger value="out-of-stock">Out of Stock ({outOfStockFilaments.length})</TabsTrigger>
          </TabsList>

          <TabsContent value="all" className="space-y-4">
            {filaments.length === 0 ? (
              <Card>
                <CardContent className="pt-6">
                  <p className="text-center text-muted-foreground py-8">No filaments found</p>
                </CardContent>
              </Card>
            ) : (
              <div className="grid gap-4">
                {filaments.map(renderFilamentCard)}
              </div>
            )}
          </TabsContent>

          <TabsContent value="in-stock" className="space-y-4">
            {inStockFilaments.length === 0 ? (
              <Card>
                <CardContent className="pt-6">
                  <p className="text-center text-muted-foreground py-8">No filaments in stock</p>
                </CardContent>
              </Card>
            ) : (
              <div className="grid gap-4">
                {inStockFilaments.map(renderFilamentCard)}
              </div>
            )}
          </TabsContent>

          <TabsContent value="out-of-stock" className="space-y-4">
            {outOfStockFilaments.length === 0 ? (
              <Card>
                <CardContent className="pt-6">
                  <p className="text-center text-muted-foreground py-8">All filaments are in stock!</p>
                </CardContent>
              </Card>
            ) : (
              <div className="grid gap-4">
                {outOfStockFilaments.map(renderFilamentCard)}
              </div>
            )}
          </TabsContent>
        </Tabs>
      )}

      <Card className="bg-muted/50">
        <CardHeader>
          <CardTitle className="text-base">Ready to print?</CardTitle>
          <CardDescription>
            Select an in-stock filament when submitting your print request
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button asChild>
            <a href="/request/new">Submit Print Request</a>
          </Button>
        </CardContent>
      </Card>
    </div>
  );
};
