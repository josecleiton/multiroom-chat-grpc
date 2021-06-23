FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /source
COPY Chat.Room Chat.Room
COPY Protos Protos

RUN dotnet publish Chat.Room -c Release -o /app -p:TreatWarningsAsErrors=true

FROM mcr.microsoft.com/dotnet/aspnet:5.0

WORKDIR /app
RUN apt-get update -qq && apt-get install -yqq libgdiplus libc6-dev
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Chat.Room.dll"]