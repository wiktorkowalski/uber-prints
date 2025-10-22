# UberPrints 3D Request System - Requirements Specification

## Project Overview

UberPrints is a web application that allows friends to request 3D printing services with optional delivery. The system supports both guest users (with tracking tokens) and Discord-authenticated users, with a special admin interface for managing requests and their statuses.

## Business Objectives

1. **Streamline 3D Printing Requests**: Provide a simple, intuitive interface for users to submit 3D printing requests
2. **Transparent Status Tracking**: Enable users to track the progress of their requests through a complete status workflow
3. **Flexible User Access**: Support both anonymous guest users and authenticated Discord users
4. **Efficient Admin Management**: Provide administrators with tools to manage request statuses and communicate with users
5. **Mobile-Friendly Experience**: Ensure the application works seamlessly on desktop and mobile devices

## User Roles and Permissions

### Guest Users
- Submit new 3D printing requests
- View all requests in the public list
- Track their specific requests using unique tracking tokens
- Cannot edit or delete requests after submission
- No persistent account or request history

### Authenticated Users (Discord)
- All guest capabilities
- Edit their own requests after submission
- Delete their own requests
- Persistent request history tied to their Discord account
- Personalized dashboard showing their requests

### Admin User
- All user capabilities
- Change status of any request in the system
- Add admin notes to status changes for user communication
- View admin-specific request management interface
- Access determined by specific Discord User ID in configuration

## Functional Requirements

### FR1: Request Submission
- **FR1.1**: Users must be able to submit 3D printing requests with the following information:
  - Requester name (required, max 100 characters)
  - Model URL (required, valid URL, max 500 characters)
  - Optional notes (max 1000 characters)
  - Delivery preference (boolean)
  - Privacy setting (public/private, default: public)
  - Filament selection (optional)
- **FR1.2**: All users (guest and authenticated) receive a unique tracking token upon submission
- **FR1.3**: Both authenticated and guest users have requests linked to their user account
- **FR1.4**: All requests start with "Pending" status
- **FR1.5**: Users can select any filament, including those out of stock (admin will manage availability)
- **FR1.6**: Guest users must create a guest session before submitting requests

### FR2: Request Viewing
- **FR2.1**: Public view displays all public requests with basic information
- **FR2.2**: Private requests are only visible to their owners
- **FR2.3**: Each request shows its current status and complete status history
- **FR2.4**: Status history includes timestamps, status names, admin notes, and user who made the change
- **FR2.5**: Request detail view shows field-level change history for tracking edits
- **FR2.6**: Requests are displayed in reverse chronological order (newest first)

### FR3: Request Tracking
- **FR3.1**: All users can track requests using the unique tracking token provided
- **FR3.2**: Tracking provides full request details, status history, and change history
- **FR3.3**: Guest users can view their requests using guest session tokens
- **FR3.4**: Authenticated users can view all their requests from their dashboard
- **FR3.5**: Admin users can view all requests in the system (both public and private)

### FR4: Request Management (All Users)
- **FR4.1**: Both authenticated and guest users can edit their own requests (all fields)
- **FR4.2**: Both authenticated and guest users can delete their own requests
- **FR4.3**: Users can only edit/delete requests they own (verified by user ID or guest session token)
- **FR4.4**: Edit/delete permissions are validated on the server
- **FR4.5**: All edits are tracked with field-level change history including user and timestamp

### FR5: Status Management (Admin)
- **FR5.1**: Admin users can change the status of any request
- **FR5.2**: Admin users can add notes when changing status
- **FR5.3**: All status changes are recorded in the status history
- **FR5.4**: Status changes include the admin user who made the change

### FR6: Filament Management (Admin)
- **FR6.1**: Admin users can add new filament types to the system
- **FR6.2**: Admin users can update existing filament information
- **FR6.3**: Admin users can remove filament types from the system
- **FR6.4**: Admin users can update filament stock quantities
- **FR6.5**: Admin users can toggle filament availability (IsAvailable flag)
- **FR6.6**: Filament information includes: name, material, brand, colour, stock amount, stock unit, availability, optional link, and optional photo
- **FR6.7**: Admin users can view and manage filament requests from users

### FR7: Filament Selection (Users)
- **FR7.1**: Users can view all filament options in the catalog
- **FR7.2**: Users can select a specific filament for their print request (optional)
- **FR7.3**: Users can preview filament details by clicking on the filament link
- **FR7.4**: Users can view filament photos when available
- **FR7.5**: Users can request new filaments that aren't in the catalog

### FR8: Filament Requests (Users)
- **FR8.1**: Users can submit requests for new filament types
- **FR8.2**: Filament requests include: material, brand, colour, optional link, and optional notes
- **FR8.3**: Filament requests have statuses: Pending, Approved, Rejected
- **FR8.4**: Admin users can approve or reject filament requests
- **FR8.5**: Approved filament requests can be converted to actual filaments in the catalog

