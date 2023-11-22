#!/bin/bash

# Run the docker container
docker run \
  --env-file "${GITHUB_ENV}" \
  --env "GITHUB_ACTION_ENVIRONMENT=true" \
  --env "GITHUB_ACTOR=${GITHUB_ACTOR}" \
  --env "GITHUB_ENV=${GITHUB_ENV}" \
  --env "GITHUB_OUTPUT=${GITHUB_OUTPUT}" \
  --env "GITHUB_TOKEN=${GITHUB_TOKEN}" \
  --env "GITHUB_TRIGGERING_ACTOR=${GITHUB_TRIGGERING_ACTOR}" \
  --env "GITHUB_WORKSPACE=${GITHUB_WORKSPACE}" \
  --env "INPUT_CONFIGURATION=${INPUT_CONFIGURATION}" \
  --env "INPUT_GITHUB_ORGANIZATION=${INPUT_GITHUB_ORGANIZATION}" \
  --env "INPUT_NUGET_API_KEY=${INPUT_NUGET_API_KEY}" \
  --env "INPUT_NUGET_AUTH_FOR_BUILD=${INPUT_NUGET_AUTH_FOR_BUILD}" \
  --env "INPUT_NUGET_PASSWORD=${INPUT_NUGET_PASSWORD}" \
  --env "INPUT_NUGET_USERNAME=${INPUT_NUGET_USERNAME}" \
  --env "INPUT_NUSPEC_FILE=${INPUT_NUSPEC_FILE}" \
  --env "INPUT_OUTPUT=${INPUT_OUTPUT}" \
  --env "INPUT_PACKAGE_NAME=${INPUT_PACKAGE_NAME}" \
  --env "INPUT_PLATFORM=${INPUT_PLATFORM}" \
  --env "INPUT_PROJECT=${INPUT_PROJECT}" \
  --env "INPUT_RUNTIME=${INPUT_RUNTIME}" \
  --env "INPUT_SCAN_FOR_PACKAGE_NAME=${INPUT_SCAN_FOR_PACKAGE_NAME}" \
  --env "INPUT_VERBOSITY=${INPUT_VERBOSITY}" \
  --env "INPUT_VERSION=${INPUT_VERSION}" \
  --interactive \
  --network 'host' \
  --rm \
  --volume "${GITHUB_ENV}:${GITHUB_ENV}" \
  --volume "${GITHUB_OUTPUT}:${GITHUB_OUTPUT}" \
  --volume "${GITHUB_WORKSPACE}:${GITHUB_WORKSPACE}" \
  "${DOCKER_IMAGE}" \
    publish \
      --configuration "${INPUT_CONFIGURATION}" \
      --github-organization "${INPUT_GITHUB_ORGANIZATION}" \
      --nuget-api-key "${INPUT_NUGET_API_KEY}" \
      --nuget-auth-for-build "${INPUT_NUGET_AUTH_FOR_BUILD}" \
      --nuget-password "${INPUT_NUGET_PASSWORD}" \
      --nuget-username "${INPUT_NUGET_USERNAME}" \
      --nuspec-file "${INPUT_NUSPEC_FILE}" \
      --output "${INPUT_OUTPUT}" \
      --package-name "${INPUT_PACKAGE_NAME}" \
      --project "${INPUT_PROJECT}" \
      --scan-for-package-name "${INPUT_SCAN_FOR_PACKAGE_NAME}" \
      --verbosity "${INPUT_VERBOSITY}" \
      --version "${INPUT_VERSION}" \
      --working-directory "${GITHUB_WORKSPACE}";
