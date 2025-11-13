# Build-vaihe
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY . ./
RUN dotnet publish -c Release -o out

# Runtime-vaihe
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# Avaa portti, jota API kuuntelee
EXPOSE 5000

# Käynnistä sovellus
ENTRYPOINT ["dotnet", "LemmikkiAPI.dll"]

# Data.db säilyy koneella, kun käynnistää tällä docker run -d -p 5000:5000 -v $(pwd)/data:/app lemmikkiapi

