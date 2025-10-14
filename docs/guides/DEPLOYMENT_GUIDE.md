# NasosoTax - Deployment Guide

## Overview
This guide provides step-by-step instructions for deploying NasosoTax to various environments.

---

## 📋 Pre-Deployment Checklist

### **1. Security**
- [ ] Change JWT secret key (use strong random value)
- [ ] Update database connection string
- [ ] Enable HTTPS only
- [ ] Configure CORS for production domains
- [ ] Review and update allowed origins

### **2. Database**
- [ ] Backup existing database (if upgrading)
- [ ] Test database connectivity
- [ ] Verify migrations are applied
- [ ] Set up automated backups

### **3. Configuration**
- [ ] Update `appsettings.Production.json`
- [ ] Set environment variables
- [ ] Configure logging
- [ ] Test health check endpoint

### **4. Testing**
- [ ] Run unit tests (if available)
- [ ] Test all API endpoints
- [ ] Verify authentication flow
- [ ] Test tax calculations
- [ ] Check error handling

---

## 🚀 Deployment Options

### **Option 1: Windows IIS**

#### **Prerequisites:**
- Windows Server 2019 or later
- IIS 10 or later
- .NET 9.0 Runtime
- SQL Server or PostgreSQL

#### **Steps:**

1. **Publish the Application**
   ```powershell
   cd NasosoTax.Web
   dotnet publish -c Release -o C:\Publish\NasosoTax
   ```

2. **Install .NET Hosting Bundle**
   - Download from: https://dotnet.microsoft.com/download/dotnet/9.0
   - Install ASP.NET Core Runtime + Hosting Bundle

3. **Create IIS Application**
   ```powershell
   # Open IIS Manager
   # Create new Application Pool (.NET CLR Version: No Managed Code)
   # Create new Website or Application
   # Point to C:\Publish\NasosoTax
   # Set Application Pool
   ```

4. **Configure Application**
   - Create `appsettings.Production.json` in publish folder
   - Set environment variable: `ASPNETCORE_ENVIRONMENT=Production`
   - Configure database connection string

5. **Test Deployment**
   - Browse to http://localhost
   - Check health endpoint: /api/health/detailed
   - Test authentication

---

### **Option 2: Linux (Ubuntu/Debian)**

#### **Prerequisites:**
- Ubuntu 20.04+ or Debian 11+
- .NET 9.0 Runtime
- PostgreSQL or MySQL
- Nginx (reverse proxy)

#### **Steps:**

1. **Install .NET Runtime**
   ```bash
   wget https://dot.net/v1/dotnet-install.sh
   chmod +x dotnet-install.sh
   ./dotnet-install.sh --version latest --runtime aspnetcore
   ```

2. **Create Service User**
   ```bash
   sudo useradd -m -s /bin/bash nasosotax
   sudo mkdir -p /var/www/nasosotax
   sudo chown nasosotax:nasosotax /var/www/nasosotax
   ```

3. **Deploy Application**
   ```bash
   # On development machine
   dotnet publish -c Release -o ./publish

   # Transfer to server
   scp -r ./publish/* user@server:/var/www/nasosotax/
   ```

4. **Create Systemd Service**
   ```bash
   sudo nano /etc/systemd/system/nasosotax.service
   ```

   ```ini
   [Unit]
   Description=NasosoTax Tax Management Portal
   After=network.target

   [Service]
   Type=notify
   User=nasosotax
   WorkingDirectory=/var/www/nasosotax
   ExecStart=/home/nasosotax/.dotnet/dotnet /var/www/nasosotax/NasosoTax.Web.dll
   Restart=always
   RestartSec=10
   KillSignal=SIGINT
   SyslogIdentifier=nasosotax
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

   [Install]
   WantedBy=multi-user.target
   ```

5. **Configure Nginx**
   ```bash
   sudo nano /etc/nginx/sites-available/nasosotax
   ```

   ```nginx
   server {
       listen 80;
       server_name yourdomain.com;

       location / {
           proxy_pass http://localhost:5000;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```

