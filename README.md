# Document Analysis System

A full-stack multi-user web application for uploading and analyzing project documents using AI.

## Features

- User authentication (Sign Up/Sign In) with JWT
- Upload up to 10 documents per user (PDF, Word)
- AI-powered document analysis using Claude Sonnet
- Extracts: project name, duration, resources, stages, conditions, and implementation boundaries
- Secure file storage and management
- Responsive web interface

## Tech Stack

### Backend
- ASP.NET Core 8 Web API
- Entity Framework Core
- SQLite Database
- JWT Authentication
- Claude AI API Integration

### Frontend
- React 18 with TypeScript
- React Router for navigation
- Axios for API calls
- CSS for styling

## Quick Start

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Claude API Key

### Backend Setup
```bash
cd Backend
dotnet restore
# Update appsettings.json with your Claude API key
dotnet run
```

Backend runs on http://localhost:5000

### Frontend Setup
```bash
cd frontend
npm install
npm start
```

Frontend runs on http://localhost:3000

## Usage

1. **Register**: Create an account with email and password
2. **Login**: Sign in with your credentials
3. **Upload**: Upload PDF or Word documents (max 10)
4. **Analyze**: Select documents and click "Analyze Selected"
5. **View Results**: See AI-extracted project information

## Deployment

See [DEPLOYMENT.md](./DEPLOYMENT.md) for detailed deployment instructions including:
- Azure deployment
- Docker deployment
- Linux server deployment
- Production configuration
- Troubleshooting guide

## Project Structure

```
Project2/
├── Backend/               # ASP.NET Core API
│   ├── Controllers/      # API endpoints
│   ├── Models/           # Data models
│   ├── Services/         # Business logic
│   ├── Data/             # Database context
│   └── DTOs/             # Data transfer objects
├── frontend/             # React application
│   ├── src/
│   │   ├── components/   # React components
│   │   ├── pages/        # Page components
│   │   ├── services/     # API services
│   │   ├── types/        # TypeScript types
│   │   └── contexts/     # React contexts
│   └── public/           # Static files
└── DEPLOYMENT.md         # Deployment guide
```

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user

### Documents
- `GET /api/documents` - Get user's documents
- `POST /api/documents/upload` - Upload document
- `POST /api/documents/analyze` - Analyze documents
- `DELETE /api/documents/{id}` - Delete document

## Security

- JWT-based authentication
- Secure password hashing with Identity
- File type validation
- File size limits
- User-specific document isolation
- CORS configuration

## Configuration

Backend configuration in `Backend/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  },
  "Jwt": {
    "Key": "YourSecretKey",
    "Issuer": "DocumentAnalysisAPI",
    "Audience": "DocumentAnalysisClient"
  },
  "Claude": {
    "ApiKey": "YOUR_CLAUDE_API_KEY"
  }
}
```

## License

This project is available for educational and commercial use.

## Support

For detailed setup instructions, see [DEPLOYMENT.md](./DEPLOYMENT.md).