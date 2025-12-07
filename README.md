# GarageUnderground – Copilot Instructions

## Project Overview
GarageUnderground is a .NET 10 Blazor WebAssembly Progressive Web App (PWA) designed to track vehicle maintenance operations by license plate.

The application will store and retrieve:
- Date of intervention
- Description and list of actions performed
- Price
- Payment status
- Vehicle license plate

The app must be simple, fast, and mobile-friendly.

## Technology Stack
The project uses:
- .NET 10
- Blazor WebAssembly
- PWA features
- LiteDB as embedded database
- .NET Aspire as orchestrator
- Deployment on Azure Container Apps

## Aspire (Very Important)
The project must adopt .NET Aspire for orchestration, developer inner-loop, system composition, and deployment consistency.

Key principles:
- Components must be modeled explicitly in Aspire
- Infrastructure, app services, and database must be declared as Aspire resources
- Aspire should handle local orchestration and future cloud deployment pipeline

Goals:
- Simplify distributed application configuration
- Ensure reproducible deployments
- Enable local orchestration of services
- Reduce configuration drift between local and production

General guidance:
- Favor simple Aspire resource definitions
- Use opinionated defaults unless overridden intentionally
- Avoid over-engineering Aspire configurations
- Container image builds must be compatible with Aspire deployment model

## Architecture Guidelines
- Use clean architecture and separation of concerns
- Logical structure:

  /Client
  /Server
  /Shared
  /Data
  /Models
  /Services
  /Components
  /Aspire

- Keep components small, focused, and testable
- Use dependency injection consistently

## Database
- Use LiteDB as embedded database
- Create strongly-typed collections for domain models
- Index license plate field
- Persistent local storage for offline-first support
- Avoid artificial async wrappers around LiteDB

## Models

Example:

public class MaintenanceRecord
{
    public ObjectId Id { get; set; }
    public string LicensePlate { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public bool IsPaid { get; set; }
}

## Coding Style and Conventions

### Naming Rules (Mandatory)
Follow standard C# conventions.
Do not use underscores "_" as prefix for private fields or methods.

Correct:
private int counter;

Incorrect:
private int _counter; // prohibited

Rules:
- Classes: PascalCase
- Methods: PascalCase
- Public fields: PascalCase
- Private fields: camelCase
- Parameters: camelCase
- Locals: camelCase
- No Hungarian notation
- No underscores for member names
- Prefer explicit, descriptive names

### Code Quality
- Minimal, clean, robust
- Avoid premature abstraction
- Avoid magic values
- Write tests when feasible

## Blazor Guidelines
- Razor components should be small and focused
- Use components to encapsulate UI logic
- Dependency injection for services
- Minimal JS interop; only when necessary

## PWA Guidelines
- Offline-first UX
- Persistent data
- Simple service worker
- Gradual enhancement, not complexity
- Data must survive reinstall/upgrade

## UI/UX
- Mobile-first
- Fast and minimal UI
- Neutral aesthetic
- Dark theme optional

## Containerization
- Final deployment will be as containerized app in Azure Container Apps
- Aspire must be able to orchestrate containers
- Multi-stage Docker builds
- Small image footprint


Example (placeholder):

FROM mcr.microsoft.com/dotnet/sdk:10 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GarageUnderground.Server.dll"]

## Security
- No authentication required for MVP
- Validate input
- Prevent database corruption

## Performance Goals
- Fast startup (<= 2s mobile)
- Low memory footprint
- Local caching
- No unnecessary HTTP calls

## MVP Features
- Create/edit/delete maintenance record
- Filter by license plate
- Search by license plate
- Simple dashboard
- (optional) Export to JSON or CSV

## Testing
- Unit tests for business logic


## Future Features (Do not implement unless requested)
- PDF export
- Cloud sync
- Multi-user login
- Notifications
- OCR license plates
- AI assistance

## Aspire-Specific Policies
- Each service should be isolated
- Aspire resource file(s) must define:
  - App front end
  - API backend
  - LiteDB container or mounted volume if externalized
- Environment configuration via Aspire manifests where possible
- Avoid manual environment variable sprawl

## Documentation
- Keep docs short and practical
- Architecture + decisions documented in /docs
- Include Aspire diagrams if useful

## Pull Requests
- Small, focused PRs
- Clear descriptions
- Breaking changes explicitly documented

## Deliverables
The application must be:
- Stable
- Simple
- Offline-first
- Aspire-enabled
- Container-ready
- Easy to deploy on Azure

## Tone of Code
- Professional
- Clean
- Minimal
