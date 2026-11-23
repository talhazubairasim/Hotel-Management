# 🏨 Hotel Management System (ASP.NET Core MVC)

![C#](https://img.shields.io/badge/C%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![ASP.NET MVC](https://img.shields.io/badge/ASP.NET%20MVC-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-563D7C?style=for-the-badge&logo=bootstrap&logoColor=white)

## 📋 Overview
The **Hotel Management System** is a full-featured web application designed to manage hotel operations such as room booking, customer management, and billing.  
Built with **ASP.NET Core MVC (.NET 8)**, it provides a smooth interface for both hotel staff and administrators to handle day-to-day operations efficiently.

---

## ✨ Features
✅ **Room Management** — Add, update, or delete rooms and track availability.  
✅ **Booking Management** — Create, view, and manage room bookings.  
✅ **Customer Profiles** — Store and manage guest information.  
✅ **Billing System** — Automatically generate invoices and calculate costs.  
✅ **Role-Based Access** — Separate dashboards for Admin and Receptionist.  
✅ **Booking Workflow** — Receptionists' bookings wait for Admin approval; check-in and check-out; stale requests expire and no-shows are flagged automatically.  
✅ **Security** — Custom middleware adds OWASP-recommended security headers, including a nonce-based Content-Security-Policy; anti-forgery tokens on every form; account lockout.  
✅ **Dynamic Validation** — Server rules mirrored in the browser (custom date validators), live room availability and cost estimates while booking.  
✅ **Responsive UI** — Built with Bootstrap for mobile and desktop.

---

## 🧰 Tech Stack
| Layer | Technology |
|-------|-------------|
| **Frontend** | HTML5, CSS3, JavaScript, Bootstrap |
| **Backend** | ASP.NET Core MVC (C#, .NET 8), ASP.NET Core Identity |
| **Database** | Microsoft SQL Server |
| **ORM** | Entity Framework Core |
| **Tests** | xUnit (unit + full-app HTTP tests), GitHub Actions |
| **IDE** | Visual Studio |
| **Version Control** | Git & GitHub |

---

## ⚙️ Installation & Setup

### 1️⃣ Clone the repository
```bash
git clone https://github.com/talhazubairasim/Hotel-Management.git
cd Hotel-Management
````
### 2️⃣ Open in Visual Studio
Requires Visual Studio 2022 (or the .NET 8 SDK) and SQL Server / SQL Server Express.

Launch Visual Studio

Open the solution file: Assignment.sln

### 3️⃣ Configure the database
In Web.config, update the connection string if needed (while it still says `YOUR_SERVER`, the app uses `ConnectionStrings:DefaultConnection` in `appsettings.json`, which points to local SQL Server Express):

````bash
<connectionStrings>
    <add name="HotelDBContext" connectionString="Data Source=YOUR_SERVER;Initial Catalog=HotelDB;Integrated Security=True" providerName="System.Data.SqlClient" />
</connectionStrings>
````
### 4️⃣ Apply migrations
Open the Package Manager Console:

````bash
Update-Database
````

### 5️⃣ Run the project
Press F5 or click Start Debugging in Visual Studio.
The app will launch in your browser at http://localhost:XXXX/.

In Development, the database is migrated on startup and demo data is seeded. Sign in with:

| Role | Email | Password |
|---|---|---|
| Admin | admin@hotel.local | Admin#2025 |
| Receptionist | reception@hotel.local | Reception#2025 |

Set real accounts for other environments under `Seed` in `appsettings.json` (or environment variables) and change these passwords.

### 6️⃣ Run the tests
```bash
dotnet test
```

## 🧩 Project Structure
````graphql
Hotel-Management/
├── Assignment/              # The web application
│   ├── Controllers/         # MVC Controllers handling requests
│   ├── Models/              # Entity Framework Models
│   ├── Views/               # Razor Views (UI templates)
│   ├── Scripts/             # JavaScript and client-side logic
│   ├── Content/             # CSS, images, and static assets
│   ├── App_Start/           # Route & Filter configurations
│   ├── Services/            # Booking workflow, billing, seeding, background automation
│   ├── Middleware/          # SecurityHeadersMiddleware (OWASP headers + CSP)
│   ├── Migrations/          # EF Core migrations
│   └── Web.config           # App configuration and connection strings
├── Assignment.Tests/        # xUnit tests
└── Assignment.sln           # Visual Studio solution
````
## 💡 Future Enhancements
### 🔐 Add JWT-based Authentication & Authorization

### 📱 Integrate a React or Blazor frontend for modern UI

### ☁️ Deploy on Azure or AWS using Docker containers

### 📊 Add analytics dashboards for revenue and occupancy

## 👨‍💻 Author
### Talha Zubair Asim
📧 talhazubairasim987@gmail.com

## 🌟 Contributing
### Contributions are welcome!
**If you'd like to enhance the system:**

Fork this repository
````bash
Create your feature branch (git checkout -b feature/YourFeatureName)
````
Commit your changes (git commit -m 'Add new feature')

Push to your branch (git push origin feature/YourFeatureName)

Open a Pull Request

## 📄 License
This project is open source and available under the MIT License.
Feel free to use and modify it for educational or personal purposes.
