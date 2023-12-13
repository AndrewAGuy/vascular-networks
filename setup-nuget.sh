#! /bin/bash

nugetPath="/usr/local/bin/nuget.exe"
if [[ -f $nugetPath ]]; then
    echo "File '$nugetPath' already exists"
    read -p "Overwrite? y/n: " ow
    if [[ "$ow" != "y" ]]; then
        exit 1
    fi
fi

sudo apt install mono-devel
sudo curl -o $nugetPath "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

cat >> ~/.bash_aliases <<EOF
nuget() {
    mono $nugetPath \"\$@\"
}
export -f nuget
EOF

if (( $# == 1 )); then
    source ~/.bash_aliases
    nuget setapikey $1 -Source "https://api.nuget.org/v3/index.json"
fi