### FR9: Authentication
- **FR9.1**: Users can authenticate using Discord OAuth 2.0
- **FR9.2**: Authentication is optional (guest access is supported via guest sessions)
- **FR9.3**: Discord authentication provides access to user's Discord ID, username, global name, and avatar
- **FR9.4**: Both JWT tokens and cookies are used for maintaining authenticated sessions
- **FR9.5**: Admin access is determined by role-based authorization
- **FR9.6**: Guest users can create anonymous sessions with a guest session token
- **FR9.7**: Guest users can be converted to authenticated users when they log in via Discord
- **FR9.8**: Guest requests can be linked to authenticated accounts upon login

### FR10: Status Workflow
- **FR10.1**: Requests follow a predefined status workflow:
  1. Pending → Initial state when request is submitted
  2. Accepted → Admin approves the request for printing
  3. Rejected → Admin declines the request
  4. OnHold → Temporarily paused by admin
  5. Paused → Printing process paused
  6. WaitingForMaterials → Need materials to continue
  7. Delivering → Item is being delivered (if requested)
  8. WaitingForPickup → Ready for collection
  9. Completed → Request fulfilled
- **FR10.2**: Status transitions follow business rules (e.g., only "Completed" and "Rejected" are terminal)
- **FR10.3**: Some statuses can transition back to "Accepted" (OnHold, Paused, WaitingForMaterials)

## User Stories

### Guest User Stories
- **GUS1**: As a guest user, I want to create a guest session, so I can submit requests without a Discord account
- **GUS2**: As a guest user, I want to submit a 3D printing request, so I can quickly request a print
- **GUS3**: As a guest user, I want to receive a tracking token for my request, so I can check its status later
- **GUS4**: As a guest user, I want to view all public requests, so I can see what others are printing
- **GUS5**: As a guest user, I want to track my request using the token, so I can monitor its progress
- **GUS6**: As a guest user, I want to edit and delete my own requests, so I can manage my submissions
- **GUS7**: As a guest user, I want to make my requests private, so only I can see them
- **GUS8**: As a guest user, I want my requests to be linked to my Discord account when I log in, so I don't lose them

### Authenticated User Stories
- **AUS1**: As a Discord user, I want to login with my Discord account, so I can have persistent access
- **AUS2**: As an authenticated user, I want to see my Discord profile (avatar, username), so I feel personalized
- **AUS3**: As an authenticated user, I want to edit my requests after submission, so I can correct mistakes
- **AUS4**: As an authenticated user, I want to delete my requests, so I can remove unwanted ones
- **AUS5**: As an authenticated user, I want to see all my requests in my dashboard, so I can manage them easily
- **AUS6**: As an authenticated user, I want my request history saved permanently, so I can reference past requests
- **AUS7**: As an authenticated user, I want to make requests private, so only I can see them
- **AUS8**: As an authenticated user, I want to see change history on my requests, so I can track edits

### Admin User Stories
- **ADS1**: As an admin, I want to change the status of any request, so I can manage the printing workflow
- **ADS2**: As an admin, I want to add notes when changing status, so I can communicate with users
- **ADS3**: As an admin, I want to see all requests in the system (including private ones), so I can oversee the entire queue
- **ADS4**: As an admin, I want to view the complete status history and change history of requests, so I can understand their journey
- **ADS5**: As an admin, I want a dedicated admin interface, so I can efficiently manage requests
- **ADS6**: As an admin, I want to edit any request field, so I can fix issues or make corrections
- **ADS7**: As an admin, I want to add new filament types to the system, so I can expand my material options
- **ADS8**: As an admin, I want to update filament stock quantities and availability, so I can keep accurate inventory
- **ADS9**: As an admin, I want to remove filament types that are no longer available, so I can keep the system clean
- **ADS10**: As an admin, I want to edit filament details like brand, colour, and links, so I can maintain accurate information
- **ADS11**: As an admin, I want to view and manage filament requests from users, so I can approve or reject them
- **ADS12**: As an admin, I want to convert approved filament requests into actual filaments, so users can use them

### Filament User Stories
- **FUS1**: As a user, I want to see all filament options in the catalog, so I can choose the right material
- **FUS2**: As a user, I want to see filament stock levels and availability, so I know what's available
- **FUS3**: As a user, I want to preview filament details by clicking a link, so I can make informed decisions
- **FUS4**: As a user, I want to see filament photos when available, so I can visualize the colour and material
- **FUS5**: As a user, I want to select a specific filament for my request, so I can get the desired result
- **FUS6**: As a user, I want to request new filaments that aren't in the catalog, so I can get custom materials
- **FUS7**: As a user, I want to track the status of my filament requests, so I know if they're approved

## Business Rules

