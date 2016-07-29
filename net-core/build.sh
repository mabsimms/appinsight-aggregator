#!/bin/bash

docker build -t masimms/appinsight-aggregator-demo:$1 .
docker save masimms/appinsight-aggregator-demo:$1 > appinsight-aggregator-demo.tar
