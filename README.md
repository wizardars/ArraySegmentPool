## ArraySegmentPool
Pool for ArraySegments.


### Features:
* **Compliance .NET Standard 2.1**
* **Thread-safe**
* **Autoresize**


### Restrictions:
* **Slice ArraySegment to zero**


### Usage:
#### Initialize
```C#
int DefaultLength = 9_038;
int InitialCapacity = 1;
int MaxCapacity = 100_000;
bool AutoResize = true;            
var ArraySegmentPool = new ArraySegmentPool<byte>(DefaultLength, InitialCapacity, MaxCapacity, AutoResize);
```

#### Rent ArraySegment
```C#
int SegmentSize = 1250;
var ArraySegment = ArraySegmentPool.DangerousRent(SegmentSize);
```

#### Return ArraySegment
```C#
ArraySegmentPool.Return(ref ArraySegment);
```
