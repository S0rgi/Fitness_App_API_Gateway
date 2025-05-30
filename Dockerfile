# Используем .NET SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем CSPROJ и все исходники проекта
COPY Fitness_App_API_Gateway/Fitness_App_API_Gateway.csproj Fitness_App_API_Gateway/
RUN dotnet restore Fitness_App_API_Gateway/Fitness_App_API_Gateway.csproj

COPY Fitness_App_API_Gateway/ Fitness_App_API_Gateway/

# Публикуем проект
RUN dotnet publish Fitness_App_API_Gateway/Fitness_App_API_Gateway.csproj -c Release -o /app/publish

# Используем runtime образ
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Запускаем
ENTRYPOINT ["dotnet", "Fitness_App_API_Gateway.dll"]
