# School Admission System

A full-stack school admission application for managing class capacity, temporary seat reservations, and student confirmations.

## Technologies
- ASP.NET Core 8
- React and Vite
- PostgreSQL
- Docker and Docker Compose

## Running the Application
Make sure Docker Desktop is running.

Run from the project root:

    docker compose up --build

## Application URLs
- Frontend: http://localhost:5173
- Backend API: http://localhost:8080
- API Documentation: http://localhost:8080/swagger

## Main Features
- Create and list school classes.
- View total, held, confirmed, and available seats.
- Apply using a 17-digit birth registration number.
- Temporarily reserve seats.
- Confirm or cancel applications.
- View student applications.
- Automatically expire reservation holds.
- Periodically refresh application and seat information.
- Persist data using PostgreSQL.

## Configuration
- HOLD_DURATION_SECONDS: Reservation duration in seconds; defaults to 600.
- VITE_API_URL: Backend API URL used by the frontend.

## Project Structure
- SchoolAdmission.Api/: Backend API and business logic.
- frontend/: React frontend.
- SchoolAdmission.Tests/: Backend automated tests.
- docker-compose.yml: Application services.
- ASSUMPTIONS.md: Project assumptions.
- AI_USAGE.md: AI usage disclosure.
