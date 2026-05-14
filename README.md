# Multi-Tenant Inventory System

A secure, multi-tenant inventory management API built with ASP.NET Core 8. Multiple businesses can register, authenticate, and manage their inventory with complete data isolation between tenants.

## Features

- **Multi-tenant architecture** with automatic tenant isolation
- **JWT authentication** with tenant-scoped tokens
- **Global query filters** ensuring data cannot leak between tenants
- **Full inventory CRUD operations** (Create, Read, Update, Delete products)
- **Request validation** with data annotations
- **API documentation** via Swagger/OpenAPI
- **Health check endpoint** for monitoring
- **Integration tests** verifying tenant isolation

## Architecture

### Tenant Isolation Strategy

The system implements a robust tenant isolation strategy using EF Core global query filters:

1. **Tenant Resolution Middleware**: On each request, the `TenantResolutionMiddleware` extracts the `tenant_id` claim from the authenticated user's JWT token and stores it in a scoped `ITenantContext` service.

2. **Global Query Filter**: The `Product` entity is configured with a `HasQueryFilter` that automatically appends `WHERE TenantId = @currentTenant` to every database query. This ensures that even if a developer forgets to filter by tenant, the data remains isolated.

```csharp
entity.HasQueryFilter(p => _tenantContext.TenantId == null || p.TenantId == _tenantContext.TenantId);
```

3. **Token-based Tenant Identity**: The JWT token contains a `tenant_id` claim, binding the user's session to their tenant. This claim is validated on every request.

### Project Structure

```
├── Controllers/
│   └── ProductsController.cs     # Product CRUD endpoints
├── Data/
│   └── AppDbContext.cs           # EF Core context with tenant filters
├── DTOs/
│   ├── CreateProductRequest.cs
│   ├── UpdateProductRequest.cs
│   ├── ProductResponse.cs
│   ├── RegisterRequest.cs
│   ├── RegisterResponse.cs
│   ├── LoginRequest.cs
│   └── LoginResponse.cs
├── Filters/
│   └── ValidationFilter.cs       # Request validation
├── Middleware/
│   ├── TenantResolutionMiddleware.cs
│   └── ExceptionHandlingMiddleware.cs
├── Migrations/
│   └── ...                       # EF Core migrations
├── Models/
│   ├── Tenant.cs
│   ├── User.cs
│   └── Product.cs
├── Services/
│   ├── ITenantContext.cs
│   ├── TenantContext.cs
│   ├── IPasswordHasher.cs
│   ├── PasswordHasher.cs
│   ├── ITokenService.cs
│   └── JwtTokenService.cs
├── Tests/
│   └── ...                       # Unit and integration tests
└── Program.cs                    # Application entry point
```

## Setup Instructions

### Prerequisites

- .NET 8 SDK or later
- SQLite (included via NuGet)

### Getting Started

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd multi-tenant-inventory-system
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Apply database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

   The API will be available at `https://localhost:5001` (or `http://localhost:5000`).

5. **Access Swagger UI** (Development mode)

   Navigate to `https://localhost:5001/swagger` to explore the API documentation.

### Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=inventory.db"
  },
  "Jwt": {
    "SecretKey": "YourSecretKeyHere",
    "Issuer": "multi-tenant-inventory-system",
    "Audience": "multi-tenant-inventory-system-users",
    "ExpirationMinutes": 60
  }
}
```

> **Important**: Change the JWT `SecretKey` to a secure value in production. Use environment variables or user secrets for sensitive configuration.

## API Endpoints

### Authentication

#### Register a New Tenant and User

```http
POST /api/register
Content-Type: application/json

{
  "tenantName": "Acme Corporation",
  "email": "admin@acme.com",
  "password": "SecurePass123!"
}
```

**Response (201 Created):**
```json
{
  "tenantId": "guid",
  "userId": "guid",
  "email": "admin@acme.com",
  "tenantName": "Acme Corporation"
}
```

#### Login

```http
POST /api/login
Content-Type: application/json

{
  "email": "admin@acme.com",
  "password": "SecurePass123!"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": "guid",
  "email": "admin@acme.com",
  "tenantId": "guid"
}
```

### Products (Requires Authentication)

All product endpoints require the `Authorization: Bearer <token>` header.

#### Create Product

```http
POST /api/products
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Widget Pro",
  "sku": "WGT-PRO-001",
  "stockCount": 100
}
```

**Response (201 Created):**
```json
{
  "id": "guid",
  "tenantId": "guid",
  "name": "Widget Pro",
  "sku": "WGT-PRO-001",
  "stockCount": 100,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

#### Get All Products

```http
GET /api/products
Authorization: Bearer <token>
```

Returns all products belonging to the authenticated tenant.

#### Get Product by ID

```http
GET /api/products/{id}
Authorization: Bearer <token>
```

#### Update Product

```http
PUT /api/products/{id}
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "Widget Pro Max",
  "sku": "WGT-PRO-002",
  "stockCount": 150
}
```

#### Delete Product

```http
DELETE /api/products/{id}
Authorization: Bearer <token>
```

**Response (204 No Content)**

### Health Check

```http
GET /health
```

**Response (200 OK):**
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": null,
      "duration": 5.123
    }
  ],
  "totalDuration": 5.456
}
```

## Validation Rules

### Registration
- `tenantName`: Required, 1-100 characters
- `email`: Required, valid email format
- `password`: Required, 6-100 characters

### Login
- `email`: Required, valid email format
- `password`: Required

### Products
- `name`: Required, 1-200 characters
- `sku`: Required, 1-100 characters
- `stockCount`: 0 or greater

## Testing

### Run All Tests

```bash
dotnet test
```

### Run Tests with Verbosity

```bash
dotnet test --verbosity normal
```

### Test Coverage

The test suite includes:

- **Unit tests** for password hashing and JWT token generation
- **Validation tests** for request DTOs
- **Integration tests** verifying:
  - Tenant A cannot see Tenant B's products
  - Tenant A cannot get Tenant B's product by ID
  - Tenant A cannot update Tenant B's product
  - Tenant A cannot delete Tenant B's product
  - Each tenant can fully manage their own products
  - Unauthenticated requests are rejected

## Error Handling

The API uses a global exception handling middleware that returns consistent error responses:

```json
{
  "error": "An unexpected error occurred",
  "traceId": "abc123"
}
```

In development mode, additional details are included:
```json
{
  "error": "An unexpected error occurred",
  "traceId": "abc123",
  "detail": "Exception message",
  "stackTrace": "..."
}
```

### HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | Success |
| 201 | Created |
| 204 | No Content (successful delete) |
| 400 | Bad Request (validation error) |
| 401 | Unauthorized (missing or invalid token) |
| 404 | Not Found |
| 409 | Conflict (e.g., duplicate email) |
| 500 | Internal Server Error |

## Security Considerations

1. **JWT tokens** expire after the configured duration (default: 60 minutes)
2. **Passwords** are hashed using BCrypt before storage
3. **Tenant isolation** is enforced at the database query level
4. **HTTPS** is enforced in production

## Technology Stack

- **Framework**: ASP.NET Core 8
- **Database**: SQLite with Entity Framework Core
- **Authentication**: JWT Bearer tokens
- **Password Hashing**: BCrypt
- **API Documentation**: Swagger/OpenAPI (Swashbuckle)
- **Testing**: xUnit with WebApplicationFactory
