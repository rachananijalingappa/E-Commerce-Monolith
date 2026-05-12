FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Ecommerce.API/Ecommerce.API.csproj", "Ecommerce.API/"]
COPY ["Ecommerce.Modules.Basket/Ecommerce.Modules.Basket.csproj", "Ecommerce.Modules.Basket/"]
COPY ["Ecommerce.Modules.Catalog/Ecommerce.Modules.Catalog.csproj", "Ecommerce.Modules.Catalog/"]
COPY ["Ecommerce.Modules.Orders/Ecommerce.Modules.Orders.csproj", "Ecommerce.Modules.Orders/"]
COPY ["Ecommerce.Shared/Ecommerce.Shared.csproj", "Ecommerce.Shared/"]

RUN dotnet restore "./Ecommerce.API/Ecommerce.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Ecommerce.API"
RUN dotnet build "./Ecommerce.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ecommerce.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ecommerce.API.dll"]
