FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["UrlShortener.API/UrlShortener.API.csproj", "UrlShortener.API/"]
COPY ["UrlShortener.BusinessLogic/UrlShortener.BusinessLogic.csproj", "UrlShortener.BusinessLogic/"]
COPY ["UrlShortener.DataAccess/UrlShortener.DataAccess.csproj", "UrlShortener.DataAccess/"]
COPY ["UrlShortener.MVC/UrlShortener.MVC.csproj", "UrlShortener.MVC/"]

RUN dotnet restore "UrlShortener.API/UrlShortener.API.csproj"

COPY . .
WORKDIR /src/UrlShortener.API
RUN dotnet publish "UrlShortener.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

RUN chmod -R 755 /app
ENTRYPOINT ["dotnet", "UrlShortener.API.dll"]
