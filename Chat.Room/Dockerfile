FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /source
COPY Chat.Room Chat.Room
COPY Protos Protos

RUN dotnet publish Chat.Room -c Release -o /app -p:TreatWarningsAsErrors=true

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine

WORKDIR /app
RUN echo "https://uk.alpinelinux.org/alpine/edge/testing" >> /etc/apk/repositories
RUN apk add --no-cache --update libgdiplus libc6-compat
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Chat.Room.dll"]