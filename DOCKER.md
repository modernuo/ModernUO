## Getting Started

Disclaimer: This document assumes familiarity with running containerized workloads.

### Build

```shell
# linux/amd64
docker build -t modernuo/modernuo .

# linux/arm64
docker build --build-arg ARCH=arm64 -t modernuo/modernuo .
```

Note: `docker buildx` is capable of building for `amd64` on `arm64` machines (and other combinations), but that beyond the scope of this document.

### Run

In order to run the image as a container you need to provide three volume mounts:
1. Save files, or an empty directory in which to save the game server.
1. Configuration files, or an empty directory in which to save the configurations.
1. Data files, from the Data directory in the repository
1. UO files.

In the following example we make three assumptions:
1. UO files are located on the host machine at the path `/some/UO/place`.
1. The `dataDirectories` array in `Configuration/modernuo.json` includes `/bin/UO`.
1. The `docker run` command is being executed in the root of the repository.

```shell
docker run \
  --volume $(pwd)/Distribution/Configuration:/bin/modernuo/Configuration \
  --volume $(pwd)/Distribution/Data:/bin/modernuo/Data \
  --volume $(pwd)/Distribution/Saves:/bin/modernuo/Saves \
  --volume /some/UO/place:/bin/UO \
  --expose 2593 \
  modernuo/modernuo:latest
```

#### Empty `Configuration` Directory

If mounting an empty directory to the `Configuration` directory, use the following command instead.  This variation will allow user input.  This is required for initial configuration with an empty directory.  In subsequent runs the above command should suffice.

```shell
docker run \
  -it \
  --volume $(pwd)/Distribution/Configuration:/bin/modernuo/Configuration \
  --volume $(pwd)/Distribution/Data:/bin/modernuo/Data \
  --volume $(pwd)/Distribution/Saves:/bin/modernuo/Saves \
  --volume /some/UO/place:/bin/UO \
  --expose 2593 \
  modernuo/modernuo:latest
```
Note: the volume is still required for the game data directory, the configuration affects the component that is `/bin/UO` in the example.

### Connect

Connect to the shard at `127.0.0.1:2593`.
