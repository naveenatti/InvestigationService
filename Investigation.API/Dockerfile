# Stage 1: base runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 80

# Stage 2: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . ./
RUN dotnet restore "Investigation.API/Investigation.API.csproj"
RUN dotnet publish "Investigation.API/Investigation.API.csproj" -c Release -o /app/publish

# Stage 3: final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Investigation.API.dll"]