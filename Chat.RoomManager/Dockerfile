FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /source
COPY Chat.RoomManager Chat.RoomManager
COPY Protos Protos

RUN dotnet publish Chat.RoomManager -c Release -o /app -p:TreatWarningsAsErrors=true

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine

WORKDIR /app
RUN echo "https://dl-cdn.alpinelinux.org/alpine/edge/testing" >> /etc/apk/repositories
RUN apk add --no-cache --update libgdiplus libc6-compat
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Chat.RoomManager.dll"]