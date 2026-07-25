# SmartHire

SmartHire is a role-based recruitment management web application built using ASP.NET Core MVC and Entity Framework Core.

## Features

### Candidate
- Register and login
- Create and update candidate profile
- Upload resume
- Browse and search jobs
- Apply for jobs
- Track job applications
- View interview details
- Track selection/rejection status
- Candidate dashboard

### Recruiter
- Register and login
- Create company/recruiter profile
- Post jobs
- Edit and manage jobs
- Close and reopen jobs
- View job applications
- Filter candidates by application status
- View candidate profiles and resumes
- Shortlist or reject candidates
- Schedule interviews
- Reschedule interviews
- Select or reject candidates after interview
- Recruiter dashboard

### Admin
- Secure Admin login
- Admin dashboard
- View platform statistics
- Manage users
- View all jobs
- Close and reopen jobs

## Recruitment Workflow

Candidate Applies
→ Recruiter Shortlists
→ Interview Scheduled
→ Interview Rescheduled (if required)
→ Candidate Selected / Rejected

## Technologies Used

- ASP.NET Core MVC
- C#
- Entity Framework Core
- ASP.NET Core Identity
- SQL Server
- Razor Views
- Bootstrap
- HTML
- CSS
- JavaScript

## User Roles

- Candidate
- Recruiter
- Admin

## Database

The application uses SQL Server with Entity Framework Core Code First migrations.

Main entities include:

- ApplicationUser
- CandidateProfile
- RecruiterProfile
- Job
- JobApplication
- Interview

## Security

- ASP.NET Core Identity authentication
- Role-based authorization
- Candidate, Recruiter and Admin access control
- Anti-forgery validation on POST operations
- Recruiter ownership checks for recruiter-specific resources

## Project Architecture

The project follows the ASP.NET Core MVC architecture:

- Models
- Views
- Controllers
- ViewModels
- Data
- Migrations

## Author

Prajakta Jagdale