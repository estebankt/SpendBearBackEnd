FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
RUN apt-get update && apt-get install -y libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy all project files for restore
COPY ["src/Api/SpendBear.Api/SpendBear.Api.csproj", "src/Api/SpendBear.Api/"]
COPY ["src/Shared/SpendBear.SharedKernel/SpendBear.SharedKernel.csproj", "src/Shared/SpendBear.SharedKernel/"]
COPY ["src/Shared/SpendBear.Infrastructure.Core/SpendBear.Infrastructure.Core.csproj", "src/Shared/SpendBear.Infrastructure.Core/"]
COPY ["src/Modules/Identity/Identity.Domain/Identity.Domain.csproj", "src/Modules/Identity/Identity.Domain/"]
COPY ["src/Modules/Identity/Identity.Application/Identity.Application.csproj", "src/Modules/Identity/Identity.Application/"]
COPY ["src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj", "src/Modules/Identity/Identity.Infrastructure/"]
COPY ["src/Modules/Identity/Identity.Api/Identity.Api.csproj", "src/Modules/Identity/Identity.Api/"]
COPY ["src/Modules/Spending/Spending.Domain/Spending.Domain.csproj", "src/Modules/Spending/Spending.Domain/"]
COPY ["src/Modules/Spending/Spending.Application/Spending.Application.csproj", "src/Modules/Spending/Spending.Application/"]
COPY ["src/Modules/Spending/Spending.Infrastructure/Spending.Infrastructure.csproj", "src/Modules/Spending/Spending.Infrastructure/"]
COPY ["src/Modules/Spending/Spending.Api/Spending.Api.csproj", "src/Modules/Spending/Spending.Api/"]
COPY ["src/Modules/Budgets/Budgets.Domain/Budgets.Domain.csproj", "src/Modules/Budgets/Budgets.Domain/"]
COPY ["src/Modules/Budgets/Budgets.Application/Budgets.Application.csproj", "src/Modules/Budgets/Budgets.Application/"]
COPY ["src/Modules/Budgets/Budgets.Infrastructure/Budgets.Infrastructure.csproj", "src/Modules/Budgets/Budgets.Infrastructure/"]
COPY ["src/Modules/Budgets/Budgets.Api/Budgets.Api.csproj", "src/Modules/Budgets/Budgets.Api/"]
COPY ["src/Modules/Notifications/Notifications.Domain/Notifications.Domain.csproj", "src/Modules/Notifications/Notifications.Domain/"]
COPY ["src/Modules/Notifications/Notifications.Application/Notifications.Application.csproj", "src/Modules/Notifications/Notifications.Application/"]
COPY ["src/Modules/Notifications/Notifications.Infrastructure/Notifications.Infrastructure.csproj", "src/Modules/Notifications/Notifications.Infrastructure/"]
COPY ["src/Modules/Notifications/Notifications.Api/Notifications.Api.csproj", "src/Modules/Notifications/Notifications.Api/"]
COPY ["src/Modules/Analytics/Analytics.Domain/Analytics.Domain.csproj", "src/Modules/Analytics/Analytics.Domain/"]
COPY ["src/Modules/Analytics/Analytics.Application/Analytics.Application.csproj", "src/Modules/Analytics/Analytics.Application/"]
COPY ["src/Modules/Analytics/Analytics.Infrastructure/Analytics.Infrastructure.csproj", "src/Modules/Analytics/Analytics.Infrastructure/"]
COPY ["src/Modules/Analytics/Analytics.Api/Analytics.Api.csproj", "src/Modules/Analytics/Analytics.Api/"]
COPY ["src/Modules/StatementImport/StatementImport.Domain/StatementImport.Domain.csproj", "src/Modules/StatementImport/StatementImport.Domain/"]
COPY ["src/Modules/StatementImport/StatementImport.Application/StatementImport.Application.csproj", "src/Modules/StatementImport/StatementImport.Application/"]
COPY ["src/Modules/StatementImport/StatementImport.Infrastructure/StatementImport.Infrastructure.csproj", "src/Modules/StatementImport/StatementImport.Infrastructure/"]
COPY ["src/Modules/StatementImport/StatementImport.Api/StatementImport.Api.csproj", "src/Modules/StatementImport/StatementImport.Api/"]

RUN dotnet restore "src/Api/SpendBear.Api/SpendBear.Api.csproj"
COPY . .
WORKDIR "/app/src/Api/SpendBear.Api"
RUN dotnet build "SpendBear.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SpendBear.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SpendBear.Api.dll"]