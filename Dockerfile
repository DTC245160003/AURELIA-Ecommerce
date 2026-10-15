# Giai doan 1: Build ASP.NET Core .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY src/HongAnhShop/HongAnhShop.csproj src/HongAnhShop/

RUN dotnet restore src/HongAnhShop/HongAnhShop.csproj

COPY src/HongAnhShop/ src/HongAnhShop/

RUN dotnet publish src/HongAnhShop/HongAnhShop.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Giai doan 2: Chay website
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

# Chay bang user khong co quyen root
# Tao thu muc luu Data Protection Keys cho user app
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R $APP_UID:$APP_UID /home/app/.aspnet

USER $APP_UID

ENTRYPOINT ["dotnet", "HongAnhShop.dll"]
