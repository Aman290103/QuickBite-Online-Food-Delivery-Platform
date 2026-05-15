# QuickBite: Online Food Delivery Platform

QuickBite is a modern, high-performance microservice-based food delivery platform. Built with **.NET 8**, **Angular**, and **PostgreSQL**, it features a robust architecture designed for scalability and reliability.

## 🚀 Key Features

- **Microservice Architecture**: Decoupled services for Auth, Restaurant Management, Menu, Cart, Orders, Payments, and Notifications.
- **Dynamic Restaurant Catalog**: Over 50+ permanently seeded restaurants with full menu catalogs.
- **Real-time Notifications**: Email and SMS alerts for order updates using Gmail and Twilio.
- **Secure Authentication**: Role-based access control (Admin, Owner, Customer, Delivery Agent) using JWT.
- **Modern Frontend**: Stunning, responsive UI built with Angular, featuring premium aesthetics and smooth animations.
- **High Performance**: Caching with Redis and asynchronous messaging with RabbitMQ.

## 🛠️ Technology Stack

- **Backend**: .NET 8 Web API, Entity Framework Core, YARP (API Gateway).
- **Frontend**: Angular 17+, RxJS, Tailwind CSS.
- **Database**: PostgreSQL, Redis.
- **Messaging**: RabbitMQ.
- **DevOps**: Docker, Docker Compose, Render (Deployment).

## 🏃 Getting Started

### Prerequisites

- Docker Desktop
- .NET 8 SDK
- Node.js & Angular CLI

### Local Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/Aman290103/QuickBite.git
   cd QuickBite
   ```

2. **Configure Environment Variables**:
   Create a `.env` file in the root directory and populate it with your credentials (see `.env.example`).

3. **Run with Docker Compose**:
   ```bash
   docker compose up --build
   ```

4. **Access the Application**:
   - Frontend: `http://localhost:4200`
   - API Gateway: `http://localhost:8080`

## 📂 Project Structure

- `QuickBite.Auth`: Identity and Access Management.
- `QuickBite.Restaurant`: Restaurant profiles and discovery.
- `QuickBite.Menu`: Catalog and item management.
- `QuickBite.Cart`: Basket and checkout logic.
- `QuickBite.Order`: Order lifecycle and status tracking.
- `QuickBite.Payment`: Payment processing integration.
- `QuickBite.Notification`: Real-time Email and SMS alerts.
- `QuickBite.Gateway`: YARP-based entry point for all services.

## 📄 License

This project is licensed under the MIT License.
