FROM mcr.hamdocker.ir/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

COPY . .
ENTRYPOINT ["dotnet", "Core.API.dll"]
