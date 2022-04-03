ARG OS_SPECIFIC=
FROM mcr.microsoft.com/dotnet/sdk:6.0${OS_SPECIFIC} AS build

WORKDIR /source

# copy csproj and restore as distinct layers
COPY Pinta.Docking/Pinta.Docking.csproj ./Pinta.Docking/
COPY Pinta.Effects/Pinta.Effects.csproj ./Pinta.Effects/
COPY Pinta.Gui.Widgets/Pinta.Gui.Widgets.csproj ./Pinta.Gui.Widgets/
COPY Pinta.Tools/Pinta.Tools.csproj ./Pinta.Tools/
COPY Pinta.Resources/Pinta.Resources.csproj ./Pinta.Resources/
COPY Pinta.Core/Pinta.Core.csproj ./Pinta.Core/
COPY Pinta/Pinta.csproj ./Pinta/
RUN dotnet restore Pinta/Pinta.csproj

# Install dependencies
RUN apt-get update && apt-get install -y \
	autoconf \
	autoconf-archive \
	automake \
	autotools-dev \
	build-essential \
	intltool \
	libglib2.0-dev \
	libgtk-3-dev \
	&& rm -rf /var/lib/apt/lists/* \
	&& apt-get clean

# copy and publish app and libraries
COPY . .
RUN ./autogen.sh
RUN make

# # Causes NETSDK1095
# -p:PublishReadyToRun=true
# # Causes Unhandled exception. System.TypeLoadException: Could not load type 'System.Collections.ObjectModel.KeyedCollection`2' from assembly 'System.ObjectModel, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'.
# -p:PublishTrimmed=true
RUN dotnet publish -c release -o /app --no-restore --runtime linux-x64 --self-contained
