#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Backend.Functions/Backend.Functions.csproj", "Backend.Functions/"]
COPY ["Backend.Common/Backend.Common.csproj", "Backend.Common/"]
COPY ["Backend.Models/Backend.Models.csproj", "Backend.Models/"]
COPY ["Backend.DataAccess/Backend.DataAccess.csproj", "Backend.DataAccess/"]
COPY ["Backend.Services/Backend.Services.csproj", "Backend.Services/"]
RUN dotnet restore "./Backend.Functions/./Backend.Functions.csproj"
COPY . .
WORKDIR "/src/Backend.Functions"
RUN dotnet build "./Backend.Functions.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Backend.Functions.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true