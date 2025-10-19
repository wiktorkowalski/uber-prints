import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { api } from '../lib/api';
import { FilamentDto, PrintRequestDto } from '../types/api';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '../components/ui/form';
import { Input } from '../components/ui/input';
import { Textarea } from '../components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../components/ui/select';
import { Checkbox } from '../components/ui/checkbox';
import { LoadingSpinner } from '../components/ui/loading-spinner';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../hooks/use-toast';
import { ArrowLeft, Loader2, Edit2 } from 'lucide-react';

const formSchema = z.object({
  requesterName: z.string().min(1, 'Name is required').max(100, 'Name must be less than 100 characters'),
  modelUrl: z.string().url('Must be a valid URL').max(500, 'URL must be less than 500 characters'),
  notes: z.string().max(1000, 'Notes must be less than 1000 characters').optional().or(z.literal('')),
  requestDelivery: z.boolean(),
  isPublic: z.boolean(),
  filamentId: z.string().min(1, 'Please select a filament'),
});

type FormValues = z.infer<typeof formSchema>;

export const EditRequest = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user, isAuthenticated } = useAuth();
  const { toast } = useToast();
  const [request, setRequest] = useState<PrintRequestDto | null>(null);
  const [filaments, setFilaments] = useState<FilamentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      requesterName: '',
      modelUrl: '',
      notes: '',
      requestDelivery: false,
      isPublic: true,
      filamentId: '',
    },
  });

  useEffect(() => {
    loadData();
  }, [id]);

  const loadData = async () => {
    if (!id) return;

    try {
      setLoading(true);
      const [requestData, filamentsData] = await Promise.all([
        api.getRequest(id),
        api.getFilaments(true), // Only get in-stock filaments
      ]);

      // Check if user owns this request
      if (user && requestData.userId !== user.id) {
        toast({
          title: "Unauthorized",
          description: "You can only edit your own requests",
          variant: "destructive",
        });
        navigate(`/request/${id}`);
        return;
      }

      setRequest(requestData);
      setFilaments(filamentsData);

      // Set form values
      form.reset({
        requesterName: requestData.requesterName,
        modelUrl: requestData.modelUrl,
        notes: requestData.notes || '',
        requestDelivery: requestData.requestDelivery,
        isPublic: requestData.isPublic,
        filamentId: requestData.filamentId,
      });
    } catch (error: any) {
      console.error('Error loading data:', error);
      toast({
        title: "Failed to load request",
        description: error.response?.status === 404 ? 'Request not found' : 'Failed to load request',
        variant: "destructive",
      });
      navigate('/requests');
    } finally {
      setLoading(false);
    }
  };

  const onSubmit = async (values: FormValues) => {
    if (!id) return;

    try {
      setSubmitting(true);

      await api.updateRequest(id, {
        requesterName: values.requesterName,
        modelUrl: values.modelUrl,
        notes: values.notes || undefined,
        requestDelivery: values.requestDelivery,
        isPublic: values.isPublic,
        filamentId: values.filamentId,
      });

      toast({
        title: "Request updated successfully!",
        description: "Your changes have been saved.",
        variant: "success",
      });
      navigate(`/request/${id}`);
    } catch (error: any) {
      console.error('Error updating request:', error);
      const errorMessage = error.response?.data?.message || 'Failed to update request. Please try again.';
      toast({
        title: "Failed to update request",
        description: errorMessage,
        variant: "destructive",
      });
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <LoadingSpinner message="Loading request..." />;
  }

  if (!request) {
    return null;
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <Button variant="ghost" onClick={() => navigate(`/request/${id}`)}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Request
        </Button>
      </div>

      <div className="text-center space-y-2">
        <h1 className="text-3xl font-bold flex items-center justify-center gap-2">
          <Edit2 className="w-8 h-8" />
          Edit Request
        </h1>
        <p className="text-muted-foreground">
          Update your 3D printing request details
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Request Details</CardTitle>
          <CardDescription>
            {isAuthenticated
              ? 'Your request is linked to your account'
              : 'Your request is saved to this browser'}
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
                    <FormLabel>Your Name *</FormLabel>
                    <FormControl>
                      <Input placeholder="John Doe" {...field} />
                    </FormControl>
                    <FormDescription>
                      How should we address you?
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="modelUrl"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Model URL *</FormLabel>
                    <FormControl>
                      <Input
                        placeholder="https://www.thingiverse.com/thing:123456"
                        type="url"
                        {...field}
                      />
                    </FormControl>
                    <FormDescription>
                      Link to the 3D model you want printed (Thingiverse, Printables, etc.)
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="filamentId"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Filament *</FormLabel>
                    <Select onValueChange={field.onChange} value={field.value}>
                      <FormControl>
                        <SelectTrigger>
                          <SelectValue placeholder="Select a filament" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {filaments.map((filament) => (
                          <SelectItem key={filament.id} value={filament.id}>
                            {filament.name} - {filament.material} ({filament.colour}) - {filament.stockAmount}{filament.stockUnit} available
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormDescription>
                      Choose the filament material and color for your print
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
                    <FormLabel>Additional Notes</FormLabel>
                    <FormControl>
                      <Textarea
                        placeholder="Any special instructions or requirements..."
                        className="resize-none"
                        rows={4}
                        {...field}
                      />
                    </FormControl>
                    <FormDescription>
                      Optional: Provide any additional details about your print request
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="requestDelivery"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                    <FormControl>
                      <Checkbox
                        checked={field.value}
                        onCheckedChange={field.onChange}
                      />
                    </FormControl>
                    <div className="space-y-1 leading-none">
                      <FormLabel>
                        Request Delivery
                      </FormLabel>
                      <FormDescription>
                        Check this if you need the print delivered to you
                      </FormDescription>
                    </div>
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="isPublic"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                    <FormControl>
                      <Checkbox
                        checked={field.value}
                        onCheckedChange={field.onChange}
                      />
                    </FormControl>
                    <div className="space-y-1 leading-none">
                      <FormLabel>
                        Make request public
                      </FormLabel>
                      <FormDescription>
                        Public requests are visible to everyone. Uncheck to make this request private (only you and admins can view it)
                      </FormDescription>
                    </div>
                  </FormItem>
                )}
              />

              <div className="flex gap-4">
                <Button type="submit" disabled={submitting} className="flex-1">
                  {submitting && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                  Save Changes
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => navigate(`/request/${id}`)}
                  disabled={submitting}
                >
                  Cancel
                </Button>
              </div>
            </form>
          </Form>
        </CardContent>
      </Card>
    </div>
  );
};
