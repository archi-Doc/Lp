FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:latest AS builder
#FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/nightly/sdk:8.0-preview-alpine AS builder
WORKDIR /src

RUN git clone https://github.com/archi-Doc/LP \
  && cd LP \
  && git switch dev \
  && dotnet restore \
  && dotnet build "./LPConsole/LPConsole.csproj" -c Release --self-contained false

# exec LPConsole/bin/Release/net8.0/LPConsole $@

FROM mcr.microsoft.com/dotnet/runtime:latest
WORKDIR /src
COPY --from=builder /src/LP/LPConsole/bin/Release/net8.0 .

CMD [ "./LPConsole" ]
