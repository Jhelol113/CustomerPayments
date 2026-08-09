# Etapa de construcción (Build)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo del proyecto y restaurar dependencias
COPY ["CustomerPaymentAPI/CustomerPaymentAPI.csproj", "CustomerPaymentAPI/"]
RUN dotnet restore "CustomerPaymentAPI/CustomerPaymentAPI.csproj"

# Copiar el resto del código y compilar
COPY . .
WORKDIR "/src/CustomerPaymentAPI"
RUN dotnet build "CustomerPaymentAPI.csproj" -c Release -o /app/build
RUN dotnet publish "CustomerPaymentAPI.csproj" -c Release -o /app/publish

# Etapa de ejecución (Run)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Exponer el puerto
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CustomerPaymentAPI.dll"]
