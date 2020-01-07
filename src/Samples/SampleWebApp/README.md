# Build docker image of the SampleWebApp
Run the following scripts in PowerShell:

cd $(REPO)\src

docker build . -f .\SampleWebApp\Dockerfile -t sample-web-app

docker tag sample-web-app XXXX.azurecr.io/sample-web-app

docker run  -e ASPNETCORE_ENVIRONMENT=Development -it -p 5002:80 sample-web-app

docker run -it -p 5002:80 sample-web-app