# Installation


> Minimum compatible Unity version is `6000.3.8f1` Earlier versions might work, but  I haven't tested it.

This repository contains additional NativeCollections for Unity that can be used within Jobs and burst compiled code.

It currently consists of the following collection types:
- NativeEventStream
- NativeGrid
- NativePriorityQueue
- NativeQuadTree
- UnsafeEventStream
- UnsafeQuadTree



## Installation instructions

Add the following line to the `dependencies` section of your project's `manifest.json` file. Replace `1.0.0` with the version you want to install.

```json
"com.bonnfiregames.customnativecontainers": "git+https://github.com/Kavehn-Mallory/CustomNativeContainers.git#1.0.0"
```

# Usage

## Usage of NativeEventStream

The NativeEventStream uses a double-buffer system, meaning that events will be removed after two update calls. 

The stream does not automatically update, therefore a system that handles the update call is needed.


1. Define a new NativeEventStream somewhere.

```C#
private NativeEventStream<UIEvent> _eventStream;
public NativeEventStream<UIEvent> EventStreamData => _eventStream;
```

2. On start up, initialize the event stream. 

```C#
_eventStream = new NativeEventStream<UIEvent>(Allocator.Persistent);
```

3. Call ```Update()``` on the stream whenever an event tick is done. This point depends on the use case. Usually, it will be every Unity Update or Fixed Update, but it can be any chosen point. Also, there is no requirement to call ```Update()``` at all, in that case anyone interested could read the entire event history at any point.

```C#
_eventStream.Update();
```

4. Write to the stream 

```C#
_eventStream.Enqueue(new UIEvent());
```

5. Systems that want to read from the stream need to define a ```Reader``` for the event stream.

```C#
private NativeEventStream<UIEvent>.Reader _reader;
```

6. Get a reference to the NativeEventStream instance and allocate the reader

```C#
reader = magicStorageClassInstance.EventStreamData.UIEvents.AllocateReader();
```

7. Read at least once per stream update to ensure that no events are missed 

```C#
while(_reader.Read(out UIEvent data))
{
    //do something
}
```

8. [Optional] Dispose the Reader

```C#
_reader.Dispose();
```

9. When the event stream is no longer needed, dispose the stream. This will automatically dispose all readers.

```C#
_eventStream.Dispose();
```

# Limitations 

- Currently the NativeEventStream does not support writes and reads at the same time.
  It should work if the same job writes first and then reads, but reading and writing in two jobs that run simultaneously will not work but there is no error message yet.
- The NativeEventStream requires additional setup. In the default setup, the Update method of the NativeEventStream should be called once per frame.
    - It is planned to add some form of code gen in the future to automate this, currently this has to be done manually. 

- In the worst case scenario, the NativeQuadTree might produce a tree that has more nodes than the original grid had data points.
  - This is a known bug and will be fixed in the future, but the difference should be no more than one node. 
