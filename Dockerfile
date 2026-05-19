FROM registry2.iran.liara.ir/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

FROM registry2.iran.liara.ir/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY NuGet.Config .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY src/Directory.Build.props src/
COPY src/Directory.Packages.props src/

COPY src/ src/
COPY InvestmentCaseManagement.sln .

RUN dotnet restore "src/Services/CoreService/Core.API/Core.API.csproj"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/Services/CoreService/Core.API/Core.API.csproj" \
    -c "$BUILD_CONFIGURATION" \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Core.API.dll"]
