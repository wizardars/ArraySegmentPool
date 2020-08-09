## ArraySegmentPool
Pool for ArraySegments.

### Features:
- [x] *Compliance .NET Standard 2.1*
- [x] *Thread-safe*
- [x]  *Autoresize*

### Restrictions:
* **Slice ArraySegment to zero**

### Model:
```
   .---------------------------------------------.
   |                                             |
   | Array_layout => [1][2][3]                   |
   |                  |  |  +-----------+        |
   |                  |  +-----+        |        |
   |                  |        |        |        |
   |                  v        v        v        |
   |        Array => [1][1][1][2][2][2][3][3][3] |
   |                                             |
   | if Array_layout[x] = 0 (free)               |
   | if Array_layout[x] = 1 (rented)             |
   '---------------------------------------------'
```

### Usage:
#### Initialize
```C#
int MaxArraySegmentLength = 9_038;
int InitialCapacity = 1;
int MaxCapacity = 100_000;
var ArraySegmentPool = new ArraySegmentPool<byte>(MaxArraySegmentLength, InitialCapacity, MaxCapacity);
```

#### Rent ArraySegment
```C#
int ArraySegmentLength = 1250;
var ArraySegment = ArraySegmentPool.DangerousRent(ArraySegmentLength);
```

#### Return ArraySegment
```C#
ArraySegmentPool.Return(ref ArraySegment);
```