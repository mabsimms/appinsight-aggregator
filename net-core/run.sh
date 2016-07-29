#!/usr/bin/env bash

docker run -d --net=host \
    --ulimit nofile=40012:40012 \
    --name appinsight-demo-graphite \
    masimms/appinsight-aggregator-demo:$1 \
