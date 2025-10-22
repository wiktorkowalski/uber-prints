import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { api } from '../lib/api';
import { FilamentRequestDto, FilamentRequestStatusEnum } from '../types/api';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '../components/ui/form';
import { Input } from '../components/ui/input';
import { Textarea } from '../components/ui/textarea';
import { LoadingSpinner } from '../components/ui/loading-spinner';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../hooks/use-toast';
import { Loader2, Plus, Trash2 } from 'lucide-react';
import { Badge } from '../components/ui/badge';
import { getDisplayName } from '../lib/utils';

const formSchema = z.object({
  requesterName: z.string().min(1, 'Name is required').max(100, 'Name must be less than 100 characters'),
  material: z.string().min(1, 'Material is required').max(50, 'Material must be less than 50 characters'),
  brand: z.string().min(1, 'Brand is required').max(100, 'Brand must be less than 100 characters'),
  colour: z.string().min(1, 'Colour is required').max(50, 'Colour must be less than 50 characters'),
  link: z.union([
    z.string().url('Must be a valid URL').max(500, 'URL must be less than 500 characters'),
    z.literal('')
  ]).optional(),
  notes: z.string().max(1000, 'Notes must be less than 1000 characters').optional(),
});

type FormValues = z.infer<typeof formSchema>;

const getStatusColor = (status: FilamentRequestStatusEnum) => {
  switch (status) {
    case FilamentRequestStatusEnum.Pending:
      return 'bg-yellow-500';
    case FilamentRequestStatusEnum.Approved:
      return 'bg-green-500';
    case FilamentRequestStatusEnum.Rejected:
      return 'bg-red-500';
    case FilamentRequestStatusEnum.Ordered:
      return 'bg-blue-500';
    case FilamentRequestStatusEnum.Received:
      return 'bg-purple-500';
    default:
      return 'bg-gray-500';
  }
};

const getStatusLabel = (status: FilamentRequestStatusEnum) => {
  return FilamentRequestStatusEnum[status];
};

export const FilamentRequests = () => {
  const { user } = useAuth();
  const { toast } = useToast();
  const [requests, setRequests] = useState<FilamentRequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [showForm, setShowForm] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      requesterName: user?.username || '',
      material: '',
      brand: '',
      colour: '',
      link: '',
      notes: '',
    },
  });

  useEffect(() => {
    loadRequests();
  }, []);

  useEffect(() => {
    if (user) {
      form.setValue('requesterName', getDisplayName(user));
    }
  }, [user, form]);

  const loadRequests = async () => {
    try {
      const data = await api.getMyFilamentRequests();
      setRequests(data);
    } catch (error) {
      console.error('Error loading filament requests:', error);
      toast({
        title: 'Failed to load filament requests',
        description: 'Please try again later.',
        variant: 'destructive',
      });
    } finally {
      setLoading(false);
    }
  };

  const onSubmit = async (values: FormValues) => {
    try {
      setSubmitting(true);

      await api.createFilamentRequest({
        requesterName: values.requesterName,
        material: values.material,
        brand: values.brand,
        colour: values.colour,
        link: values.link || undefined,
        notes: values.notes || undefined,
      });

      toast({
        title: 'Request submitted successfully!',
        description: 'Your filament request has been created.',
        variant: 'success',
      });

      form.reset({
        requesterName: user?.username || '',
        material: '',
        brand: '',
        colour: '',
        link: '',
        notes: '',
      });
      setShowForm(false);
      loadRequests();
    } catch (error: any) {
      console.error('Error creating filament request:', error);
      const errorMessage = error.response?.data?.message || 'Failed to submit request. Please try again.';
      toast({
        title: 'Failed to submit request',
        description: errorMessage,
        variant: 'destructive',
      });
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this filament request?')) {
      return;
    }

    try {
      await api.deleteFilamentRequest(id);
      toast({
        title: 'Request deleted',
        description: 'Filament request has been deleted successfully.',
        variant: 'success',
      });
      loadRequests();
    } catch (error) {
      console.error('Error deleting filament request:', error);
      toast({
        title: 'Failed to delete request',
        description: 'Please try again later.',
        variant: 'destructive',
      });
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <LoadingSpinner className="w-8 h-8" />
      </div>
    );
  }

  return (
    <div className="container mx-auto py-8 px-4">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Filament Requests</h1>
        <Button onClick={() => setShowForm(!showForm)}>
          <Plus className="mr-2 h-4 w-4" />
          {showForm ? 'Hide Form' : 'Request Filament'}
        </Button>
      </div>

      {showForm && (
        <Card className="mb-8">
          <CardHeader>
            <CardTitle>Request a New Filament</CardTitle>
            <CardDescription>
              Request a filament that you'd like to be added to the inventory
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Form {...form}>
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                <FormField
                  control={form.control}
                  name="requesterName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Your Name</FormLabel>
                      <FormControl>
                        <Input {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="material"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Material</FormLabel>
                        <FormControl>
                          <Input placeholder="e.g., PLA, PETG, ABS" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name="colour"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Colour</FormLabel>
                        <FormControl>
                          <Input placeholder="e.g., Black, White" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>

                <FormField
                  control={form.control}
                  name="brand"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Brand</FormLabel>
                      <FormControl>
                        <Input placeholder="e.g., Prusament, Hatchbox" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="link"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Product Link (Optional)</FormLabel>
                      <FormControl>
                        <Input placeholder="https://..." {...field} />
                      </FormControl>
                      <FormDescription>
                        Link to where the filament can be purchased
                      </FormDescription>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <FormField
                  control={form.control}
                  name="notes"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Notes (Optional)</FormLabel>
                      <FormControl>
                        <Textarea
                          placeholder="Any additional information..."
                          className="resize-none"
                          rows={3}
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />

                <Button type="submit" disabled={submitting}>
                  {submitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Submit Request
                </Button>
              </form>
            </Form>
          </CardContent>
        </Card>
      )}

      <div className="space-y-4">
        <h2 className="text-2xl font-semibold">Your Requests</h2>
        {requests.length === 0 ? (
          <Card>
            <CardContent className="py-8 text-center text-muted-foreground">
              No filament requests yet. Click "Request Filament" to create one.
            </CardContent>
          </Card>
        ) : (
          requests.map((request) => (
            <Card key={request.id}>
              <CardHeader>
                <div className="flex justify-between items-start">
                  <div>
                    <CardTitle>
                      {request.brand} - {request.material} ({request.colour})
                    </CardTitle>
                    <CardDescription>
                      Requested on {new Date(request.createdAt).toLocaleDateString()}
                    </CardDescription>
                  </div>
                  <div className="flex items-center gap-2">
                    <Badge className={getStatusColor(request.currentStatus)}>
                      {getStatusLabel(request.currentStatus)}
                    </Badge>
                    {request.currentStatus === FilamentRequestStatusEnum.Pending && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => handleDelete(request.id)}
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                {request.link && (
                  <p className="text-sm mb-2">
                    <a
                      href={request.link}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-blue-500 hover:underline"
                    >
                      Product Link
                    </a>
                  </p>
                )}
                {request.notes && (
                  <p className="text-sm text-muted-foreground">{request.notes}</p>
                )}
                {request.filamentName && (
                  <p className="text-sm mt-2 text-green-600">
                    Linked to filament: {request.filamentName}
                  </p>
                )}
              </CardContent>
            </Card>
          ))
        )}
      </div>
    </div>
  );
};
