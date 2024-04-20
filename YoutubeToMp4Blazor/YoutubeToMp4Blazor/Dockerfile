FROM mcr.microsoft.com/dotnet/runtime:8.0

# Set working directory
WORKDIR /app

RUN apt-get update && apt-get install -y libxcb1

COPY '/bin/Release/net8.0/publish' .
# Copy the ffmpeg archive into the Docker image
COPY ffmpeg-6.1.1 /app/ffmpeg-6.1.1
ENTRYPOINT ["dotnet", "YouToMp4Blazor.dll"]