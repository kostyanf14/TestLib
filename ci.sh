#!/bin/bash

set -e

root_dir="$(cd "$(dirname $(realpath "$0"))" && pwd)"

cmd //C "${root_dir}/build.cmd"

find "${root_dir}" | grep bin