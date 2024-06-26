FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY src/Mvc .
RUN dotnet publish -c Release -o out /p:Logging=true

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
ENV URLS http://+:8080

WORKDIR /app
COPY --from=build /app/out ./
COPY appsettings.postgresql.json ./appsettings.json

EXPOSE 8080

ENTRYPOINT ["dotnet", "Mvc.dll"]
