# syntax=docker.io/docker/dockerfile:1.7-labs
ARG DOTNET_VERSION=9.0.101
ARG OS=linux
ARG ARCH=x64

FROM ubuntu:24.10 AS dependencies
ARG DOTNET_VERSION
ARG ARCH
ARG OS

RUN apt-get update -y \
    && apt-get install -y libicu-dev curl

RUN curl -L https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.sh -o dotnet-install.sh && \
    chmod +x dotnet-install.sh && \
    ./dotnet-install.sh --version ${DOTNET_VERSION} --architecture ${ARCH} --os ${OS}

ENV PATH $PATH:/root/.dotnet

FROM dependencies AS publish
ARG OS
ARG ARCH

WORKDIR /bin/modernuo

COPY . .

RUN ./publish.sh release ${OS} ${ARCH}

FROM dependencies AS deploy
ARG DOTNET_VERSION
ARG ARCH

ENV DOTNET_ROOT=/root/.dotnet

RUN apt-get update -y \
    && apt-get install -y \
    libdeflate-dev zstd libargon2-dev

WORKDIR /bin/modernuo

COPY --from=publish /bin/modernuo/Distribution /bin/modernuo

CMD '/bin/modernuo/ModernUO'
