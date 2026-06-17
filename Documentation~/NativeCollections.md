# Documentation

## Overview

## Package contents

- NativeEventStream
- NativeGrid
- NativePriorityQueue
- NativeQuadTree 
- UnsafeEventStream
- UnsafeQuadTree

All collections have their own tests and there is a Performance Test of the NativeEventStream using a copy of the Unity.Collections.PerformanceTests setup. 
Tests can be generated from the DOTS dropdown menu. 

## Installation instructions

Add the following line to the `dependencies` section of your project's `manifest.json` file. Replace `1.0.0` with the version you want to install.

```json
"com.bonnfiregames.customnativecontainers": "git+https://github.com/Kavehn-Mallory/CustomNativeContainers.git#1.0.0"
```

## Requirements

This package is currently using Unity 6000.3.8f1. It might also support earlier versions, but I haven't tested it.

## Limitations

- Currently the NativeEventStream does not support writes and reads at the same time.
It should work if the same job writes first and then reads, but reading and writing in two jobs that run simultaneously will not work but there is no error message yet. 
- The NativeEventStream requires additional setup. In the default setup, the Update method of the NativeEventStream should be called once per frame. 
  - It is planned to add some form of code gen in the future to automate this, currently this has to be done manually. 

## Workflows
### NativeEventStream

The NativeEventStream uses a double-buffer setup to allow all systems to react to events. Calling the Update method of the stream will swap the buffers and clear the older buffer. 
Therefore, events will stay readable for one update call. This means that if the stream gets updated once a frame, every system that does not want to miss any events needs to check for new events every frame as well. 

To avoid reading events multiple times any interested system should store the ReadHead it requests for future use. 
The read head remembers the last element and will continue from there. If the system is no longer interested in the event type, call dispose on the ReadHead to return the ReadHead to the pool.

