import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../lib/api';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { useToast } from '../hooks/use-toast';
import { LoadingSpinner } from '../components/ui/loading-spinner';
import { Search, Package, AlertCircle } from 'lucide-react';

export const TrackRequest = () => {
  const navigate = useNavigate();
  const { toast } = useToast();
  const [trackingToken, setTrackingToken] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!trackingToken.trim()) {
      toast({
        title: "Tracking token required",
        description: "Please enter your tracking token",
        variant: "destructive",
      });
      return;
    }

    try {
      setLoading(true);
      const request = await api.trackRequest(trackingToken.trim());
      toast({
        title: "Request found!",
        description: "Redirecting to your request...",
        variant: "success",
      });
      navigate(`/request/${request.id}`);
    } catch (error: any) {
      console.error('Error tracking request:', error);
      const errorMessage = error.response?.status === 404
        ? 'No request found with that tracking token'
        : 'Failed to track request. Please try again.';
      toast({
        title: "Request not found",
        description: errorMessage,
        variant: "destructive",
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div className="text-center space-y-2">
        <h1 className="text-3xl font-bold flex items-center justify-center gap-2">
          <Search className="w-8 h-8" />
          Track Your Request
        </h1>
        <p className="text-muted-foreground">
          Enter your tracking token to view your print request status
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Guest Request Tracking</CardTitle>
          <CardDescription>
            Use your tracking token to check the status of your print request without logging in
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="trackingToken">Tracking Token</Label>
              <Input
                id="trackingToken"
                placeholder="Enter your tracking token (e.g., ABC123DEF4567890)"
                value={trackingToken}
                onChange={(e) => setTrackingToken(e.target.value.toUpperCase())}
                disabled={loading}
                className="font-mono"
                maxLength={16}
              />
              <p className="text-sm text-muted-foreground">
                Your tracking token was provided when you submitted your request
              </p>
            </div>

            <Button type="submit" disabled={loading || !trackingToken.trim()} className="w-full">
              {loading ? (
                <>
                  <LoadingSpinner />
                  Tracking...
                </>
              ) : (
                <>
                  <Search className="w-4 h-4 mr-2" />
                  Track Request
                </>
              )}
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card className="bg-blue-50 dark:bg-blue-950 border-blue-200 dark:border-blue-800">
        <CardContent className="pt-6">
          <div className="flex items-start gap-3">
            <AlertCircle className="w-5 h-5 text-blue-600 dark:text-blue-400 flex-shrink-0 mt-0.5" />
            <div className="space-y-2">
              <h3 className="font-medium text-blue-900 dark:text-blue-100">
                What is a tracking token?
              </h3>
              <p className="text-sm text-blue-700 dark:text-blue-300">
                A tracking token is a unique code that was generated when you submitted your print request.
                You can find it on the request details page or in any confirmation message you received.
              </p>
              <p className="text-sm text-blue-700 dark:text-blue-300">
                If you have an account, you can simply sign in to view all your requests without needing a tracking token.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card className="bg-muted/50">
        <CardContent className="pt-6">
          <div className="flex items-start gap-3">
            <Package className="w-5 h-5 text-muted-foreground flex-shrink-0 mt-0.5" />
            <div>
              <h3 className="font-medium mb-1">
                Want to see all your requests?
              </h3>
              <p className="text-sm text-muted-foreground mb-3">
                Sign in with Discord to access all your print requests from any device and get notifications about status updates.
              </p>
              <Button
                variant="outline"
                size="sm"
                onClick={() => window.location.href = api.getDiscordLoginUrl()}
              >
                Sign in with Discord
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
