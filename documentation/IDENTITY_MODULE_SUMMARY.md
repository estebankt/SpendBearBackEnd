# Identity Module - Implementation Summary

**Status:** ✅ Complete
**API Status:** Running on http://localhost:5109/api/identity

---

## 📦 Overview
The Identity module manages user registration and profile information, integrating with Auth0 for authentication while maintaining local user data.

## 🚀 API Endpoints

### 1. Register User
`POST /api/identity/register`
- **Purpose**: Creates a new user record in the local database after successful Auth0 authentication.
- **Authentication**: Required (Auth0 JWT).
- **Request Body**:
```json
{
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe"
}
```
- **Response**: `200 OK` with `UserId`.

### 2. Get Profile
`GET /api/identity/me`
- **Purpose**: Retrieves the authenticated user's profile information.
- **Authentication**: Required (Auth0 JWT).
- **Response**: `200 OK`
```json
{
  "id": "uuid",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "createdAt": "2026-02-01T12:00:00Z",
  "lastLoginAt": "2026-02-06T10:00:00Z"
}
```

## 🏗️ Domain Logic
- **User Aggregate**: The core aggregate root for the module.
  - Validates `Auth0UserId` and `Email` during creation.
  - Automatically records registration timestamp.
  - Tracks `LastLoginAt` via `RecordLogin()` method.
- **Domain Events**:
  - `UserRegisteredEvent`: Raised when a new user joins the system.

## 🛠️ Infrastructure
- **Schema**: `identity`
- **Persistence**: `IdentityDbContext` using EF Core.
- **UserRepository**: Handles storage and retrieval of User aggregates.

## ✅ Implementation Details
- **CQRS**: Implemented using vertical slices (Features/RegisterUser, Features/GetProfile).
- **Middleware Integration**: Works with `UserResolutionMiddleware` to map Auth0 claims to local user context.
- **Development Support**: `DevelopmentAuthMiddleware` allows testing these endpoints in development without valid Auth0 tokens.
