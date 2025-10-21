import { useEffect, useState } from 'react';
import { api } from '../lib/api';
import { ProfileDto } from '../types/api';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { Skeleton } from '../components/ui/skeleton';
import { Avatar, AvatarFallback, AvatarImage } from '../components/ui/avatar';
import { Separator } from '../components/ui/separator';
import { User, Edit2, Save, X, Loader2 } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const displayNameSchema = z.object({
  displayName: z.string().min(1, 'Display name is required').max(100, 'Display name must be less than 100 characters'),
});

type DisplayNameForm = z.infer<typeof displayNameSchema>;

export const Profile = () => {
  const [profile, setProfile] = useState<ProfileDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [saveLoading, setSaveLoading] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<DisplayNameForm>({
    resolver: zodResolver(displayNameSchema),
  });

  const loadProfile = async () => {
    try {
      setLoading(true);
      const data = await api.getProfile();
      setProfile(data);
      reset({ displayName: data.globalName || data.username });
    } catch (err) {
      console.error('Error loading profile:', err);
      setError('Failed to load profile');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadProfile();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const onSubmit = async (data: DisplayNameForm) => {
    try {
      setSaveLoading(true);
      const updatedProfile = await api.updateDisplayName({ displayName: data.displayName });
      setProfile(updatedProfile);
      setIsEditing(false);
      setError(null);
    } catch (err) {
      console.error('Error updating display name:', err);
      setError('Failed to update display name');
    } finally {
      setSaveLoading(false);
    }
  };

  const handleCancelEdit = () => {
    setIsEditing(false);
    reset({ displayName: profile?.globalName || profile?.username });
    setError(null);
  };

  if (loading) {
    return (
      <div className="max-w-4xl mx-auto space-y-6">
        <div>
          <Skeleton className="h-10 w-32" />
          <Skeleton className="h-4 w-96 mt-2" />
        </div>
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-64" />
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="flex items-center gap-4">
              <Skeleton className="w-24 h-24 rounded-full" />
              <div className="space-y-2">
                <Skeleton className="h-4 w-32" />
                <Skeleton className="h-4 w-48" />
              </div>
            </div>
            <div className="space-y-2">
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-10 w-full" />
            </div>
            <div className="space-y-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-10 w-full" />
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-32" />
            <Skeleton className="h-4 w-64" />
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-10 w-full" />
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="text-center py-12">
        <p className="text-muted-foreground">Unable to load profile</p>
      </div>
    );
  }

  const displayName = profile.globalName || profile.username;
  const getInitials = (name: string) => {
    return name
      .split(' ')
      .map(n => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold flex items-center gap-2">
          <User className="w-8 h-8" />
          Profile
        </h1>
        <p className="text-muted-foreground mt-2">
          View and manage your profile information from Discord
        </p>
      </div>

      {error && (
        <div className="bg-destructive/10 text-destructive px-4 py-3 rounded-md">
          {error}
        </div>
      )}

      {/* Profile Card */}
      <Card>
        <CardHeader>
          <CardTitle>Discord Information</CardTitle>
          <CardDescription>
            Information provided by Discord OAuth
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Avatar */}
          <div className="flex items-center gap-4">
            <Avatar className="w-24 h-24">
              <AvatarImage src={profile.avatarUrl || undefined} alt={displayName} />
              <AvatarFallback className="text-2xl">
                {getInitials(displayName)}
              </AvatarFallback>
            </Avatar>
            <div>
              <p className="text-sm font-medium text-muted-foreground">Avatar</p>
              <p className="text-sm text-muted-foreground mt-1">
                Synced from Discord
              </p>
            </div>
          </div>

          <Separator />

          {/* Discord Username */}
          <div>
            <Label>Discord Username</Label>
            <Input
              value={profile.username}
              disabled
              className="mt-2"
            />
            <p className="text-sm text-muted-foreground mt-1">
              This is your unique Discord username
            </p>
          </div>

          {/* Discord ID */}
          {profile.discordId && (
            <div>
              <Label>Discord ID</Label>
              <Input
                value={profile.discordId}
                disabled
                className="mt-2 font-mono text-sm"
              />
            </div>
          )}
        </CardContent>
      </Card>

      {/* Display Name Card */}
      <Card>
        <CardHeader>
          <CardTitle>Display Name</CardTitle>
          <CardDescription>
            The name that will be shown throughout the application
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div>
              <div className="flex items-center justify-between mb-2">
                <Label htmlFor="displayName">Display Name</Label>
                {!isEditing && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => setIsEditing(true)}
                  >
                    <Edit2 className="w-4 h-4 mr-2" />
                    Edit
                  </Button>
                )}
              </div>
              <Input
                id="displayName"
                {...register('displayName')}
                disabled={!isEditing}
                className={isEditing ? '' : 'bg-muted'}
              />
              {errors.displayName && (
                <p className="text-sm text-destructive mt-1">
                  {errors.displayName.message}
                </p>
              )}
              <p className="text-sm text-muted-foreground mt-1">
                {isEditing
                  ? 'Enter a custom display name for this system'
                  : 'Click "Edit" to change your display name'}
              </p>
            </div>

            {isEditing && (
              <div className="flex gap-2">
                <Button type="submit" disabled={saveLoading}>
                  {saveLoading ? (
                    <>
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                      Saving...
                    </>
                  ) : (
                    <>
                      <Save className="w-4 h-4 mr-2" />
                      Save
                    </>
                  )}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleCancelEdit}
                  disabled={saveLoading}
                >
                  <X className="w-4 h-4 mr-2" />
                  Cancel
                </Button>
              </div>
            )}
          </form>
        </CardContent>
      </Card>

      {/* Account Details Card */}
      <Card>
        <CardHeader>
          <CardTitle>Account Details</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label>Account Type</Label>
            <Input
              value={profile.isAdmin ? 'Administrator' : 'User'}
              disabled
              className="mt-2"
            />
          </div>

          <div>
            <Label>Member Since</Label>
            <Input
              value={new Date(profile.createdAt).toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'long',
                day: 'numeric',
              })}
              disabled
              className="mt-2"
            />
          </div>
        </CardContent>
      </Card>
    </div>
  );
};
