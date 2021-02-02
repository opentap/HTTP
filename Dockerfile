FROM mcr.microsoft.com/dotnet/core/sdk:2.1-bionic

# Update and install dependencies
RUN apt update -y
RUN apt upgrade -y
RUN apt install unzip libc6-dev libunwind8 curl git apt-transport-https -y
RUN apt install libcurl3 -y


# Install OpenTAP
RUN wget -O opentap.tar https://www.opentap.io/docs/OpenTAP.9.1.3+c6190994.tar
RUN tar -xf opentap.tar
RUN unzip OpenTAPLinux.TapPackage -d /opt/tap
RUN chmod -R +w /opt/tap
RUN chmod +x /opt/tap/tap
ENV PATH="/opt/tap:${PATH}"
ENV TAP_PATH="/opt/tap"

# Test OpenTAP
RUN tap -h
RUN tap package list -v