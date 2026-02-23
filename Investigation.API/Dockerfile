# -----------------------
# Stage 1: base runtime
# -----------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 80

# -----------------------
# Stage 2: build
# -----------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN ls -R
WORKDIR /src

# Copy solution file
COPY InvestigationService.sln .

# Copy only project files first (for caching)
COPY Investigation.API/Investigation.API.csproj Investigation.API/
COPY Investigation.Application/Investigation.Application.csproj Investigation.Application/
COPY Investigation.Domain/Investigation.Domain.csproj Investigation.Domain/
COPY Investigation.Infrastructure/Investigation.Infrastructure.csproj Investigation.Infrastructure/

# Restore dependencies
RUN dotnet restore Investigation.API/Investigation.API.csproj

# Now copy the rest of the source code
COPY . .

# Publish
WORKDIR /src/Investigation.API
RUN dotnet publish -c Release -o /app/publish

# -----------------------
# Stage 3: final
# -----------------------
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Investigation.API.dll"]