6. **Enable and Start Services**
   ```bash
   sudo systemctl enable nasosotax
   sudo systemctl start nasosotax
   sudo systemctl status nasosotax

   sudo ln -s /etc/nginx/sites-available/nasosotax /etc/nginx/sites-enabled/
   sudo nginx -t
   sudo systemctl restart nginx
   ```

7. **Configure SSL with Let's Encrypt**
   ```bash
   sudo apt install certbot python3-certbot-nginx
   sudo certbot --nginx -d yourdomain.com
   ```

---

### **Option 3: Docker**

#### **1. Create Dockerfile**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["NasosoTax.Web/NasosoTax.Web.csproj", "NasosoTax.Web/"]
COPY ["NasosoTax.Application/NasosoTax.Application.csproj", "NasosoTax.Application/"]
COPY ["NasosoTax.Domain/NasosoTax.Domain.csproj", "NasosoTax.Domain/"]
COPY ["NasosoTax.Infrastructure/NasosoTax.Infrastructure.csproj", "NasosoTax.Infrastructure/"]
RUN dotnet restore "NasosoTax.Web/NasosoTax.Web.csproj"
COPY . .
WORKDIR "/src/NasosoTax.Web"
RUN dotnet build "NasosoTax.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NasosoTax.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NasosoTax.Web.dll"]
```

#### **2. Create docker-compose.yml**
```yaml
version: '3.8'

services:
  nasosotax:
    build: .
    ports:
      - "80:80"
      - "443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=nasosotax;Username=postgres;Password=yourpassword
      - Jwt__Key=your-super-secret-jwt-key
    depends_on:
      - postgres
    volumes:
      - ./logs:/app/internal/logs

  postgres:
    image: postgres:15
    environment:
      - POSTGRES_DB=nasosotax
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=yourpassword
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

volumes:
  postgres_data:
```

#### **3. Deploy with Docker**
```bash
# Build and run
docker-compose up -d

# View logs
docker-compose logs -f nasosotax

# Stop
docker-compose down
```

---

### **Option 4: Azure App Service**

#### **Steps:**

1. **Create Azure Resources**
   ```bash
   # Install Azure CLI
   az login

   # Create resource group
   az group create --name NasosoTax-RG --location eastus

   # Create App Service plan
   az appservice plan create \
     --name NasosoTax-Plan \
     --resource-group NasosoTax-RG \
     --sku B1 \
     --is-linux

   # Create web app
   az webapp create \
     --name nasosotax \
     --resource-group NasosoTax-RG \
     --plan NasosoTax-Plan \
     --runtime "DOTNETCORE:9.0"
   ```

2. **Configure Application Settings**
   ```bash
   az webapp config appsettings set \
     --name nasosotax \
     --resource-group NasosoTax-RG \
     --settings \
       ASPNETCORE_ENVIRONMENT=Production \
       Jwt__Key="your-secret-key" \
       ConnectionStrings__DefaultConnection="your-connection-string"
   ```

3. **Deploy Application**
   ```bash
   # Option A: From Visual Studio
   # Right-click project > Publish > Azure > Azure App Service

   # Option B: From CLI
   cd NasosoTax.Web
   dotnet publish -c Release
   cd bin/Release/net9.0/publish
   zip -r deploy.zip .
   az webapp deploy \
     --name nasosotax \
     --resource-group NasosoTax-RG \
     --src-path deploy.zip
   ```

4. **Configure SSL**
   - Azure provides free SSL certificate
   - Or upload custom certificate in Azure Portal

---

## 🗄️ Database Migration

### **SQLite to PostgreSQL**

1. **Export Data from SQLite**
   ```bash
   sqlite3 nasosotax.db .dump > dump.sql
   ```

2. **Install PostgreSQL**
   ```bash
   sudo apt install postgresql postgresql-contrib
   ```

3. **Create Database**
   ```bash
   sudo -u postgres psql
   CREATE DATABASE nasosotax;
   CREATE USER nasosotax_user WITH PASSWORD 'yourpassword';
   GRANT ALL PRIVILEGES ON DATABASE nasosotax TO nasosotax_user;
   \q
   ```

4. **Update Connection String**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=nasosotax;Username=nasosotax_user;Password=yourpassword"
     }
   }
   ```

