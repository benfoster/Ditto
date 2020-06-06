FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS base

ARG BUILDCONFIG=RELEASE
ARG VERSION=1.0.0

# copy csproj and restore as distinct layers
COPY ./src/Ditto/Ditto.csproj ./Ditto/
RUN dotnet restore Ditto/Ditto.csproj

# copy everything else and build
COPY ./src/Ditto/ ./Ditto/
RUN dotnet publish Ditto/Ditto.csproj -c $BUILDCONFIG -o out /p:Version=$VERSION

# build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine
WORKDIR /app
COPY --from=base /out ./

EXPOSE 5000
ENTRYPOINT ["dotnet", "Ditto.dll"]
