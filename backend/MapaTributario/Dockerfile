FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY backend/MapaTributario/MapaTributario.sln ./backend/MapaTributario/
COPY backend/MapaTributario/src/MapaTributario.API/MapaTributario.API.csproj ./backend/MapaTributario/src/MapaTributario.API/
COPY backend/MapaTributario/tests/MapaTributario.Tests.Unit/MapaTributario.Tests.Unit.csproj ./backend/MapaTributario/tests/MapaTributario.Tests.Unit/
COPY backend/MapaTributario/tests/MapaTributario.Tests.Integration/MapaTributario.Tests.Integration.csproj ./backend/MapaTributario/tests/MapaTributario.Tests.Integration/
RUN dotnet restore backend/MapaTributario/MapaTributario.sln

# Copy context directory (municipios.json seed data) and all source
COPY context/ ./context/
COPY backend/MapaTributario/ ./backend/MapaTributario/
RUN dotnet publish backend/MapaTributario/src/MapaTributario.API/MapaTributario.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
# HTTP is used inside the Docker network only; TLS is terminated at the reverse proxy (nginx/load balancer).
# Do NOT expose this container directly to the internet without TLS termination.
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "MapaTributario.API.dll"]
