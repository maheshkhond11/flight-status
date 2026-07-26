# Flight Status

A simple Flight Status application built using:

- **Backend:** ASP.NET Core (.NET 10)
- **Frontend:** Angular
- **Containerization:** Docker

---

## Prerequisites

- .NET 10 SDK
- Node.js (LTS)
- Angular CLI
- Docker Desktop

---

# Backend

Navigate to the API project:

```bash
cd FlightStatus.Api
```

Run locally:

```bash
dotnet restore
dotnet run
```

---

# Docker

## Build Docker Image

```bash
docker build -f "C:\Users\Acer\source\repos\flight-status\FlightStatus.Api\Dockerfile" ^
-t flightstatusapi:dev ^
--label "com.microsoft.created-by=visual-studio" ^
--label "com.microsoft.visual-studio.project-name=FlightStatus.Api" ^
--target base ^
--build-arg "BUILD_CONFIGURATION=Debug" ^
--build-arg "LAUNCHING_FROM_VS=true" ^
"C:\Users\Acer\source\repos\flight-status"
```

### Simplified Build Command

Run from the repository root:

```bash
docker build -t flightstatusapi:dev -f FlightStatus.Api/Dockerfile .
```

---

## Run Docker Container

```bash
docker run -d \
--name flightstatusapi \
-p 8080:8080 \
-p 8081:8081 \
flightstatusapi:dev
```

View running containers:

```bash
docker ps
```

Stop container:

```bash
docker stop flightstatusapi
```

Remove container:

```bash
docker rm flightstatusapi
```

---

# Frontend (Angular)

Navigate to the Angular project:

```bash
cd FlightStatus.UI
```

Install dependencies:

```bash
npm install
```

or

```bash
npm i
```

Start development server:

```bash
npm start
```

If your project uses Angular CLI directly:

```bash
ng serve
```

---

# Useful Docker Commands

List images:

```bash
docker images
```

List containers:

```bash
docker ps -a
```

Remove image:

```bash
docker rmi flightstatusapi:dev
```

View container logs:

```bash
docker logs flightstatusapi
```

Open shell inside container:

```bash
docker exec -it flightstatusapi /bin/sh
```

---

## Project Structure

```
flight-status
├── FlightStatus.Api
│   ├── Dockerfile
│   └── ...
├── FlightStatus.UI
│   └── ...
└── README.md
```