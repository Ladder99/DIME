# Prepare DIME and Dozzle Images

https://docs.splunk.com/Documentation/Edge/2.1.0/Admin/Containers  

```sh
cd ~
git clone https://github.com/ladder99/DIME

cd DIME/DIME
docker build -f Dockerfile --tag=ladder99/dime:latest .

cd ~/DIME/DIME/Connectors/SplunkEhSdk/Dime
docker save -o dime.tar ladder99/dime:latest
tar -cf dime-tar.tar edge.json dime.tar

cd ~/DIME/DIME/Connectors/SplunkEhSdk/Dozzle
docker save -o dozzle.tar amir20/dozzle:latest
tar -cf dozzle-tar.tar edge.json dozzle.tar
```