5. **Update NuGet Package**
   ```bash
   cd NasosoTax.Infrastructure
   dotnet remove package Microsoft.EntityFrameworkCore.Sqlite
   dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
   ```

6. **Update DbContext Configuration**
   ```csharp
   // In Program.cs
   builder.Services.AddDbContext<TaxDbContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

7. **Run Migrations**
   ```bash
   cd NasosoTax.Web
   dotnet ef database update
   ```

---

## 🔒 Security Hardening

### **1. Update appsettings.Production.json**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "yourdomain.com",
  "ConnectionStrings": {
    "DefaultConnection": "#{DB_CONNECTION}#"
  },
  "Jwt": {
    "Key": "#{JWT_SECRET}#",
    "Issuer": "NasosoTax",
    "Audience": "NasosoTaxUsers"
  }
}
```

### **2. Environment Variables**
```bash
# Linux
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="your-connection"
export Jwt__Key="your-secret-key"

# Windows
setx ASPNETCORE_ENVIRONMENT Production
setx ConnectionStrings__DefaultConnection "your-connection"
setx Jwt__Key "your-secret-key"
```

### **3. Enable HTTPS**
```csharp
// In Program.cs
app.UseHttpsRedirection();
app.UseHsts();
```

### **4. Configure CORS for Production**
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Use
app.UseCors("Production");
```

---

## 📊 Monitoring and Maintenance

### **1. Health Checks**
```bash
# Check application health
curl https://yourdomain.com/api/health/detailed
```

### **2. Log Monitoring**
```bash
# View logs
tail -f /var/www/nasosotax/internal/logs/nasosotax-*.log

# Or use Azure Application Insights, ELK Stack, etc.
```

### **3. Database Backups**
```bash
# PostgreSQL
pg_dump -U postgres nasosotax > backup-$(date +%Y%m%d).sql

# Automate with cron
0 2 * * * /usr/bin/pg_dump -U postgres nasosotax > /backups/nasosotax-$(date +\%Y\%m\%d).sql
```

### **4. Update Application**
```bash
# Stop service
sudo systemctl stop nasosotax

# Backup current version
cp -r /var/www/nasosotax /var/www/nasosotax-backup

# Deploy new version
# ... copy files ...

# Start service
sudo systemctl start nasosotax

# Check status
sudo systemctl status nasosotax
```

---

## 🐛 Troubleshooting

### **Common Issues:**

1. **Application Won't Start**
   ```bash
   # Check logs
   journalctl -u nasosotax -n 50
   
   # Check permissions
   ls -la /var/www/nasosotax
   
   # Check .NET runtime
   dotnet --info
   ```

2. **Database Connection Failed**
   ```bash
   # Test connection
   psql -h localhost -U nasosotax_user -d nasosotax
   
   # Check connection string in logs
   ```

3. **502 Bad Gateway (Nginx)**
   ```bash
   # Check if app is running
   sudo systemctl status nasosotax
   
   # Check Nginx config
   sudo nginx -t
   
   # Check logs
   sudo tail -f /var/log/nginx/error.log
   ```

4. **JWT Token Issues**
   - Verify JWT secret is set correctly
   - Check token expiration time
   - Ensure HTTPS is used in production

---

## ✅ Post-Deployment Verification

1. **Health Check**
   ```bash
   curl https://yourdomain.com/api/health
   ```

2. **Register Test User**
   ```bash
   curl -X POST https://yourdomain.com/api/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "username": "testuser",
       "email": "test@example.com",
       "password": "SecurePass123",
       "fullName": "Test User"
     }'
   ```

3. **Login Test**
   ```bash
   curl -X POST https://yourdomain.com/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{
       "username": "testuser",
       "password": "SecurePass123"
     }'
   ```

4. **Test Tax Calculation**
   ```bash
   curl https://yourdomain.com/api/tax/brackets
   ```

---

## 📞 Support

For issues or questions:
- Check logs: `/internal/logs/` or `/var/log/nasosotax/`
- Review documentation: `README.md`, `API_DOCUMENTATION.md`
- GitHub Issues: [Repository URL]

---

**Last Updated:** October 10, 2025  
**Version:** 1.1.0
