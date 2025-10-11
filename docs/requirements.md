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
  - Filament selection (optional, only from available stock)
- **FR1.2**: Guest users receive a unique tracking token upon submission
- **FR1.3**: Authenticated users have requests automatically linked to their account
- **FR1.4**: All requests start with "Pending" status
- **FR1.5**: Users can only select filaments that are currently in stock

### FR2: Request Viewing
- **FR2.1**: Public view displays all requests with basic information
- **FR2.2**: Each request shows its current status and complete status history
- **FR2.3**: Status history includes timestamps, status names, and admin notes
- **FR2.4**: Requests are displayed in reverse chronological order (newest first)

### FR3: Request Tracking
- **FR3.1**: Guest users can track their requests using the unique token provided
- **FR3.2**: Tracking provides full request details and status history
- **FR3.3**: Authenticated users can view all their requests from their dashboard
- **FR3.4**: Admin users can view all requests in the system

### FR4: Request Management (Authenticated Users)
- **FR4.1**: Authenticated users can edit their own requests (all fields except requester name)
- **FR4.2**: Authenticated users can delete their own requests
- **FR4.3**: Users can only edit/delete requests they own
- **FR4.4**: Edit/delete permissions are validated on the server

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
- **FR6.5**: Filament information includes: amount, material, brand, colour, link, and optional photo

### FR7: Filament Selection (Users)
- **FR7.1**: Users can view available filament options based on current stock
- **FR7.2**: Users can select a specific filament for their print request
- **FR7.3**: Users can preview filament details by clicking on the filament link
- **FR7.4**: Users can view filament photos when available
- **FR7.5**: Out-of-stock filaments are not selectable for new requests

### FR8: Authentication
- **FR8.1**: Users can authenticate using Discord OAuth 2.0
- **FR8.2**: Authentication is optional (guest access is supported)
- **FR8.3**: Discord authentication provides access to user's Discord ID, username, and email
- **FR8.4**: JWT tokens are used for maintaining authenticated sessions
- **FR8.5**: Admin access is determined by specific Discord User ID in configuration

### FR9: Status Workflow
- **FR9.1**: Requests follow a predefined status workflow:
  1. Pending → Initial state when request is submitted
  2. Accepted → Admin approves the request for printing
  3. Rejected → Admin declines the request
  4. On Hold → Temporarily paused by admin
  5. Paused → Printing process paused
  6. Waiting for Materials → Need materials to continue
  7. Delivering → Item is being delivered (if requested)
  8. Waiting for Pickup → Ready for collection
  9. Completed → Request fulfilled
- **FR9.2**: Status transitions follow business rules (e.g., only "Completed" is terminal)
- **FR9.3**: Some statuses can transition back to "Accepted" (On Hold, Paused, Waiting for Materials)

## User Stories

### Guest User Stories
- **GUS1**: As a guest user, I want to submit a 3D printing request without creating an account, so I can quickly request a print
- **GUS2**: As a guest user, I want to receive a tracking token for my request, so I can check its status later
- **GUS3**: As a guest user, I want to view all public requests, so I can see what others are printing
- **GUS4**: As a guest user, I want to track my request using the token, so I can monitor its progress

### Authenticated User Stories
- **AUS1**: As a Discord user, I want to login with my Discord account, so I can have enhanced features
- **AUS2**: As an authenticated user, I want to edit my requests after submission, so I can correct mistakes
- **AUS3**: As an authenticated user, I want to delete my requests, so I can remove unwanted ones
- **AUS4**: As an authenticated user, I want to see all my requests in my dashboard, so I can manage them easily
- **AUS5**: As an authenticated user, I want my request history saved, so I can reference past requests

### Admin User Stories
- **ADS1**: As an admin, I want to change the status of any request, so I can manage the printing workflow
- **ADS2**: As an admin, I want to add notes when changing status, so I can communicate with users
- **ADS3**: As an admin, I want to see all requests in the system, so I can oversee the entire queue
- **ADS4**: As an admin, I want to view the complete status history of requests, so I can understand their journey
- **ADS5**: As an admin, I want a dedicated admin interface, so I can efficiently manage requests
- **ADS6**: As an admin, I want to add new filament types to the system, so I can expand my material options
- **ADS7**: As an admin, I want to update filament stock quantities, so I can keep accurate inventory
- **ADS8**: As an admin, I want to remove filament types that are no longer available, so I can keep the system clean
- **ADS9**: As an admin, I want to edit filament details like brand, colour, and links, so I can maintain accurate information

### Filament User Stories
- **FUS1**: As a user, I want to see available filament options for my print, so I can choose the right material
- **FUS2**: As a user, I want to see filament stock levels, so I know what's available
- **FUS3**: As a user, I want to preview filament details by clicking a link, so I can make informed decisions
- **FUS4**: As a user, I want to see filament photos when available, so I can visualize the colour and material
- **FUS5**: As a user, I want to select a specific filament for my request, so I can get the desired result

## Business Rules

### BR1: Request Submission Rules
- All requests must have a valid model URL
- Requester name is required and limited to 100 characters
- Notes are optional but limited to 1000 characters if provided
- Delivery preference must be specified (yes/no)
- Filament selection is optional

### BR2: Status Transition Rules
- New requests always start as "Pending"
- Only "Completed" and "Rejected" are terminal states
- "On Hold", "Paused", and "Waiting for Materials" can transition back to "Accepted"
- "Delivering" and "Waiting for Pickup" can only transition to "Completed"
- Admin users can override most transition rules with proper justification

### BR3: Permission Rules
- Guest users can only submit and view requests
- Authenticated users can edit/delete only their own requests
- Admin users can manage all requests
- Admin access is restricted to specific Discord User IDs

### BR4: Data Rules
- Guest tracking tokens must be unique and non-guessable
- All status changes must be audited with timestamps
- User data from Discord is stored for authentication purposes only
- Request data is retained indefinitely for historical reference

### BR5: Filament Management Rules
- Filament stock quantities cannot be negative
- Filament types with active requests cannot be deleted
- Filament links must be valid URLs when provided
- Filament photos must be valid image URLs when provided
- Only filaments with stock > 0 are available for new requests

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
- Discord User ID (for authenticated users)
- Discord username
- Discord email (optional)
- Admin status flag
- Account creation timestamp

### DR2: Request Data
- Requester name
- Model URL
- Optional notes
- Delivery preference
- Selected filament ID
- Current status
- Creation and update timestamps
- Associated user (if authenticated)
- Guest tracking token (if guest)

### DR3: Filament Data
- Filament name/identifier
- Material type (PLA, ABS, PETG, etc.)
- Brand
- Colour
- Current stock amount (in grams or meters)
- Optional product link
- Optional photo URL
- Creation and update timestamps

### DR4: Status Data
- Status name
- Status description
- Status history with timestamps
- Admin notes for status changes
- User who made status changes

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
