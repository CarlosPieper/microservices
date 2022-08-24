aws ecr get-login-password --region us-east-1 --profile weather-ecr-agent-demo | docker login --username AWS --password-stdin 552167602163.dkr.ecr.us-east-1.amazonaws.com
docker build -t cloud-weather-temperature .
docker tag cloud-weather-temperature:latest 552167602163.dkr.ecr.us-east-1.amazonaws.com/cloud-weather-temperature:latest
docker push 552167602163.dkr.ecr.us-east-1.amazonaws.com/cloud-weather-temperature:latest