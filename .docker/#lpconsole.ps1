# Set-ExecutionPolicy RemoteSigned

$reponame="archidoc422/lpconsole"
$localname="lpconsole"

# Clear
docker manifest rm ${reponame}

# AMD
docker build --no-cache -f ./main_amd64.Dockerfile --platform linux/amd64 -t ${localname}:amd64 .
docker tag ${localname}:amd64 ${reponame}:amd64
docker push ${reponame}:amd64

# ARM
docker build --no-cache -f ./main_arm64.Dockerfile --platform linux/arm64 -t ${localname}:arm64 .
docker tag ${localname}:arm64 ${reponame}:arm64
docker push ${reponame}:arm64

# Push image
docker manifest create --amend ${reponame} ${reponame}:amd64 ${reponame}:arm64
docker manifest push ${reponame}
