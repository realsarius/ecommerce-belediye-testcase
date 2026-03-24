# E-Commerce API

## Table of Contents

- [0. Quick Setup](#0-quick-setup)
- [1. Scope](#1-project-scope)
- [2. Technology Stack](#2-technology-stack)
- [3. Database Design](#3-database-design)
- [4. API Design](#4-api-design-and-standards)
- [5. Logging and Error Management](#5-logging-observability-and-error-management)
- [6. Test Strategy](#6-test-strategy)
- [7. Setup and Running](#7-setup-and-running)
- [8. Frontend](#8-frontend)
- [9. Production Notes](#9-production-notes)
- [10. License and Usage Note](#10-license-and-usage-note)
- [Additional Documents](#additional-documents)

## 0. Quick Setup

### With Docker (Recommended)

```bash
# 1) Prepare environment variables
cp .env.example .env

# 2) Start development + logging environment
docker compose --profile dev --profile logging up -d --build

# 3) Start test environment
docker compose --profile test up -d --build

# 4) Stop all
docker compose down
```

### Access URLs

- Frontend: <http://localhost:3000>
- API (Swagger): <http://localhost:5000/swagger>
- pgAdmin: <http://localhost:5050>
- Kibana: <http://localhost:5601>
- Elasticsearch: <http://localhost:9200>

---

## 1. Project Scope

**Authentication and Authorization**: Implemented JWT and Refresh Token-based authentication. Access Tokens are short-lived; Refresh Tokens are stored hashed in the database. Role-based authorization (RBAC) separates Customer, Seller, Support, and Admin permissions at the endpoint level.

**Product Catalog**: Listing, filtering, and pagination are available. Redis Cache is used for frequently accessed data; the relevant cache is invalidated on product add/update operations.

**Admin Product Management**: CRUD operations for Sellers and Admins. Product updates are logged.

**Cart Management**: Cart data is stored on Redis for performance. Database load is minimized using atomic increment/decrement operations with Hash data structure.

**Checkout and Orders**: Order creation takes place within transaction integrity. Stock control, coupon validation, and shipping calculation are performed at order time; the order is created in `PendingPayment` status and redirected to the payment step.

**Payment Integration**: Iyzico (Sandbox) integrated. Repeated payments are prevented with Idempotency Key.

**Stock Management and Consistency**: Redis Distributed Lock (product-based, `product:{id}`) is used for stock consistency in concurrent orders. Race conditions and overselling are prevented for concurrent requests on the same product.

**Wishlist Experience**: The favorites flow has been productized. Price and add-date snapshots, guest wishlist synchronization, collections, shareable wishlists, price alerts, low stock notifications, cursor-based listing, wishlist count synchronization, and bulk add-to-cart from wishlist flows are active.

**Gift Card Flow**: Gift card generation by admin, balance usage with a code at checkout, completing orders without a card when the full amount is covered, and automatically restoring balance on cancellation/refund are available.

**Referral & Loyalty**: Registration with referral code, first-order reward, loyalty point movements, and reward reclaim scenarios are active.

**SEO & Discovery**: `sitemap.xml`, `robots.txt`, canonical management, meta/OG fields on public pages, and JSON-LD structured data generation are available.

**Admin / Seller Dashboard**: Admin and seller panels work with dashboard KPIs, order/product/finance/return/announcement/system health surfaces, and role-based operation flows.

## 2. Technology Stack

| Category | Technology / Library | Purpose |
|---|---|---|
| **Core & Architecture** | .NET 8, Clean Architecture, RESTful API | Modularity, testability, layered architecture |
| **Frontend** | React, Redux Toolkit, Shadcn/ui, Zod, Tailwind CSS, Vite | SPA, state management |
| **Data Access** | Entity Framework Core 8, PostgreSQL 16 | ORM, Migration management |
| **Dependency Injection & AOP** | Autofac | Advanced DI, Interception, Aspect-Oriented Programming (Log, Cache, Validation, Transaction) |
| **Validation** | FluentValidation | Object validation and business rules (AOP integrated) |
| **Caching & Performance** | Redis 7, Distributed Cache | Caching, cart management, and distributed lock |
| **Search & Indexing** | Elasticsearch 8 | Product search, typo tolerance, index sync |
| **Realtime Communication** | SignalR | Live support messaging (customer/support/admin) |
| **Messaging & Event Bus** | RabbitMQ, MassTransit | Async event flow, retry and dead-letter management |
| **Logging & Monitoring** | Serilog, Elasticsearch, Kibana | Structured logging, centralized log management |
| **Auth** | JWT, BCrypt | Token-based authentication and password hashing |
| **DevOps** | Docker, Docker Compose | Containerization and multi-service orchestration |
| **Background Jobs** | Hangfire, PostgreSQL Storage | Scheduled tasks, background processing |
| **Documentation** | Swagger / OpenAPI | API documentation and test interface |
| **Testing** | xUnit, Moq, FluentAssertions | Unit tests, mocking, and fluent assertions |

## 3. Database Design

The database diagram is visualized in Dbdiagram:
> 🔗 **[Live Database Diagram (dbdiagram.io)](https://dbdiagram.io/d/694d9913b8f7d8688620ad62)**

### 3.1 Entity List

1. **Users**: System users.
2. **Roles**: Authorization roles (Customer, Seller, Support, Admin).
3. **SellerProfiles**: Seller profile information.
4. **Products**: Products.
5. **Categories**: Product categories.
6. **Inventories**: Product stock quantities.
7. **InventoryMovements**: Stock change movements (audit log).
8. **Orders**: Order header information.
9. **OrderItems**: Order line items.
10. **Payments**: Payment transactions and results.
11. **ShippingAddresses**: Delivery addresses.
12. **Carts**: User carts.
13. **CartItems**: Cart products.
14. **Coupons**: Discount codes.
15. **CreditCards**: Encrypted card information.
16. **RefreshTokens**: Session renewal keys.
17. **SupportConversations**: Live support conversation headers.
18. **SupportMessages**: Live support message records.
19. **Wishlists**: Favorite list root record belonging to the user.
20. **WishlistCollections**: Multiple favorite lists / collections.
21. **WishlistItems**: Product records added to favorites and price snapshot information.
22. **PriceAlerts**: Wishlist alert records based on target price.
23. **GiftCards**: Gift card records that can be linked to users and track balance.
24. **GiftCardTransactions**: Gift card creation, usage, and return movements.
25. **InvoiceInfos**: Individual/corporate invoice information per order.
26. **ReturnRequestAttachments**: Photo/file records attached to return requests.
27. **InboxMessages**: Consumer idempotency and dedupe records.
28. **OutboxMessages (App)**: Application outbox event records.
29. **MassTransit Outbox/Inbox State Tables**: Transactional event publish infrastructure (`InboxState`, `OutboxMessage`, `OutboxState`).

### 3.2 Migration and Schema Management

**Entity Framework Core Code-First** methodology is used.

- Changes are made on the code side (Entities).
- Versioned migrations are created with `dotnet ef migrations add [MigrationName]`.
- The database stays in sync with code across all environments.

## 4. API Design and Standards

Consistency, predictability, and observability are prioritized.

### 4.1 Endpoint Standards

All endpoints follow RESTful principles and a versioning strategy has been adopted.

- **Base URL:** `/api/v1/{resource}` (e.g., `/api/v1/products`, `/api/v1/orders`)
- **HTTP Methods:** GET, POST, PUT, DELETE, PATCH conform to standards.
- **Versioning Rule:** New public API versions progress path-based (`/api/v2/...`). No breaking changes inside `v1`; changes that break backward compatibility are opened under a new version.
- **Audit Result:** Existing controller routes were scanned; no endpoint deviating from the `/api/v1` standard was found on the active HTTP surface.

### 4.2 Response and Error Model

All responses are in a standard structure; frontend integration is simplified.

**Successful Responses (Success):**

```json
{
  "success": true,
  "message": "Operation successful",
  "data": { }
}
```

**Error Responses (Error):**
All errors are caught by a central Middleware and returned in a single format.

```json
{
  "traceId": "0HLQ8...",
  "errorCode": "INSUFFICIENT_STOCK",
  "message": "The requested stock quantity is not available.",
  "details": {
    "productId": 123,
    "requested": 5,
    "available": 2
  }
}
```

### 4.3 Pagination

Pagination is standard for endpoints returning lists.

- **Request:** `?page=1&pageSize=10`
- **Response Metadata:**

    ```json
    {
      "items": [],
      "page": 1,
      "pageSize": 10,
      "totalCount": 150,
      "totalPages": 15,
      "hasPreviousPage": false,
      "hasNextPage": true
    }
    ```

### 4.4 Swagger & OpenAPI

All endpoints can be tested interactively via Swagger UI. JWT Auth integration is available.

### 4.5 Elasticsearch Product Search

Product search works through Elasticsearch. Endpoint:

- `GET /api/v1/search/products?q=&categoryId=&minPrice=&maxPrice=&page=&pageSize=`

Features:

- Pagination and filter support
- Partial match (prefix) and typo tolerance (fuzzy)
  Example: `q=adida` query can return `Adidas` products
- Index sync after product `create/update/delete`
- DB fallback search if Elasticsearch is unreachable

Example request:

```bash
curl "http://localhost:5000/api/v1/search/products?q=adida&page=1&pageSize=10"
```

### 4.6 Wishlist API Highlights

The wishlist flow goes beyond standard CRUD and includes event-driven, UX-focused additional contracts:

- `GET /api/v1/wishlists?cursor=&limit=&collectionId=`: cursor-based favorite listing and collection filter
- `POST /api/v1/wishlists/items`: add product to favorites
- `PATCH /api/v1/wishlists/items/{productId}/collection`: move product between collections
- `GET /api/v1/wishlists/collections`: list user's collections
- `POST /api/v1/wishlists/collections`: create a new collection
- `POST /api/v1/wishlists/add-all-to-cart`: bulk add eligible wishlist items to cart
- `GET/POST/DELETE /api/v1/wishlists/share`: manage sharing settings
- `GET /api/v1/wishlists/share/{shareToken}`: read publicly shared wishlist
- `GET/PUT/DELETE /api/v1/wishlists/price-alerts`: price alert management

These flows work with Redis rate limiting, audit log, wishlist count synchronization, and MassTransit event publish chain.

### 4.7 Payment Webhook Semantics and Retry Policy

Webhook endpoint:

- `POST /api/v1/payments/webhook`
- Required header: `X-IYZ-SIGNATURE-V3`

Canonical signature payload format:

- Direct payment event: `secretKey + iyziEventType + paymentId + paymentConversationId + status`
- HPP payment event: `secretKey + iyziEventType + iyziPaymentId + token + paymentConversationId + status`
- Signature verification is done with whichever of these two formats is appropriate; if one of the required fields is missing, the request is rejected.

HTTP response semantics:

- `200 OK`: Event was successfully processed or an idempotent previously-processed event arrived again (`duplicate`).
- `400 Bad Request`: A required field like `conversationId` is missing.
- `401 Unauthorized`: Signature is missing/invalid.
- `404 Not Found`: Related order/payment record not found.
- `422 Unprocessable Entity`: State rejected at the business rule level.
- `500 Internal Server Error`: Unexpected error.

Retry rule (operational contract):

- `2xx` responses are treated as terminal success; the provider should not retry.
- `4xx` responses should be treated as permanent errors caused by payload/authentication issues.
- `5xx` responses are treated as transient errors; the provider retry strategy should kick in here.

Log security:

- Potentially sensitive fields are masked in webhook logs with `SensitiveDataLogSanitizer`.
- Fields like `secretKey`, `token`, `card` are not printed as plain text in logs.

OpenTelemetry meter name for webhook observability: `EcommerceAPI.PaymentWebhook`, counter: `payment_webhook_events_total`.

## 5. Logging, Observability, and Error Management

### 5.1 Structured Logging (Serilog + Elasticsearch)

Structured logging is set up with **Serilog**. Logs are in JSON format. Centralized log management is provided with Elasticsearch + Kibana integration.

### 5.2 Correlation ID / Trace ID (Observability)

- A unique `X-Correlation-Id` is assigned to each HTTP request.
- This ID is injected into Serilog LogContext and stamped onto all logs during that request.
- Also added to response headers so it can be tracked by the client.

### 5.3 Global Exception Handler

All error management is in the central `ExceptionHandlingMiddleware`:

- Different exception types (`NotFoundException`, `InsufficientStockException`, `ValidationException`, `BusinessException`) are caught and returned with the appropriate HTTP Status Code and structured error body.
- The Correlation ID through which the error can be traced is communicated via `traceId`.
- Unexpected errors are logged and a general message that does not leak sensitive information is returned to the client.

### 5.4 Audit Log (Critical Business Flows)

Operations such as Stock Changes are recorded with an audit log for business process observability.

### 5.5 Wishlist Analytics and Kibana

Structured analytics fields were added to facilitate Kibana dashboard setup for the wishlist feature set. The following flows are now logged with a common analytics language:

- wishlist add / remove events
- bulk add-to-cart from favorites and skipped products
- price alert triggering and delivery
- low stock notification delivery

Core log fields:

- `AnalyticsStream`
- `AnalyticsEvent`
- `FunnelStage`
- `NotificationChannel`
- `UserId`
- `WishlistId`
- `ProductId`
- `ProductName`
- `Category`
- `PriceAtTime`
- `Currency`
- `WishlistCount`
- `RequestedCount`
- `AddedCount`
- `SkippedCount`
- `Reason`
- `TargetPrice`
- `OldPrice`
- `NewPrice`
- `StockQuantity`
- `Threshold`
- `OccurredAt`

For detailed wishlist architecture and Kibana panel recommendations:

- [Wishlist Feature Status](docs/wishlist-feature-status.md)

## 6. Test Strategy

Comprehensive tests were written to guarantee code reliability and correctness of business rules.

### 6.1 Unit Tests

xUnit, Moq, and FluentAssertions were used.

### 6.2 Integration Tests

E2E tests were written with the **WebApplicationFactory** infrastructure. In-memory database and test containers were used.

### 6.3 Test Commands

```bash
# Run all tests
dotnet test

# Run only Unit tests
dotnet test --filter "FullyQualifiedName~UnitTests"

# Run only Integration tests
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

## 7. Setup and Running

### 7.1 Requirements

[Docker & Docker Compose](https://docs.docker.com/compose/)

### 7.2 Environment Variables

Environment variables are defined in the `.env.example` file.

```bash
cp .env.example .env
```

Edit the `.env` file and fill in the following values:

```bash
# Database
DATABASE_DEV_NAME=ecommerce_dev
DATABASE_DEV_USER=postgres
DATABASE_DEV_PASSWORD=yourpassword
DATABASE_DEV_PORT=5432

# JWT Auth
JWT_SECRET_KEY=
JWT_ISSUER=
JWT_AUDIENCE=
JWT_EXPIRATION_MINUTES=

# Iyzico Payment (Sandbox)
IYZICO_API_KEY=sandbox-xxx
IYZICO_SECRET_KEY=sandbox-xxx
IYZICO_BASE_URL=https://sandbox-api.iyzipay.com

# Security
ENCRYPTION_KEY=
HASH_PEPPER=

# Redis
REDIS_CONNECTION_STRING=localhost:6379
```

### 7.3 Running with Docker Compose (Recommended)

You can start all services (API, PostgreSQL, Redis, Frontend) with a single command:

```bash
# Start development environment
docker compose --profile dev --profile logging up -d

# Start test environment
docker compose --profile test up -d

# Stop all services
docker compose --profile dev --profile test --profile logging down
```

**Service Access URLs (Dev):**

| Service | Port | URL |
|--------|------|-----|
| Frontend | 3000 | <http://localhost:3000> |
| API (Swagger) | 5000 | <http://localhost:5000/swagger> |
| pgAdmin | 5050 | <http://localhost:5050> |
| PostgreSQL | 5432 | - |
| Redis | 6379 | - |
| RabbitMQ AMQP | 5672 | - |
| RabbitMQ Management | 15672 | <http://localhost:15672> |
| Kibana (logging profile) | 5601 | <http://localhost:5601> |
| Elasticsearch (logging profile) | 9200 | <http://localhost:9200> |
| Hangfire Dashboard | 5000 | <http://localhost:5000/hangfire> |

### 7.4 Manual Setup

**Requirements:** .NET 8 SDK, PostgreSQL 16, Redis 7, Node.js 22 (LTS recommended)

```bash
# 1. Install dependencies
dotnet restore

# 2. Apply database migrations
dotnet ef database update --project EcommerceAPI.DataAccess --startup-project EcommerceAPI.API

# 3. Run the API
cd EcommerceAPI.API
dotnet run

# 4. Run the Frontend
cd frontend
npm install
npm run dev
```

### 7.5 Seed Data (Sample Data)

When the application is started in **Development** mode, the `EcommerceAPI.Seeder` layer reads JSON files in the [seed-data/](seed-data) folder and loads them into the database.

JSON files: 10 categories, 100+ products, stock records ([seed-data/](seed-data))

Created by code: 4 roles, 4 test users ([SeedRunner](EcommerceAPI.Seeder/SeedRunner.cs))

**User Passwords**

| Email | Password |
|-------|----------|
| `testadmin@test.com` | `Test123!` |
| `testseller@test.com` | `Test123!` |
| `customer@test.com` | `Test123!` |
| `support@test.com` | `Test123!` |

### 7.5.1 Frontend Container Note

The frontend dev container automatically syncs dependencies when `package-lock.json` changes. When a new package is added or a Vite import error is seen, the following command is sufficient:

```bash
docker compose --profile dev --profile logging up -d --build frontend-dev
```

Especially for packages added later like `recharts`, `@dnd-kit/*`, when `node_modules` inside the container falls behind, sync is restored with this command.

### 7.6 Payment Provider (Iyzico Sandbox)

Iyzico Sandbox integration is implemented in the project. No real money flow exists.

For test cards: [iyzico/iyzipay-dotnet](https://github.com/iyzico/iyzipay-dotnet)

Iyzico Docs: <https://docs.iyzico.com/on-hazirliklar/sandbox>

### 7.7 Sample Usage Flow (cURL)

cURL commands showing a complete e-commerce flow:

```bash
# 0. User Registration
curl -X POST "http://localhost:5000/api/v1/auth/register" \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@test.com","password":"Demo123!","firstName":"Demo","lastName":"User"}'

# 1. User Login → Get Token
curl -X POST "http://localhost:5000/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@test.com","password":"Demo123!"}'
# Copy the "token" value from the response

# 2. List Products
curl "http://localhost:5000/api/v1/products?page=1&pageSize=10"

# 3. Add Product to Cart (Token required)
curl -X POST "http://localhost:5000/api/v1/cart/items" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{"productId":103,"quantity":1}'

# 4. Checkout - Create Order (Token required)
curl -X POST "http://localhost:5000/api/v1/orders" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{"shippingAddress":"Sample District, Test Street No:1, Istanbul","paymentMethod":"CreditCard"}'
# Get "orderId" from the response

# 5. Make Payment (Token required)
curl -X POST "http://localhost:5000/api/v1/payments" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{"orderId":<ORDER_ID>,"cardNumber":"5406670000000009","cardHolderName":"Demo User","expiryDate":"12/26","cvv":"123"}'

# 6. List Orders (Token required)
curl "http://localhost:5000/api/v1/orders" \
  -H "Authorization: Bearer <TOKEN>"
```

### 7.8 Swagger UI

API documentation: `http://localhost:5000/swagger`

### 7.9 Postman Collection

Postman collection file: `postman/EcommerceAPI.postman_collection.json`
Total content: **198 requests / 41 folders** (last updated: 2026-03-06)

**Collection Contents:**

| Folder | Description |
|--------|-------------|
| Auth | Register/Login/Refresh/Revoke/Me + Social Login + Email Verify/Code + Password Reset flows |
| Account | Email change endpoint |
| Products / Categories / Cart / Orders / Payments | Public and customer surfaces |
| Wishlists | Collection, sharing, add/remove, add-all-to-cart, price-alert management |
| Notifications | User notifications + admin template management |
| Shipping Addresses / Credit Cards / Coupons | User checkout helper flows |
| Seller Profile | Seller profile create / update / delete |
| Admin - Dashboard / Products / Categories / Orders | Admin panel core operations |
| Admin - Returns / Sellers / Finance / Users | Return, seller, revenue, and user management |
| Admin - Data Migration | One-time admin migration endpoints |
| Admin - Announcements / System | Announcement management and system health endpoints |
| Seller - Dashboard / Products / Orders | Seller panel core operation surface |
| Seller - Finance / Reviews | Seller analytics, finance, and review management |
| Gift Cards / Referrals & Loyalty | Loyalty, referral, and gift card flows |
| Campaigns / Media | Campaign interactions and R2 media management |
| Reviews (Public & Admin) | Product reviews + admin moderation flows |
| Search / Support (Live Chat) | Search (including suggestions) and live support endpoints |
| Full E-Commerce Flow | End-to-end test scenario |

## 8. Frontend

React 19 + TypeScript based SPA. Folder structure:

```
frontend/src/
├── components/    # UI components (Shadcn/ui)
├── features/      # Redux slices (auth, cart, orders, products...)
├── pages/         # Page components
├── hooks/         # Custom React hooks
└── types/         # TypeScript types
```

**Pages:** Home, Login, Register, Cart, Checkout, Orders, ProductDetail, Account, Addresses, CreditCards, Loyalty, GiftCards, Referrals, Notifications, Wishlist

**Admin Panel:** Dashboard, user management, product/category/order/return management, seller operations, finance, coupon/campaign, gift card, review moderation, announcements, support, and system health

**Seller Panel:** Dashboard, product add/edit, multi-image/variant management, stock update, order shipping, finance, review reply, and store profile

## 9. Production Notes

### SignalR Scale Strategy

The live support module works directly with SignalR in single-instance operation.
If multiple API instances or replicas will be run, the Redis backplane must be enabled.

Configuration:

- `SIGNALR_REDIS_BACKPLANE_ENABLED=true`
- `SIGNALR_CHANNEL_PREFIX=ecommerce-prod`
- `REDIS_CONNECTION_STRING=redis:6379`

Rationale:

- Redis is already in the system for cache, distributed lock, and rate limiting
- Therefore, Redis backplane is the lowest operational cost solution for the first scaling step
- Managed SignalR service can be evaluated later if needed

Operational notes:

- All API instances must connect to the same Redis
- Channel prefix must be separated between environments (`ecommerce-dev`, `ecommerce-prod`)
- In multi-replica scenarios, reverse proxy and WebSocket timeout settings should be reviewed separately

### 9.1 RabbitMQ + Elasticsearch Sync Flow

- Search index sync for product `create/update/delete/stock` operations works through event publish instead of direct calls.
- The API places an event in the `product-index-sync` queue; the consumer applies `Upsert/Delete` on the Elasticsearch side.
- After retry (3 attempts, 2-second interval), failed messages fall into the `_error` queue.

### 9.2 Operational Checks

Basic health checks:

```bash
curl -fsS http://localhost:5000/health/ready >/dev/null
curl -fsS "http://localhost:5000/api/v1/search/products?q=test&page=1&pageSize=5" >/dev/null
curl -fsS "http://localhost:5000/api/v1/search/suggestions?q=ad&limit=5" >/dev/null
docker exec ecommerce-rabbitmq rabbitmqctl list_queues -p /ecommerce name messages consumers
```

Expected:

- `health/ready` returns successfully
- search and suggestion endpoints return 200
- `product-index-sync` queue has `consumers > 0`
- No continuous increase in `_error` queues

For Swagger validation in development or staging environments:

```bash
curl -fsS http://localhost:5000/swagger/index.html >/dev/null
```

You can also run the smoke script directly for quick validation after the application starts:

```bash
./scripts/ci/run_api_smoke.sh
```

To skip the payment collection step while keeping the checkout order creation step in environments without Iyzico secrets:

```bash
SMOKE_INCLUDE_PAYMENT_FLOW=false ./scripts/ci/run_api_smoke.sh
```

For production smoke:

```bash
API_BASE_URL=http://localhost:5001 SMOKE_EXPECT_SWAGGER=false ./scripts/ci/run_api_smoke.sh
```

### 9.3 Critical Environment Variables

- `RABBITMQ_HOST`, `RABBITMQ_PORT`, `RABBITMQ_VHOST`
- `RABBITMQ_USER`, `RABBITMQ_PASSWORD`
- `ELASTICSEARCH_URL`
- `JWT_SECRET_KEY`

For controlled error generation for testing purposes:

- `PRODUCT_INDEX_SYNC_FORCE_FAIL` (must remain `false` in Production)

### 9.4 Deployment Readiness Checklist

For env/secrets validation before deploy, post-deploy smoke, and rollback verification steps:

- [`docs/deployment-readiness-checklist.md`](docs/deployment-readiness-checklist.md)

### 9.5 Rollback Runbook

Most frequently referenced during operations:

- [Operational Checks](#92-operational-checks)
- [`docs/deployment-readiness-checklist.md`](docs/deployment-readiness-checklist.md)
- [`scripts/ci/run_api_smoke.sh`](scripts/ci/run_api_smoke.sh)
- [`scripts/ci/run_api_perf_smoke.sh`](scripts/ci/run_api_perf_smoke.sh)
- [Observability and Logging](#5-logging-observability-and-error-management)
- [`observability/prometheus-alerts.yml`](observability/prometheus-alerts.yml)
- [CI/CD Pipeline](.github/workflows/main.yml)

Practical rollback approach:

1. If there is a regression after the last deploy, first clarify the affected area (`search`, `support`, `checkout`, `payment`).
2. Verify the health and smoke status of the current release.
3. If the issue comes from recent changes, roll back to the previous stable image/tag.
4. After rollback, run `health/ready`, basic smoke, and queue checks again.
5. Do not consider the rollback complete until `_error` queue, latency, and log flow return to normal.

### 9.6 Backup / Restore Plan

Backup/restore steps and disaster scenario for PostgreSQL, Redis, and RabbitMQ:

- [`docs/backup-restore-plan.md`](docs/backup-restore-plan.md)

### 9.7 Incident Response (Short Flow)

1. **Isolate the area first**: Is the issue with `search`, `support`, `checkout`, `payment`, or general API access? Clarify this first.
2. **Verify basic health**: Run `health/ready`, Swagger (development/staging) if needed, and basic smoke calls.
3. **Check dependencies**:
   - On `search` side: Elasticsearch and `product-index-sync`
   - On `support` side: SignalR/RabbitMQ/Redis
   - On `checkout` side: PostgreSQL, Redis, and payment flow
4. **Contain the impact**: If the issue is consumer, external service, or last-deploy-related, temporarily restrict the relevant flow or rollback if necessary.
5. **Re-verify after fix**: Just returning 200 from the endpoint is not enough; smoke, queue, and alert sides should also return to normal.
6. **Leave a note when closing**: Produce a short incident summary. Write the root cause, impact duration, and action to prevent recurrence to the backlog.

Recommended quick check commands:

```bash
curl -fsS http://localhost:5000/health/ready
curl -fsS "http://localhost:5000/api/v1/search/products?q=test&page=1&pageSize=5"
curl -fsS "http://localhost:5000/api/v1/search/suggestions?q=ad&limit=5"
docker exec ecommerce-rabbitmq rabbitmqctl list_queues -p /ecommerce name messages consumers
./scripts/ci/run_api_smoke.sh
```

## 10. License and Usage Note

This repository is not licensed as open source.

- Source code and related materials are protected under `All Rights Reserved`.
- Copying, reusing, distributing, creating derivative works from, or using the code in a production environment without written permission is prohibited.
- Third-party packages used are subject to their own license terms.

For the detailed text, see the [`LICENSE`](LICENSE) file in the root directory.

## Additional Documents

- [Product Roadmap](docs/product-roadmap.md)
- [Wishlist Feature Status](docs/wishlist-feature-status.md)
- [Wishlist Kibana Dashboard Setup](docs/wishlist-kibana-dashboard-setup.md)
- [Wishlist Smoke Checklist](docs/wishlist-smoke-checklist.md)
- [Backup Restore Plan](docs/backup-restore-plan.md)
- [Deployment Readiness Checklist](docs/deployment-readiness-checklist.md)
- [Rollback Runbook](docs/rollback-runbook.md)
