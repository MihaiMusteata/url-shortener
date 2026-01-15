FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["UrlShortener.MVC/UrlShortener.MVC.csproj", "UrlShortener.MVC/"]
COPY ["UrlShortener.BusinessLogic/UrlShortener.BusinessLogic.csproj", "UrlShortener.BusinessLogic/"]
COPY ["UrlShortener.DataAccess/UrlShortener.DataAccess.csproj", "UrlShortener.DataAccess/"]
COPY ["UrlShortener.API/UrlShortener.API.csproj", "UrlShortener.API/"]

RUN dotnet restore "UrlShortener.MVC/UrlShortener.MVC.csproj"

COPY . .
WORKDIR /src/UrlShortener.MVC
RUN dotnet publish "UrlShortener.MVC.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081

COPY --from=build /app/publish .
RUN chmod -R 755 /app
ENTRYPOINT ["dotnet", "UrlShortener.MVC.dll"]
