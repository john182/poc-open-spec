FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY MapaTributário.sln .
COPY src/MapaTributario.API/MapaTributario.API.csproj src/MapaTributario.API/
COPY tests/MapaTributario.Tests.Unit/MapaTributario.Tests.Unit.csproj tests/MapaTributario.Tests.Unit/
COPY tests/MapaTributario.Tests.Integration/MapaTributario.Tests.Integration.csproj tests/MapaTributario.Tests.Integration/
RUN dotnet restore
COPY . .
RUN dotnet publish src/MapaTributario.API/MapaTributario.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
# HTTP is used inside the Docker network only; TLS is terminated at the reverse proxy (nginx/load balancer).
# Do NOT expose this container directly to the internet without TLS termination.
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "MapaTributario.API.dll"]
