#!/bin/bash

set -e

root_dir="$(cd "$(dirname $(realpath "$0"))" && pwd)"

cmd //C "${root_dir}/build.cmd"

find "${root_dir}" | grep bin

(
    cd "${root_dir}/TestLib.WorkerService/bin/x64/Release"
    7z a "${root_dir}/artifacts/WorkerService-x64-Release.zip" *
)

echo -n "value" | openssl dgst -sha256 -hmac "key"