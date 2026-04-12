# BlindMatch PAS — Setup Guide
## PUSL2020 Coursework | Group Assignment

---

## Prerequisites
- .NET 8 SDK (https://dotnet.microsoft.com/download)
- SQL Server LocalDB (installed with Visual Studio)
- Visual Studio 2022 or VS Code

---

## Quick Start

### 1. Clone / Extract the project

```bash
# If using Git, initialise your repo first:
git init
git add .
git commit -m "feat: initial project setup — CLI initialisation"
```

### 2. Restore packages

```bash
dotnet restore
```

### 3. Apply EF Core Migrations

```bash
cd BlindMatchPAS
dotnet ef migrations add InitialCreate
dotnet ef database update
```

This creates the database with all tables and seeds:
- 6 default research areas
- Admin account: `admin@blindmatch.ac.lk` / `Admin@1234`
- Module Leader: `moduleleader@blindmatch.ac.lk` / `Leader@1234`

### 4. Run the application

```bash
dotnet run
```

Navigate to `https://localhost:5000`

---

## Run Tests

```bash
cd BlindMatchPAS.Tests
dotnet test --verbosity normal
```

---

## Project Structure

```
BlindMatchPAS/
├── Controllers/
│   ├── AccountController.cs      # Login, Register, Logout
│   ├── StudentController.cs      # Student dashboard & proposals
│   ├── SupervisorController.cs   # Blind review & match confirm
│   ├── ModuleLeaderController.cs # Oversight & reassignment
│   └── AdminController.cs        # User management
├── Data/
│   ├── ApplicationDbContext.cs   # EF Core context + seeded data
│   └── DbSeeder.cs               # Role & default user seeding
├── Models/
│   ├── Models.cs                 # Domain entities
│   └── ViewModels.cs             # View-specific models
├── Services/
│   └── BlindMatchService.cs      # Core blind-match business logic
├── Views/
│   ├── Account/                  # Login, Register
│   ├── Student/                  # Dashboard, Submit, Edit
│   ├── Supervisor/               # Dashboard (blind review)
│   ├── ModuleLeader/             # Admin oversight
│   └── Admin/                    # User management
└── Program.cs                    # DI, middleware, startup

BlindMatchPAS.Tests/
├── Unit/
│   ├── BlindMatchServiceTests.cs # 12 unit tests for core logic
│   └── ControllerTests.cs        # Controller tests with Moq
└── Integration/
    └── DatabaseIntegrationTests.cs # 5 integration tests
```

## User Roles

| Role | Default Account | Password |
|------|----------------|----------|
| Admin | admin@blindmatch.ac.lk | Admin@1234 |
| Module Leader | moduleleader@blindmatch.ac.lk | Leader@1234 |
| Supervisor | Register via /Account/Register | — |
| Student | Register via /Account/Register | — |

---

## Blind Match Flow

```
Student submits proposal (status: Pending)
        ↓
Supervisor browses proposals (NO student name shown)
        ↓
Supervisor clicks "Express Interest" (status: Under Review)
        ↓
Supervisor clicks "Confirm & Reveal" (status: Matched)
        ↓
★ IDENTITY REVEAL ★ — both parties see each other's details
```
