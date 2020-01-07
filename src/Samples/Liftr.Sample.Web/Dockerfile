FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
# stretch-slim: https://stackoverflow.com/questions/57501053/asp-net-core-docker-image-aspnet2-2-stretch-slim

ARG BILLING_WEB_ID=6171
RUN groupadd -g $BILLING_WEB_ID sampleweb && \
    useradd -r -u $BILLING_WEB_ID -g sampleweb sampleweb

WORKDIR /home/sampleweb
COPY bin/publish/ .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
USER sampleweb:sampleweb
ENTRYPOINT ["dotnet", "Liftr.Sample.Web.dll"]