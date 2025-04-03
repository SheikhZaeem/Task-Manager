# Task Manager API 🚀

A backend API to manage personal tasks with secure authentication, built for a software engineering internship assessment.

**Key Features**  
- User registration/login with password hashing  
- Create, read, update, and delete tasks  
- Daily/weekly/monthly task reporting  
- JWT authentication for all protected routes  
- Self-documenting via Swagger UI  

## Tech Stack
- **Framework**: ASP.NET Core 7  
- **Database**: MongoDB  
- **Authentication**: JWT + BCrypt  
- **Tools**: Swagger for API documentation  

## Getting Started

### Prerequisites
- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- [MongoDB Community Server](https://www.mongodb.com/try/download/community)
- (Optional) [Postman](https://www.postman.com/downloads/) for API testing

### Installation
1. Clone the repo:
   ```bash
   git clone https://github.com/yourusername/TaskManager.git
   cd TaskManager/TaskManager

### Start MongoDB (macOS)
brew services start mongodb-community

### Run API
dotnet run

### Open Swagger UI in your browser
`http://localhost:5102/swagger`
