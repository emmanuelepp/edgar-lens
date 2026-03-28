FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/EdgarLens.Api/EdgarLens.Api.csproj", "src/EdgarLens.Api/"]
COPY ["src/EdgarLens.Core/EdgarLens.Core.csproj", "src/EdgarLens.Core/"]
COPY ["src/EdgarLens.Infrastructure/EdgarLens.Infrastructure.csproj", "src/EdgarLens.Infrastructure/"]
RUN dotnet restore "src/EdgarLens.Api/EdgarLens.Api.csproj"
COPY . .
WORKDIR "/src/src/EdgarLens.Api"
RUN dotnet build "EdgarLens.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EdgarLens.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EdgarLens.Api.dll"]