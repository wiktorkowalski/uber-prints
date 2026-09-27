import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../lib/api';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { useToast } from '../hooks/use-toast';
import { LoadingSpinner } from '../components/ui/loading-spinner';
import { Ticket, TicketDivider } from '../components/Ticket';

const TOKEN_LENGTH = 16;

export const TrackRequest = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { toast } = useToast();
  const [trackingToken, setTrackingToken] = useState(() => (searchParams.get('token') ?? '').trim().toUpperCase());
  const [loading, setLoading] = useState(false);
  const autoSubmitted = useRef(false);

  const track = useCallback(async (token: string) => {
    try {
      setLoading(true);
      const request = await api.trackRequest(token);
      toast({
        title: "Request found!",
        description: "Redirecting to your request...",
        variant: "success",
      });
      navigate(`/requests/${request.id}`, { state: { tracked: true } });
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
  }, [navigate, toast]);

  // /track?token=XYZ looks the token up right away.
  useEffect(() => {
    if (autoSubmitted.current || !trackingToken) return;
    autoSubmitted.current = true;
    track(trackingToken);
  }, [trackingToken, track]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!trackingToken.trim()) {
      toast({
        title: "Tracking token required",
        description: "Please enter your tracking token",
        variant: "destructive",
      });
      return;
    }

    track(trackingToken.trim());
  };

  return (
    <div className="mx-auto max-w-md space-y-6 py-4">
      <div className="space-y-2">
        <h1 className="page-title">Track your print</h1>
        <p className="text-muted-foreground">
          Enter your tracking token to see where your request is. No account needed.
        </p>
      </div>

      <Ticket className="space-y-4 px-6 pt-6">
        <div className="font-heading text-lg font-extrabold uppercase tracking-wide">Print request</div>
        <TicketDivider />
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="trackingToken" className="font-ticket font-normal text-muted-foreground">
              Enter the {TOKEN_LENGTH}-character code from your request page
            </Label>
            <Input
              id="trackingToken"
              placeholder="ABC123DEF4567890"
              value={trackingToken}
              onChange={(e) => setTrackingToken(e.target.value.toUpperCase())}
              disabled={loading}
              className="h-12 font-ticket text-base tracking-widest"
              maxLength={TOKEN_LENGTH}
              autoComplete="off"
              spellCheck={false}
            />
          </div>
          <Button type="submit" disabled={loading || !trackingToken.trim()} className="w-full">
            {loading ? (
              <>
                <LoadingSpinner />
                Tracking...
              </>
            ) : (
              'Track'
            )}
          </Button>
        </form>
        <TicketDivider />
        <p className="text-xs text-muted-foreground">
          Signed in with Discord? Your requests are on your{' '}
          <Link to="/dashboard" className="font-semibold text-primary hover:underline">dashboard</Link>.
        </p>
      </Ticket>
    </div>
  );
};
