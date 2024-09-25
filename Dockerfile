FROM ubuntu:22.04

WORKDIR /tmp

RUN apt-get update \
	&& apt-get install -y vim wget libicu-dev libz-dev zstd libargon2-dev tzdata

RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
RUN chmod +x ./dotnet-install.sh
RUN ./dotnet-install.sh --version 7.0.400

RUN echo 'DOTNET_ROOT=$HOME/.dotnet' >> /root/.bashrc
RUN echo 'PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools' >> /root/.bashrc

WORKDIR /app

CMD [ "tail", "-f", "/dev/null" ]
