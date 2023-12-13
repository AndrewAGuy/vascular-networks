#! /bin/bash

if (( $# == 0 )); then
    echo "No tag specified"
    exit 1
fi

# CHECK VERSION IN GIT

tag="v$1"
tagRef="refs/tags/$tag"

remoteTag=$(git ls-remote origin "$tagRef")
if [[ "$remoteTag" != "" ]] ; then
    echo "Error - Tag already exists in remote repository:"
    echo "$remoteTag"
    exit 1
fi

commit=$(git rev-parse HEAD)
if git show-ref "$tagRef" --verify --quiet ; then
    echo "Tag already exists in local repository"
    tagCommit=$(git rev-parse "$tagRef")
    if [[ $commit == $tagCommit ]] ; then
        echo "Commits are equal"
        echo "HEAD: $commit"
        echo "Fix: git tag -d $tag"
        exit 1
    else
        echo "Error - Commits differ:"
        echo "HEAD: $commit"
        echo "$tag: $tagCommit"
        exit 1
    fi
fi

echo "No tag conflicts found for proposed version: $1"

# BUILD & PACK
set -e

echo "Preparing clean build"
rm -r Release
dotnet build -c Release -p:Version="$1"

echo "Preparing package"
cp -RT package Release
nuget pack ./Release/*.nuspec -Version "$1" -p "commit=$commit;branch=$(git branch --show-current)"

# TAG & PUSH
set +e

if (( $# == 1 )); then
    read -p "Enter 'push' to upload release of $tag: " confirmation
    [[ "$confirmation" == "push" ]]
    upload=$?
else
    [[ "$2" == "push" ]]
    upload=$?
fi

if (( upload == 0 )); then
    set -e

    echo "Creating tag"
    git tag "$tag"
    git push origin "$tag"

    echo "Uploading package"
    nuget push "./*.$1.nupkg" -Source "https://api.nuget.org/v3/index.json"
fi
