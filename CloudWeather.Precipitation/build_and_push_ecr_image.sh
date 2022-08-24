aws ecr get-login-password --region us-east-1 --profile weather-ecr-agent-demo | docker login --username AWS --password-stdin 552167602163.dkr.ecr.us-east-1.amazonaws.com
docker build -t cloud-weather-precipitation .
docker tag cloud-weather-precipitation:latest 552167602163.dkr.ecr.us-east-1.amazonaws.com/cloud-weather-precipitation:latest
docker push 552167602163.dkr.ecr.us-east-1.amazonaws.com/cloud-weather-precipitation:latest