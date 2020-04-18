FROM mcr.microsoft.com/dotnet/core/runtime:3.1.2-buster-slim AS base
# stretch-slim: https://stackoverflow.com/questions/57501053/asp-net-core-docker-image-aspnet2-2-stretch-slim

ARG SAMPLE_CONSOLE_ID=6171
RUN groupadd -g $SAMPLE_CONSOLE_ID sampleconsole && \
    useradd -r -u $SAMPLE_CONSOLE_ID -g sampleconsole sampleconsole

WORKDIR /home/sampleconsole
COPY bin/publish/ .

USER sampleconsole:sampleconsole
ENTRYPOINT ["dotnet", "Liftr.Sample.WorkerService.dll"]