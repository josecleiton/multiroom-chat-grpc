# multiroom-chat-grpc

Projeto da disciplina Sistemas Distribuídos na UNEB Campus Salvador.

## objetivo

Construção de um sistema de chat com múltiplas salas utilizando gRPC.

### arquitetura

- `Chat.RoomManager` é o servidor responsável por saber quais salas estão disponíveis para o usuário se conectar
- `Chat.Room` é a própria sala onde ocorrerá a troca de mensagens. Na sua inicialização ele se declara ao `Chat.RoomManager`, passando o seu endereço (DNS).
- `Chat.Client` é uma implementação de cliente, bem primitiva, usando o projeto `console` do .NET. Porém, por escolhermos `gRPC`, o cliente pode ser implementado em qualquer linguagem.

## requisitos

- .NET Core 5
- Docker Compose **use se for utilizar outra solução como cliente**

## como inicializar

```bash
$ dotnet publish -c Release -o bin
$ ./bin/Chat.RoomManager
# mude as variáveis de ambiente para configurar a sala de acordo com o desejado
$ PORT=5002 HTTP_PORT=5080 NAME="Grupo da Família" ./bin/Chat.Room
# rode o exemplo de cliente
$ ./bin/Chat.Client
```

### docker + docker compose

```bash
$ docker-compose up --build
```

## contribuição

Sinta-se à vontade de implementar clientes em qualquer outra linguagem, basta adicioná-lo no diretório `clients/{tecnologia}` e abrir um pull request.