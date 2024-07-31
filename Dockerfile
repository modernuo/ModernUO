FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app
COPY . /app

#RUN cd /app/Badlands && dotnet build Badlands.csproj -c Release
RUN cd /app && ./publish.sh debug

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS runtime

RUN apt -qyy update && apt -qyy install libicu-dev libdeflate-dev zstd libargon2-dev nano

WORKDIR /app
COPY --from=build /app/Distribution /app
RUN chown -R 1000:1000 /app
USER 1000:1000