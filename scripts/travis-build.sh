#!/bin/bash
DOCKER_TAG=''

case "$TRAVIS_BRANCH" in
  "master")
    DOCKER_TAG=1.0.$TRAVIS_BUILD_NUMBER
    ;;
  "dev")
    DOCKER_TAG=dev
    ;;
esac

docker build -t rangerlabs/ranger.tasks.subscription_enforcer:$DOCKER_TAG --build-arg MYGET_API_KEY=$MYGET_KEY .