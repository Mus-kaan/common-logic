FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY PackageVersions.props .
COPY Directory.Build.props .
COPY ["SampleWebApp/SampleWebApp.csproj", "SampleWebApp/"]
COPY ["Liftr.WebHosting/Liftr.WebHosting.csproj", "Liftr.WebHosting/"]
COPY ["Liftr.Configuration/Liftr.Configuration.csproj", "Liftr.Configuration/"]
COPY ["Liftr.Logging.AspNetCore/Liftr.Logging.AspNetCore.csproj", "Liftr.Logging.AspNetCore/"]
COPY ["Liftr.DiagnosticSource/Liftr.DiagnosticSource.csproj", "Liftr.DiagnosticSource/"]
COPY ["Liftr.Utilities/Liftr.Utilities.csproj", "Liftr.Utilities/"]
COPY ["Liftr.Logging/Liftr.Logging.csproj", "Liftr.Logging/"]
COPY ["Liftr.Hosting.Swagger/Liftr.Hosting.Swagger.csproj", "Liftr.Hosting.Swagger/"]
COPY ["Liftr.Contracts/Liftr.Contracts.csproj", "Liftr.Contracts/"]
RUN dotnet restore "SampleWebApp/SampleWebApp.csproj"
COPY . .
WORKDIR "/src/SampleWebApp"
RUN dotnet build "SampleWebApp.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "SampleWebApp.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SampleWebApp.dll"]