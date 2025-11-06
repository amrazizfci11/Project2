# Document Analysis System - Deployment Guide

## Table of Contents
1. [System Overview](#system-overview)
2. [Prerequisites](#prerequisites)
3. [Backend Setup](#backend-setup)
4. [Frontend Setup](#frontend-setup)
5. [Configuration](#configuration)
6. [Running Locally](#running-locally)
7. [Production Deployment](#production-deployment)
8. [Troubleshooting](#troubleshooting)

---

## System Overview

This is a multi-user document analysis system built with:
- **Backend**: ASP.NET Core 8 Web API
- **Frontend**: React 18 with TypeScript
- **Database**: SQLite (can be replaced with SQL Server, PostgreSQL, etc.)
- **AI**: Claude Sonnet API for document analysis
- **Authentication**: JWT Bearer tokens

### Features
- User registration and login with JWT authentication
- Upload up to 10 documents per user (PDF and Word formats)
- AI-powered document analysis using Claude API
- Extract project information: name, duration, resources, stages, conditions, and boundaries
- Secure file storage and management
- Responsive web interface

---

## Prerequisites

### Required Software
1. **.NET 8 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
   ```bash
   # Verify installation
   dotnet --version
   ```

2. **Node.js 18+** and **npm** - [Download here](https://nodejs.org/)
   ```bash
   # Verify installation
   node --version
   npm --version
   ```

3. **Git** - [Download here](https://git-scm.com/)

### Required API Keys
- **Anthropic Claude API Key** - [Get one here](https://console.anthropic.com/)

---

## Backend Setup

### Step 1: Navigate to Backend Directory
```bash
cd Backend
```

### Step 2: Restore NuGet Packages
```bash
dotnet restore
```

### Step 3: Configure Application Settings
Edit `appsettings.json` and update the Claude API key:

```json
{
  "Claude": {
    "ApiKey": "YOUR_ACTUAL_CLAUDE_API_KEY_HERE"
  }
}
```

**IMPORTANT**: For production, use environment variables or Azure Key Vault instead of hardcoding keys.

### Step 4: Build the Backend
```bash
dotnet build
```

### Step 5: Run Database Migrations (Optional)
The application uses SQLite and will create the database automatically on first run. For other databases:

```bash
# For SQL Server or PostgreSQL, update ConnectionStrings in appsettings.json
# Then run:
dotnet ef database update
```

### Step 6: Run the Backend
```bash
dotnet run
```

The API will start on `http://localhost:5000` (or check the console output for the actual port).

---

## Frontend Setup

### Step 1: Navigate to Frontend Directory
```bash
cd frontend
```

### Step 2: Install Dependencies
```bash
npm install
```

### Step 3: Configure API Endpoint (if needed)
If your backend runs on a different port, edit `frontend/src/services/api.ts`:

```typescript
const API_URL = 'http://localhost:5000/api'; // Update port if needed
```

### Step 4: Start the Frontend
```bash
npm start
```

The React app will open in your browser at `http://localhost:3000`.

---

## Configuration

### Backend Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!",
    "Issuer": "DocumentAnalysisAPI",
    "Audience": "DocumentAnalysisClient"
  },
  "Claude": {
    "ApiKey": "YOUR_CLAUDE_API_KEY"
  }
}
```

### Environment Variables (Production)
For production, use environment variables:

```bash
# Linux/Mac
export ConnectionStrings__DefaultConnection="YourConnectionString"
export Jwt__Key="YourSecretKey"
export Claude__ApiKey="YourClaudeKey"

# Windows PowerShell
$env:ConnectionStrings__DefaultConnection="YourConnectionString"
$env:Jwt__Key="YourSecretKey"
$env:Claude__ApiKey="YourClaudeKey"
```

---

## Running Locally

### Complete Local Setup

1. **Terminal 1 - Backend**:
   ```bash
   cd Backend
   dotnet run
   ```

2. **Terminal 2 - Frontend**:
   ```bash
   cd frontend
   npm start
   ```

3. **Open Browser**:
   Navigate to `http://localhost:3000`

4. **Create an Account**:
   - Click "Register here"
   - Enter email and password (min 6 characters)
   - Password must contain: uppercase, lowercase, and digit

5. **Upload Documents**:
   - Click file input and select PDF or Word documents
   - Maximum 10 files per user
   - Click "Upload"

6. **Analyze Documents**:
   - Select documents using checkboxes
   - Click "Analyze Selected"
   - Wait for Claude AI to process (may take 30-60 seconds)
   - View results displayed on each document card

---

## Production Deployment

### Option 1: Deploy to Azure

#### Backend (Azure App Service)

1. **Create Azure App Service**:
   ```bash
   az webapp create --resource-group myResourceGroup \
     --plan myAppServicePlan \
     --name myDocAnalysisAPI \
     --runtime "DOTNET|8.0"
   ```

2. **Configure App Settings**:
   ```bash
   az webapp config appsettings set --resource-group myResourceGroup \
     --name myDocAnalysisAPI \
     --settings Jwt__Key="YourSecretKey" \
                 Claude__ApiKey="YourClaudeKey"
   ```

3. **Deploy Backend**:
   ```bash
   cd Backend
   dotnet publish -c Release -o ./publish
   cd publish
   zip -r ../publish.zip .
   az webapp deployment source config-zip --resource-group myResourceGroup \
     --name myDocAnalysisAPI --src ../publish.zip
   ```

#### Frontend (Azure Static Web Apps or Storage)

1. **Build Frontend**:
   ```bash
   cd frontend
   # Update API_URL in src/services/api.ts to your Azure backend URL
   npm run build
   ```

2. **Deploy to Azure Static Web Apps**:
   ```bash
   # Install Azure Static Web Apps CLI
   npm install -g @azure/static-web-apps-cli

   # Deploy
   swa deploy ./build --app-name myDocAnalysisFrontend
   ```

### Option 2: Deploy to Docker

#### Backend Dockerfile

Create `Backend/Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Backend.dll"]
```

#### Frontend Dockerfile

Create `frontend/Dockerfile`:

```dockerfile
FROM node:18 AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

#### Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  backend:
    build: ./Backend
    ports:
      - "5000:80"
    environment:
      - Jwt__Key=${JWT_KEY}
      - Claude__ApiKey=${CLAUDE_API_KEY}
    volumes:
      - ./uploads:/app/uploads

  frontend:
    build: ./frontend
    ports:
      - "3000:80"
    depends_on:
      - backend
```

Run with:
```bash
docker-compose up -d
```

### Option 3: Deploy to Linux Server (Ubuntu)

1. **Install Prerequisites**:
   ```bash
   # Install .NET 8
   wget https://dot.net/v1/dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --channel 8.0

   # Install Node.js
   curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
   sudo apt-get install -y nodejs

   # Install Nginx
   sudo apt-get install -y nginx
   ```

2. **Deploy Backend**:
   ```bash
   cd Backend
   dotnet publish -c Release -o /var/www/docanalysis/backend

   # Create systemd service
   sudo nano /etc/systemd/system/docanalysis-api.service
   ```

   Service file content:
   ```ini
   [Unit]
   Description=Document Analysis API

   [Service]
   WorkingDirectory=/var/www/docanalysis/backend
   ExecStart=/usr/bin/dotnet /var/www/docanalysis/backend/Backend.dll
   Restart=always
   RestartSec=10
   Environment="Jwt__Key=YourSecretKey"
   Environment="Claude__ApiKey=YourClaudeKey"

   [Install]
   WantedBy=multi-user.target
   ```

   Enable and start:
   ```bash
   sudo systemctl enable docanalysis-api
   sudo systemctl start docanalysis-api
   ```

3. **Deploy Frontend**:
   ```bash
   cd frontend
   npm run build
   sudo cp -r build/* /var/www/docanalysis/frontend/
   ```

4. **Configure Nginx**:
   ```bash
   sudo nano /etc/nginx/sites-available/docanalysis
   ```

   Nginx configuration:
   ```nginx
   server {
       listen 80;
       server_name your-domain.com;

       # Frontend
       location / {
           root /var/www/docanalysis/frontend;
           try_files $uri $uri/ /index.html;
       }

       # Backend API
       location /api {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
       }
   }
   ```

   Enable and restart:
   ```bash
   sudo ln -s /etc/nginx/sites-available/docanalysis /etc/nginx/sites-enabled/
   sudo nginx -t
   sudo systemctl restart nginx
   ```

---

## Troubleshooting

### Common Issues

#### 1. Backend won't start
- **Error**: "Unable to bind to http://localhost:5000"
  - **Solution**: Port is in use. Kill the process or change the port in `Properties/launchSettings.json`

#### 2. Database errors
- **Error**: "Unable to open database file"
  - **Solution**: Ensure the application has write permissions to the directory where app.db will be created

#### 3. CORS errors
- **Error**: "CORS policy blocked"
  - **Solution**: Verify CORS is configured in `Program.cs` and the frontend URL is in the allowed origins

#### 4. Claude API errors
- **Error**: "401 Unauthorized" from Claude
  - **Solution**: Check your Claude API key in appsettings.json
  - **Solution**: Verify your API key has sufficient credits

#### 5. File upload fails
- **Error**: "Maximum of 10 documents allowed"
  - **Solution**: Delete some existing documents first
- **Error**: "Only PDF and Word documents are allowed"
  - **Solution**: Ensure file is .pdf, .doc, or .docx

#### 6. Frontend can't connect to backend
- **Error**: Network errors in browser console
  - **Solution**: Verify backend is running on the expected port
  - **Solution**: Check `API_URL` in `frontend/src/services/api.ts`

### Logs

**Backend logs**:
```bash
# Development
cd Backend
dotnet run --verbosity detailed

# Production (systemd)
sudo journalctl -u docanalysis-api -f
```

**Frontend logs**:
- Open browser Developer Tools (F12)
- Check Console tab for errors

---

## Security Considerations

1. **Change JWT Secret**: Never use the default JWT key in production
2. **Use HTTPS**: Always use SSL/TLS certificates in production
3. **Secure API Keys**: Use environment variables or secret management services
4. **File Validation**: The system validates file types and sizes
5. **Rate Limiting**: Consider adding rate limiting to prevent abuse
6. **Database**: Use a production-grade database (PostgreSQL, SQL Server) instead of SQLite

---

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review backend logs
3. Check browser console for frontend errors
4. Verify all prerequisites are installed correctly

---

## License

This project is provided as-is for educational and commercial use.