### BR1: Request Submission Rules
- All requests must have a valid model URL
- Requester name is required and limited to 100 characters
- Notes are optional but limited to 1000 characters if provided
- Delivery preference must be specified (yes/no)
- Privacy preference must be specified (default: public)
- Filament selection is optional
- Users must have a valid session (guest or authenticated) to submit requests

### BR2: Status Transition Rules
- New requests always start as "Pending"
- Only "Completed" and "Rejected" are terminal states
- "OnHold", "Paused", and "WaitingForMaterials" can transition back to "Accepted"
- "Delivering" and "WaitingForPickup" can only transition to "Completed"
- Admin users can override most transition rules with proper justification
- All status changes are recorded in the status history with timestamps

### BR3: Permission Rules
- Guest users can submit, view, edit, and delete their own requests
- Authenticated users can edit/delete only their own requests
- Admin users can manage all requests (including private ones)
- Admin access is restricted by role-based authorization
- Ownership is verified by matching user ID or guest session token
- Private requests are only visible to the owner and admins

### BR4: Data Rules
- Guest tracking tokens must be unique and non-guessable (16-character uppercase hex)
- Guest session tokens must be unique and non-guessable (32-character uppercase hex)
- All status changes must be audited with timestamps
- All field changes must be audited with timestamps
- User data from Discord is stored for authentication and display purposes
- Request data is retained indefinitely for historical reference
- All IDs are UUIDs (v7 format for better performance)

### BR5: Filament Management Rules
- Filament stock quantities cannot be negative
- Filament types with active requests should not be deleted (soft delete via IsAvailable flag preferred)
- Filament links must be valid URLs when provided
- Filament photos must be valid image URLs when provided
- Filament availability is managed via the IsAvailable flag
- Users can select any filament regardless of stock or availability (admin manages this)

## Non-Functional Requirements

### NFR1: Performance
- The application should load within 3 seconds on standard broadband
- API responses should be under 500ms for database operations
- The system should support 100 concurrent users without degradation

### NFR2: Security
- All communications must use HTTPS
- User input must be validated and sanitized
- JWT tokens should expire after 1 hour with refresh capability
- Admin operations must be properly authorized

### NFR3: Usability
- The interface should be responsive on mobile devices (320px width minimum)
- The application should be accessible according to WCAG 2.1 AA standards
- User workflows should require minimal clicks to complete

### NFR4: Reliability
- The system should have 99.9% uptime
- Database operations should be atomic and consistent
- User sessions should persist across browser restarts (when authenticated)

### NFR5: Maintainability
- Code should follow established coding standards
- API should be versioned for future compatibility
- Database schema changes should be handled through migrations
- Documentation should be kept current with features

## Data Requirements

### DR1: User Data
- User ID (UUID/GUID)
- Discord User ID (for authenticated users)
- Discord username
- Discord global name (display name)
- Discord avatar hash
- Guest session token (for guest users)
- Admin status flag
- Account creation timestamp

### DR2: Request Data
- Request ID (UUID/GUID)
- Requester name
- Model URL
- Optional notes
- Delivery preference
- Privacy setting (public/private)
- Selected filament ID (optional)
- Current status (enum)
- Creation and update timestamps
- Associated user ID (for both authenticated and guest users)
- Guest tracking token (separate from guest session, for anonymous tracking)
- Change history (field-level audit trail)
- Status history

### DR3: Filament Data
- Filament ID (UUID/GUID)
- Filament name/identifier
- Material type (PLA, ABS, PETG, etc.)
- Brand
- Colour
- Current stock amount (in grams or meters)
- Stock unit (default: grams)
- Availability flag (IsAvailable)
- Optional product link
- Optional photo URL
- Creation and update timestamps

### DR4: Status Data
- Status ID (UUID/GUID)
- Status (enum: Pending, Accepted, Rejected, OnHold, Paused, WaitingForMaterials, Delivering, WaitingForPickup, Completed)
- Status history with timestamps
- Admin notes for status changes
- User who made status changes (optional)

### DR5: Filament Request Data
- Filament request ID (UUID/GUID)
- Requester name
- Material type requested
- Brand requested
- Colour requested
- Optional product link
- Optional notes
- Current status (enum: Pending, Approved, Rejected)
- Associated user ID (optional)
- Created filament ID (if approved and filament created)
- Status history
- Creation and update timestamps

### DR6: Change Tracking Data
- Change ID (UUID/GUID)
- Print request ID
- Field name changed
- Old value
- New value
- User who made the change (optional)
- Timestamp

## Integration Requirements

### IR1: Discord OAuth Integration
- Implement OAuth 2.0 flow with Discord
- Request user identity and email scopes
- Handle authentication callbacks
- Store user information securely

### IR2: Database Integration
- Use PostgreSQL 18 as the primary database
- Implement Entity Framework Core for data access
- Support database migrations
- Handle connection pooling and failover
