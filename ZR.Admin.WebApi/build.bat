@echo off

docker build -t ZRAdmin:latest -f ./Dockerfile .

echo "==============�鿴����==========="
docker images

pause