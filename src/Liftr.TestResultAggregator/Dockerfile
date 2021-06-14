FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.2-buster-slim AS base
# stretch-slim: https://stackoverflow.com/questions/57501053/asp-net-core-docker-image-aspnet2-2-stretch-slim

ARG TEST_AGGREGATOR_WEB_ID=6391
RUN groupadd -g $TEST_AGGREGATOR_WEB_ID testaggregator && \
    useradd -r -u $TEST_AGGREGATOR_WEB_ID -g testaggregator testaggregator

WORKDIR /home/testaggregator
COPY bin/publish/ .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
USER testaggregator:testaggregator
ENTRYPOINT ["dotnet", "Microsoft.Liftr.TestResultAggregator.dll"]