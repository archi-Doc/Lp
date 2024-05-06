# Set-ExecutionPolicy RemoteSigned

$reponame="archidoc422/lpdev-amd64"
$localname="lpdev-amd64"

# Clear
docker manifest rm ${reponame}

# AMD
docker build --no-cache -f ./dev_amd64.Dockerfile --platform linux/amd64 -t ${localname}:amd64 .
docker tag ${localname}:amd64 ${reponame}:latest
docker push ${reponame}:latest

# ARM
# docker build --no-cache -f ./dev_arm64.Dockerfile --platform linux/arm64 -t ${localname}:arm64 .
# docker tag ${localname}:arm64 ${reponame}:latest
# docker push ${reponame}:latest
