FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
# stretch-slim: https://stackoverflow.com/questions/57501053/asp-net-core-docker-image-aspnet2-2-stretch-slim

ARG PROM2ICM_USER_ID=8371
RUN groupadd -g $PROM2ICM_USER_ID prom2icmuser && \
    useradd -r -u $PROM2ICM_USER_ID -g prom2icmuser prom2icmuser

WORKDIR /home/prom2icmuser
COPY bin/publish/ .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
USER prom2icmuser:prom2icmuser
ENTRYPOINT ["dotnet", "Microsoft.Liftr.Prom2IcM.dll"]