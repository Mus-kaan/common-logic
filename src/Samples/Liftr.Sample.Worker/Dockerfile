FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.2-buster-slim AS base
# stretch-slim: https://stackoverflow.com/questions/57501053/asp-net-core-docker-image-aspnet2-2-stretch-slim

ARG SAMPLE_WORKER_ID=6193
RUN groupadd -g $SAMPLE_WORKER_ID sampleworker && \
    useradd -r -u $SAMPLE_WORKER_ID -g sampleworker sampleworker

WORKDIR /home/sampleworker
COPY bin/publish/ .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
USER sampleworker:sampleworker
ENTRYPOINT ["dotnet", "Liftr.Sample.Worker.dll"]