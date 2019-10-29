FROM mcr.microsoft.com/dotnet/core/runtime:2.2-stretch-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY PackageVersions.props .
COPY Directory.Build.props .
COPY ["SampleConsoleApp/SampleConsoleApp.csproj", "SampleConsoleApp/"]
COPY ["Liftr.Logging.GenericHosting/Liftr.Logging.GenericHosting.csproj", "Liftr.Logging.GenericHosting/"]
COPY ["Liftr.Utilities/Liftr.Utilities.csproj", "Liftr.Utilities/"]
COPY ["Liftr.Logging/Liftr.Logging.csproj", "Liftr.Logging/"]
COPY ["Liftr.DiagnosticSource/Liftr.DiagnosticSource.csproj", "Liftr.DiagnosticSource/"]
COPY ["Liftr.GenericHosting/Liftr.GenericHosting.csproj", "Liftr.GenericHosting/"]
COPY ["Liftr.Configuration/Liftr.Configuration.csproj", "Liftr.Configuration/"]
RUN dotnet restore "SampleConsoleApp/SampleConsoleApp.csproj"
COPY . .
WORKDIR "/src/SampleConsoleApp"
RUN dotnet build "SampleConsoleApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SampleConsoleApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SampleConsoleApp.dll"]