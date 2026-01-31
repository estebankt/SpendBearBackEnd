FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Api/SpendBear.Api/SpendBear.Api.csproj", "src/Api/SpendBear.Api/"]
COPY ["src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj", "src/Modules/Identity/Identity.Infrastructure/"]
COPY ["src/Shared/SpendBear.Infrastructure.Core/SpendBear.Infrastructure.Core.csproj", "src/Shared/SpendBear.Infrastructure.Core/"]
COPY ["src/Shared/SpendBear.SharedKernel/SpendBear.SharedKernel.csproj", "src/Shared/SpendBear.SharedKernel/"]
RUN dotnet restore "src/Api/SpendBear.Api/SpendBear.Api.csproj"
COPY . .
WORKDIR "/src/src/Api/SpendBear.Api"
RUN dotnet build "SpendBear.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SpendBear.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SpendBear.Api.dll"]
