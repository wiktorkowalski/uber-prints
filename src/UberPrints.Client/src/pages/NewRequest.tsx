import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { api } from '../lib/api';
import { FilamentDto } from '../types/api';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '../components/ui/form';
import { Input } from '../components/ui/input';
import { Textarea } from '../components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../components/ui/select';
import { Checkbox } from '../components/ui/checkbox';
import { Skeleton } from '../components/ui/skeleton';
import { Popover, PopoverContent, PopoverTrigger } from '../components/ui/popover';
import { Info, ExternalLink } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { useToast } from '../hooks/use-toast';
import { Loader2, Package, Clock, CheckCircle2 } from 'lucide-react';
import { getDisplayName } from '../lib/utils';

const formSchema = z.object({
  requesterName: z.string().min(1, 'Name is required').max(100, 'Name must be less than 100 characters'),
  modelUrl: z.string().url('Must be a valid URL').max(500, 'URL must be less than 500 characters'),
  notes: z.string().max(1000, 'Notes must be less than 1000 characters').optional(),
  requestDelivery: z.boolean(),
  isPublic: z.boolean(),
  notifyOnStatusChange: z.boolean(),
  filamentId: z.string().optional(),
});

type FormValues = z.infer<typeof formSchema>;

export const NewRequest = () => {
  const navigate = useNavigate();
  const { user, isAuthenticated } = useAuth();
  const { toast } = useToast();
  const [filaments, setFilaments] = useState<FilamentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const form = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      requesterName: user?.username || '',
      modelUrl: '',
      notes: '',
      requestDelivery: false,
      isPublic: true,
      notifyOnStatusChange: true,
      filamentId: '',
    },
  });

  useEffect(() => {
    loadFilaments();
  }, []);

  useEffect(() => {
    if (user) {
      form.setValue('requesterName', getDisplayName(user));
    }
  }, [user, form]);

  const loadFilaments = async () => {
    try {
      const data = await api.getFilaments(); // Get all filaments (including pending)
      setFilaments(data);
    } catch (error) {
      console.error('Error loading filaments:', error);
    } finally {
      setLoading(false);
    }
  };

  const onSubmit = async (values: FormValues) => {
    try {
      setSubmitting(true);

      const request = await api.createRequest({
        requesterName: values.requesterName,
        modelUrl: values.modelUrl,
        notes: values.notes || undefined,
        requestDelivery: values.requestDelivery,
        isPublic: values.isPublic,
        notifyOnStatusChange: values.notifyOnStatusChange,
        filamentId: values.filamentId || undefined,
      });

      toast({
        title: "Request submitted successfully!",
        description: "Your print request has been created.",
        variant: "success",
      });
      navigate(`/requests/${request.id}`);
    } catch (error: any) {
      console.error('Error creating request:', error);
      const errorMessage = error.response?.data?.message || 'Failed to submit request. Please try again.';
      toast({
        title: "Failed to submit request",
        description: errorMessage,
        variant: "destructive",
      });
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="text-center space-y-2">
          <Skeleton className="h-10 w-64 mx-auto" />
          <Skeleton className="h-4 w-96 mx-auto" />
        </div>
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-72" />
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="space-y-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-10 w-full" />
            </div>
            <div className="space-y-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-10 w-full" />
            </div>
            <div className="space-y-2">
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-10 w-full" />
            </div>
            <div className="space-y-2">
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-32 w-full" />
            </div>
            <Skeleton className="h-16 w-full" />
          </CardContent>
        </Card>
      </div>
    );
  }

  if (filaments.length === 0) {
    return (
      <div className="max-w-2xl mx-auto">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Package className="w-6 h-6" />
              No Filaments Available
            </CardTitle>
            <CardDescription>
              There are currently no filaments in stock to process your request
            </CardDescription>
          </CardHeader>
          <CardContent className="text-center">
            <p className="text-muted-foreground mb-4">
              Please check back later when filaments are restocked, or contact the administrator.
            </p>
            <Button variant="outline" onClick={() => navigate('/requests')}>
              View Existing Requests
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="text-center space-y-2">
        <h1 className="text-3xl font-bold flex items-center justify-center gap-2">
          <Package className="w-8 h-8" />
          Submit New Request
        </h1>
        <p className="text-muted-foreground">
          Fill out the form below to submit your 3D printing request
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Request Details</CardTitle>
          <CardDescription>
            {isAuthenticated
              ? 'Your request will be linked to your account'
              : 'Your requests will be saved to this browser'}
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
                    <div className="flex items-center gap-2">
                      <FormLabel>Model URL *</FormLabel>
                      <Popover>
                        <PopoverTrigger asChild>
                          <button type="button" className="focus:outline-none">
                            <Info className="w-4 h-4 text-muted-foreground cursor-help hover:text-foreground transition-colors" />
                          </button>
                        </PopoverTrigger>
                        <PopoverContent className="w-80">
                          <div className="space-y-3">
                            <div>
                              <h4 className="font-semibold text-sm mb-2">Recommended Model Sources</h4>
                              <p className="text-sm text-muted-foreground mb-3">
                                <strong className="text-foreground">Printables</strong> is preferred, but you can also use:
                              </p>
                            </div>
                            <div className="space-y-2">
                              <a
                                href="https://www.printables.com"
                                target="_blank"
                                rel="noopener noreferrer"
                                className="flex items-center gap-2 text-sm text-primary hover:underline"
                              >
                                <ExternalLink className="w-3 h-3" />
                                Printables.com (Preferred)
                              </a>
                              <a
                                href="https://www.thingiverse.com"
                                target="_blank"
                                rel="noopener noreferrer"
                                className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground hover:underline"
                              >
                                <ExternalLink className="w-3 h-3" />
                                Thingiverse.com (Alternative)
                              </a>
                              <a
                                href="https://makerworld.com"
                                target="_blank"
                                rel="noopener noreferrer"
                                className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground hover:underline"
                              >
                                <ExternalLink className="w-3 h-3" />
                                MakerWorld.com (Alternative)
                              </a>
                            </div>
                            <p className="text-xs text-muted-foreground pt-2 border-t">
                              Other model links are also accepted
                            </p>
                          </div>
                        </PopoverContent>
                      </Popover>
                    </div>
                    <FormControl>
                      <Input
                        placeholder="https://www.printables.com/model/123456"
                        type="url"
                        {...field}
                      />
                    </FormControl>
                    <FormDescription>
                      Link to the 3D model (Printables preferred, Thingiverse and MakerWorld also supported)
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
                    <FormLabel>Filament (Optional)</FormLabel>
                    <Select
                      onValueChange={(value) => field.onChange(value === 'none' ? '' : value)}
                      defaultValue={field.value || 'none'}
                    >
                      <FormControl>
                        <SelectTrigger>
                          <SelectValue placeholder="Select a filament (optional - admin will assign if not selected)" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value="none">No preference - Let admin choose</SelectItem>
                        {filaments.filter(f => f.isAvailable && f.stockAmount > 0).length > 0 && (
                          <>
                            <div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground">
                              ✓ Available Now
                            </div>
                            {filaments
                              .filter(f => f.isAvailable && f.stockAmount > 0)
                              .map((filament) => (
                                <SelectItem key={filament.id} value={filament.id}>
                                  <span className="flex items-center gap-2">
                                    <CheckCircle2 className="w-4 h-4 text-green-500" />
                                    {filament.name} - {filament.material} ({filament.colour}) - {filament.stockAmount}{filament.stockUnit}
                                  </span>
                                </SelectItem>
                              ))}
                          </>
                        )}
                        {filaments.filter(f => !f.isAvailable || f.stockAmount === 0).length > 0 && (
                          <>
                            <div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground border-t mt-1 pt-2">
                              ⏳ Pending / Out of Stock
                            </div>
                            {filaments
                              .filter(f => !f.isAvailable || f.stockAmount === 0)
                              .map((filament) => (
                                <SelectItem key={filament.id} value={filament.id}>
                                  <span className="flex items-center gap-2 text-muted-foreground">
                                    <Clock className="w-4 h-4 text-yellow-500" />
                                    {filament.name} - {filament.material} ({filament.colour})
                                    {!filament.isAvailable && <span className="text-xs">(Pending approval)</span>}
                                    {filament.isAvailable && filament.stockAmount === 0 && <span className="text-xs">(Out of stock)</span>}
                                  </span>
                                </SelectItem>
                              ))}
                          </>
                        )}
                      </SelectContent>
                    </Select>
                    <FormDescription>
                      Choose the filament material and color for your print, or leave blank if you're not sure
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
                    <div className="space-y-1 leading-none flex-1">
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

              <FormField
                control={form.control}
                name="notifyOnStatusChange"
                render={({ field }) => (
                  <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                    <FormControl>
                      <Checkbox
                        checked={field.value}
                        onCheckedChange={field.onChange}
                      />
                    </FormControl>
                    <div className="space-y-1 leading-none flex-1">
                      <FormLabel>
                        Notify me about status changes
                      </FormLabel>
                      <FormDescription>
                        Get Discord DM notifications when your request status changes
                      </FormDescription>
                      <div className="mt-2 p-2 bg-blue-50 dark:bg-blue-950 rounded text-xs text-blue-900 dark:text-blue-100">
                        <strong>Note:</strong> You must{' '}
                        <a
                          href="https://discord.com/oauth2/authorize?client_id=1402663574962438194"
                          target="_blank"
                          rel="noopener noreferrer"
                          className="underline hover:text-blue-700 dark:hover:text-blue-300"
                        >
                          add the bot to your Discord
                        </a>
                        {' '}for notifications to work
                      </div>
                    </div>
                  </FormItem>
                )}
              />

              <div className="flex gap-4">
                <Button type="submit" disabled={submitting} className="flex-1">
                  {submitting && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                  Submit Request
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => navigate('/requests')}
                  disabled={submitting}
                >
                  Cancel
                </Button>
              </div>
            </form>
          </Form>
        </CardContent>
      </Card>

      {!isAuthenticated && (
        <Card className="bg-muted/50">
          <CardContent className="pt-6">
            <p className="text-sm text-muted-foreground text-center">
              💡 <strong>Tip:</strong> Sign in with Discord to access your requests from any device and get additional features!
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
};
