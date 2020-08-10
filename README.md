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
                         POOL
   .---------------------------------------------.
   |                                             |
   |  arrayLayout => [1][2][3]                   |
   |                  |  |  +-----------+        |
   |                  |  +-----+        |        |
   |                  |        |        |        |
   |                  v        v        v        |
   |        array => [1][1][1][2][2][2][3][3][3] |
   |                                             |
   |  if arrayLayout[x] = 0 (free)               |
   |  if arrayLayout[x] = 1 (rented)             |
   '---------------------------------------------'
```

### Usage:
#### Initialize
```C#
int maxArraySegmentLength = 9_038;
int initialCapacity = 1;
int maxCapacity = 100_000;
var arraySegmentPool = new ArraySegmentPool<byte>(maxArraySegmentLength, initialCapacity, maxCapacity);
```

#### Rent
```C#
int arraySegmentLength = 1250;
ArraySegment<byte> arraySegment = arraySegmentPool.DangerousRent(arraySegmentLength);
```

#### Return
```C#
arraySegmentPool.Return(ref arraySegment);
```