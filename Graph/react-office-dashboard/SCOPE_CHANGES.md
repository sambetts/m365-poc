# Dynamic Authentication Scopes Implementation

## Overview
The authentication system has been updated to allow users to dynamically select which Microsoft Graph API scopes/permissions they want to grant before signing in, rather than using hard-coded scopes.

## Changes Made

### 1. **authConfig.js** - Updated Configuration
- Renamed `scopes` to `defaultScopes` to clarify these are defaults, not hard-coded requirements
- Added `availableScopes` array with 11 common Microsoft Graph scopes, each with:
  - `value`: The actual scope string
  - `label`: User-friendly name
  - `description`: What the permission allows

Available scopes include:
- User profile (User.Read, User.ReadBasic.All)
- Chat (Chat.Read, Chat.ReadWrite)
- Mail (Mail.Read, Mail.ReadBasic, Mail.ReadWrite)
- Files (Files.Read, Files.ReadWrite)
- Calendar (Calendars.Read, Calendars.ReadWrite)

### 2. **ScopeSelector.tsx** - New Component
Created a new UI component that:
- Accepts the MSAL instance as a prop
- Displays all available scopes with checkboxes
- Pre-selects the default scopes (User.Read, Chat.Read, Mail.ReadBasic)
- Shows descriptions for each permission
- Validates that at least one scope is selected
- Displays the currently selected scopes
- **Triggers authentication immediately** when user clicks "Sign in with selected permissions"
- Calls back to parent component to save the selected scopes

### 3. **App.tsx** - Direct Authentication Flow
Updated the main App component to:
- Import `defaultScopes` instead of `scopes`
- Add state for `selectedScopes` (initialized with defaults)
- Add `handleScopesSelected` callback to save selected scopes from ScopeSelector
- Pass both `msalInstance` and callback to ScopeSelector
- Pass `selectedScopes` to `ExampleAppGraphLoader` after authentication
- Simplified flow: ScopeSelector directly triggers login, no intermediate screen
- Removed SignInButton component usage (authentication now happens directly from ScopeSelector)

### 4. **SignInButton.tsx** - Still Available
This component still exists and works with dynamic scopes, but is no longer used in the main flow. The ScopeSelector now handles authentication directly for a streamlined user experience.

## User Flow

1. **Initial Load (Unauthenticated)**
   - User sees the ScopeSelector component
   - Default scopes are pre-selected (User.Read, Chat.Read, Mail.ReadBasic)
   - User can check/uncheck scopes as desired

2. **Sign In with Selected Permissions**
   - User clicks "Sign in with selected permissions"
   - Selected scopes are saved to App state
   - MSAL authentication popup is triggered immediately with the selected scopes
   - User consents to the permissions in the Azure AD popup

3. **Authenticated State**
   - After successful authentication, the app loads with the GraphLoader configured with the selected scopes
   - AppMainContent is displayed with access to the granted permissions

## Benefits

1. **User Control**: Users can choose exactly which permissions to grant
2. **Principle of Least Privilege**: Users can grant minimal permissions needed
3. **Transparency**: Clear descriptions of what each permission allows
4. **Flexibility**: Easy to add new scopes to the available list
5. **Better UX**: Users understand what they're consenting to

## Testing

To test the changes:
1. Start the application (unauthenticated)
2. Verify the scope selector appears with default scopes selected
3. Try selecting/deselecting different scopes
4. Click "Sign in with selected permissions"
5. Verify the Azure AD authentication popup appears immediately
6. Complete authentication in the popup
7. Verify the app loads with the selected scopes and displays the main content

## Future Enhancements

Possible improvements:
- Save user's scope preferences to localStorage
- Group scopes by category (Mail, Calendar, Files, etc.)
- Show which scopes are required vs optional for app functionality
- Add scope validation based on app features being used
- Display warnings if insufficient scopes are selected for certain features
