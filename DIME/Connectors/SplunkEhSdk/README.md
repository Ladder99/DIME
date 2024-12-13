# Prepare Image

https://docs.splunk.com/Documentation/Edge/2.1.0/Admin/Containers  

```sh
cd ~
git clone https://github.com/ladder99/DIME

cd DIME/DIME
docker build -f Dockerfile --tag=ladder99/dime:latest .
cd Connectors/SplunkEhSdk
docker save -o dime.tar ladder99/dime:latest
tar -cf dime-tar.tar edge.json dime.tar
```