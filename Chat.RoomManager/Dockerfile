FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /source
COPY Chat.RoomManager Chat.RoomManager
COPY Protos Protos

RUN dotnet publish Chat.RoomManager -c Release -o /app -p:TreatWarningsAsErrors=true

FROM mcr.microsoft.com/dotnet/aspnet:5.0

WORKDIR /app
RUN apt-get update -qq && apt-get install -yqq libgdiplus libc6-dev
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Chat.RoomManager.dll"]